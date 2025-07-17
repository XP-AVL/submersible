using UnityEngine;

public class WhaleCallBehavior : MonoBehaviour
{
    [Header("Call Timing")]
    [SerializeField] private float minCallInterval = 3f;
    [SerializeField] private float maxCallInterval = 12f;
    [SerializeField] private float responseChance = 0.3f; // 30% chance to respond to nearby calls
    [SerializeField] private float listenRange = 80f;     // How far can this whale hear others?
    
    [Header("Individual Personality")]
    [SerializeField] private float chattiness = 1f;       // How often this whale likes to call (0.5 = quiet, 2 = very chatty)
    [SerializeField] private float shyness = 0f;          // How much this whale avoids calling when others are active
    
    [Header("Gill Glow System")]
    [SerializeField] private JellyfishGill gillSystem;    // Reference to the gill system component
    [SerializeField] private string glowPropertyName = "_GlowIntensity"; // Shader property name
    [SerializeField] private float baseGlowIntensity = 4f; // Maximum glow intensity
    [SerializeField] private bool useNoteBasedColors = true; // Different colors for different notes?
    
    private WhaleCallSynthesizer synthesizer;
    private float nextCallTime;
    private float lastCallTime = -999f;
    private bool isListening = false;
    
    // Gill glow system variables
    private Coroutine currentGlowCoroutine;
    private Material uniqueGillMaterial; // Each whale gets its own copy
    private float currentGlowIntensity = 0f;
    private Color whaleNoteColor; // Color based on the whale's musical note
    
    // Conductor system variables
    private bool isPausedForChorus = false;
    private float pauseEndTime = 0f;
    
    // Static variables to manage the pod's overall behavior
    private static float lastAnyWhaleCallTime = -999f;
    private static int activeCalls = 0;
    private static float podQuietPeriod = 2f; // Minimum silence between any whale calls
    
    // Reset static variables when scene starts
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetStaticVariables()
    {
        lastAnyWhaleCallTime = -999f;
        activeCalls = 0;
    }
    
    // GILL GLOW SYSTEM METHODS
    
    void InitializeGillGlow()
    {
        // Create a unique material for this whale's gills
        if (gillSystem != null && gillSystem.gillMaterial != null)
        {
            Debug.Log($"Original material: {gillSystem.gillMaterial.name}");
            uniqueGillMaterial = new Material(gillSystem.gillMaterial);
            Debug.Log($"Created unique material: {uniqueGillMaterial.name}");
            gillSystem.gillMaterial = uniqueGillMaterial;
        }
        else
        {
            Debug.LogError($"Whale {gameObject.name}: GillSystem or gillMaterial is null!");
        }
    }
    
    // CONDUCTOR SYSTEM METHODS
    
    public void PauseForChorus(float duration)
    {
        isPausedForChorus = true;
        pauseEndTime = Time.time + duration;
        Debug.Log($"Whale {gameObject.name} paused for chorus (duration: {duration:F1}s)");
    }
    
    public void TriggerChorusCall()
    {
        // Force a call regardless of normal timing constraints
        Debug.Log($"🎵 Whale {gameObject.name} joins the chorus! 🎵");
        
        if (synthesizer != null)
        {
            synthesizer.TriggerCall();
            
            // Update timing but don't increment activeCalls for chorus
            // (we want to allow the chorus to override normal limits)
            lastCallTime = Time.time;
            lastAnyWhaleCallTime = Time.time;
            
            // Start the gill glow effect
            StartGillGlow();
            
            // Schedule next regular call for much later so they don't immediately call again
            nextCallTime = Time.time + Random.Range(8f, 15f);
        }
    }
    
    // Public method for conductor to check last call time
    public float GetLastCallTime()
    {
        return lastCallTime;
    }
    
    Color GetNoteColor(float frequency)
    {
        if (!useNoteBasedColors) return Color.cyan; // Default whale color
        
        // Assign colors based on the musical note for beautiful visual harmonies
        if (Mathf.Approximately(frequency, 110.00f)) return new Color(0.2f, 0.4f, 1f, 1f);    // A2 - Deep Blue
        if (Mathf.Approximately(frequency, 123.47f)) return new Color(0.4f, 1f, 0.6f, 1f);    // B2 - Sea Green  
        if (Mathf.Approximately(frequency, 164.81f)) return new Color(1f, 0.6f, 0.2f, 1f);    // E3 - Warm Orange
        if (Mathf.Approximately(frequency, 220.00f)) return new Color(0.6f, 0.2f, 1f, 1f);    // A3 - Purple
        if (Mathf.Approximately(frequency, 246.94f)) return new Color(1f, 1f, 0.4f, 1f);      // B3 - Golden
        
        return Color.cyan; // Fallback
    }
    
    void StartGillGlow()
    {
        // Stop any existing glow effect
        if (currentGlowCoroutine != null)
        {
            StopCoroutine(currentGlowCoroutine);
        }
        
        // Start new synchronized glow effect
        currentGlowCoroutine = StartCoroutine(SynchronizedGlowEffect());
    }
    
    System.Collections.IEnumerator SynchronizedGlowEffect()
    {
        float elapsed = 0f;
        float callDuration = synthesizer.CallDuration;
        
        while (elapsed < callDuration + 0.5f) // Match call duration plus fade buffer
        {
            // Calculate progress through the call (0 to 1)
            float callProgress = elapsed / callDuration;
            
            // Get the volume from the synthesizer's envelope
            float volumeLevel = 1f;
            if (synthesizer.VolumeEnvelope != null)
            {
                volumeLevel = synthesizer.VolumeEnvelope.Evaluate(Mathf.Clamp01(callProgress));
            }
            
            // Calculate glow intensity based on volume envelope
            currentGlowIntensity = volumeLevel * baseGlowIntensity;
            
            // Apply fade-in and fade-out for smooth transitions
            if (elapsed < 0.02f) // 20ms fade-in
            {
                float fadeIn = elapsed / 0.02f;
                currentGlowIntensity *= fadeIn;
            }
            else if (elapsed > callDuration + 0.4f) // 100ms fade-out
            {
                float fadeOut = 1f - ((elapsed - callDuration - 0.4f) / 0.1f);
                currentGlowIntensity *= Mathf.Max(0f, fadeOut);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at zero intensity
        currentGlowIntensity = 0f;
        currentGlowCoroutine = null;
    }
    
    void UpdateGillGlow()
    {
        if (gillSystem == null) return;
        
        // Apply current glow intensity to all gills
        for (int i = 0; i < gillSystem.GetGillCount(); i++)
        {
            LineRenderer gill = gillSystem.GetGill(i);
            if (gill != null && gill.material != null)
            {
                // Set glow intensity
                gill.material.SetFloat(glowPropertyName, currentGlowIntensity);
                
                // Set note-based color if enabled
                if (useNoteBasedColors && currentGlowIntensity > 0f)
                {
                    Color finalColor = whaleNoteColor * Mathf.Min(currentGlowIntensity, 1f);
                    gill.material.SetColor("_Color", finalColor);
                }
            }
        }
    }
    
    void Start()
    {
        synthesizer = GetComponent<WhaleCallSynthesizer>();
        
        // Initialize gill glow system
        InitializeGillGlow();
        
        // Give each whale a unique personality
        RandomizePersonality();
        
        // Schedule first call with some variation
        ScheduleNextCall();
        
        // Offset the start time randomly so not all whales start together
        nextCallTime += Random.Range(0f, 5f);
    }
    
    void Update()
    {
        // Check if whale is paused for chorus
        if (isPausedForChorus)
        {
            if (Time.time >= pauseEndTime)
            {
                isPausedForChorus = false;
                Debug.Log($"Whale {gameObject.name} resume from chorus pause");
            }
            else
            {
                // Skip normal behavior while paused
                UpdateGillGlow(); // Still update glow though
                return;
            }
        }
        
        // Check if it's time for this whale to potentially call
        if (Time.time >= nextCallTime)
        {
            TryToCall();
        }
        
        // Listen for nearby whale calls to potentially respond
        CheckForNearbyCallsToRespondTo();
        
        // Update gill glow intensity based on current audio
        UpdateGillGlow();
        
        // Emergency reset if activeCalls gets stuck (shouldn't happen, but just in case)
        if (activeCalls > 10)
        {
            Debug.LogWarning("Resetting stuck activeCalls counter!");
            activeCalls = 0;
        }
        
        // Periodic status report (every 10 seconds)
        if (Time.time % 10f < Time.deltaTime)
        {
            Debug.Log($"Pod Status: Active calls: {activeCalls}, Last call: {Time.time - lastAnyWhaleCallTime:F1}s ago");
        }
    }
    
    void TryToCall()
    {
        // Debug what's happening
        string debugReason = "";
        
        // Check pod-wide quiet period
        if (Time.time - lastAnyWhaleCallTime < podQuietPeriod)
        {
            debugReason = $"Pod quiet period (last call {Time.time - lastAnyWhaleCallTime:F1}s ago)";
            nextCallTime = Time.time + Random.Range(0.5f, 2f);
            if (Random.value < 0.1f) Debug.Log($"Whale {gameObject.name}: {debugReason}");
            return;
        }
        
        // Check if too many whales are currently calling
        if (activeCalls >= 2) // Max 2 whales calling at once
        {
            debugReason = $"Too many active calls ({activeCalls})";
            nextCallTime = Time.time + Random.Range(1f, 3f);
            if (Random.value < 0.1f) Debug.Log($"Whale {gameObject.name}: {debugReason}");
            return;
        }
        
        // Apply shyness - if other whales are active, this whale might stay quiet
        if (activeCalls > 0 && Random.value < shyness)
        {
            debugReason = $"Being shy (shyness={shyness:F2}, activeCalls={activeCalls})";
            nextCallTime = Time.time + Random.Range(2f, 5f);
            if (Random.value < 0.1f) Debug.Log($"Whale {gameObject.name}: {debugReason}");
            return;
        }
        
        // All conditions met - this whale can call!
        MakeCall();
    }
    
    void MakeCall()
    {
        if (synthesizer != null)
        {
            synthesizer.TriggerCall();
            
            // Update timing tracking
            lastCallTime = Time.time;
            lastAnyWhaleCallTime = Time.time;
            activeCalls++;
            
            // Schedule next call
            ScheduleNextCall();
            
            // Start the gill glow effect synchronized with the call
            StartGillGlow();
            
            // Start a coroutine to decrement active calls when this call ends
            StartCoroutine(TrackCallDuration());
            
            Debug.Log($"Whale {gameObject.name} calls out! Active calls: {activeCalls}");
        }
    }
    
    System.Collections.IEnumerator TrackCallDuration()
    {
        // Wait for the call duration plus a little buffer
        yield return new WaitForSeconds(synthesizer.CallDuration + 0.5f);
        
        // Safety check to prevent negative values
        activeCalls = Mathf.Max(0, activeCalls - 1);
        
        Debug.Log($"Whale {gameObject.name} finished calling. Active calls now: {activeCalls}");
    }
    
    void ScheduleNextCall()
    {
        // Calculate next call time based on personality
        float baseInterval = Random.Range(minCallInterval, maxCallInterval);
        float personalityModifier = 1f / chattiness; // More chatty = shorter intervals
        
        nextCallTime = Time.time + (baseInterval * personalityModifier);
    }
    
    void CheckForNearbyCallsToRespondTo()
    {
        // Only check occasionally to save performance
        if (Time.time % 0.5f < Time.deltaTime) // Check twice per second
        {
            // Find all other whales in listening range
            WhaleCallBehavior[] otherWhales = FindObjectsByType<WhaleCallBehavior>(FindObjectsSortMode.None);
            
            foreach (var otherWhale in otherWhales)
            {
                if (otherWhale == this) continue; // Don't respond to self
                
                float distance = Vector3.Distance(transform.position, otherWhale.transform.position);
                
                // If another whale called recently and is within range
                if (distance <= listenRange && 
                    Time.time - otherWhale.lastCallTime < 3f && // They called in last 3 seconds
                    Time.time - otherWhale.lastCallTime > 0.5f && // But not too recently
                    Time.time - lastCallTime > 5f) // And we haven't called recently
                {
                    // Maybe respond!
                    if (Random.value < responseChance)
                    {
                        // Schedule a response call soon
                        nextCallTime = Time.time + Random.Range(1f, 4f);
                        Debug.Log($"Whale {gameObject.name} decides to respond to {otherWhale.gameObject.name}");
                        break; // Only respond to one whale at a time
                    }
                }
            }
        }
    }
    
    void RandomizePersonality()
    {
        // Give each whale a slightly different personality
        chattiness = Random.Range(0.7f, 1.5f);
        shyness = Random.Range(0f, 0.8f);
        responseChance = Random.Range(0.1f, 0.5f);
        
        // Create a completely unique voice for this whale!
        if (synthesizer != null)
        {
            RandomizeVoiceParameters();
            CreateUniqueAnimationCurves();
        }
    }
    
    void RandomizeVoiceParameters()
    {
        // A minor sus2 chord: A - B - E
        // Using different octaves for a rich, deep whale-song spread
        float[] aminorSus2Notes = {
            110.00f, // A2 (root) - the deep foundation
            123.47f, // B2 (sus2) - that beautiful floating note
            164.81f, // E3 (fifth) - the harmonic pillar
            220.00f, // A3 (octave) - higher root for brightness
            246.94f  // B3 (sus2 octave) - ethereal high note
        };
        
        // Each whale gets a random note from the A minor sus2 chord
        synthesizer.CarrierFrequency = aminorSus2Notes[Random.Range(0, aminorSus2Notes.Length)];
        
        // Keep modulator frequencies that complement the chord structure
        // Lower mod frequencies maintain that bell-like, harmonic quality
        synthesizer.ModulatorFrequency = Random.Range(35f, 70f);
        
        // Modulation depth - keep it expressive but musical
        synthesizer.ModulationDepth = Random.Range(80f, 140f);
        
        // Drift - subtle organic variation that won't break the harmony
        synthesizer.DriftAmount = Random.Range(0.015f, 0.035f);
        
        // Breathing rate - slow and whale-like
        synthesizer.BreathingRate = Random.Range(0.3f, 0.6f);
        
        // Convert frequency to note name for debugging
        string noteName = GetNoteName(synthesizer.CarrierFrequency);
        
        // Set the whale's note-based color
        whaleNoteColor = GetNoteColor(synthesizer.CarrierFrequency);
        
        Debug.Log($"Whale {gameObject.name} voice: {noteName} ({synthesizer.CarrierFrequency:F1}Hz), " +
                 $"Mod={synthesizer.ModulatorFrequency:F1}Hz, Depth={synthesizer.ModulationDepth:F1}");
    }
    
    // Helper function to convert frequency to note name for debugging
    string GetNoteName(float frequency)
    {
        if (Mathf.Approximately(frequency, 110.00f)) return "A2";
        if (Mathf.Approximately(frequency, 123.47f)) return "B2";
        if (Mathf.Approximately(frequency, 164.81f)) return "E3";
        if (Mathf.Approximately(frequency, 220.00f)) return "A3";
        if (Mathf.Approximately(frequency, 246.94f)) return "B3";
        return $"{frequency:F1}Hz";
    }
    
    void CreateUniqueAnimationCurves()
    {
        // Create unique volume envelope - how the whale controls its breath
        synthesizer.VolumeEnvelope = CreateRandomVolumeEnvelope();
        
        // Create unique modulation envelope - the whale's pitch expression over time
        synthesizer.ModulationEnvelope = CreateRandomModulationEnvelope();
        
        // Create unique organic curve - the whale's long-term breathing pattern
        synthesizer.OrganicCurve = CreateRandomOrganicCurve();
    }
    
    AnimationCurve CreateRandomVolumeEnvelope()
    {
        AnimationCurve curve = new AnimationCurve();
        
        // All whales start and end quietly, but the middle varies
        curve.AddKey(0f, 0f); // Always start silent
        
        // Attack phase - how quickly does the whale reach full volume?
        float attackTime = Random.Range(0.05f, 0.3f);
        float attackLevel = Random.Range(0.7f, 1f);
        curve.AddKey(attackTime, attackLevel);
        
        // Sustain phase - some whales have steady calls, others vary
        if (Random.value > 0.3f) // 70% chance of having a sustain phase
        {
            float sustainTime = Random.Range(0.4f, 0.8f);
            float sustainLevel = Random.Range(0.6f, 1f);
            curve.AddKey(sustainTime, sustainLevel);
        }
        
        // Release phase - how does the whale end its call?
        float releaseStart = Random.Range(0.75f, 0.95f);
        float releaseLevel = Random.Range(0.1f, 0.5f);
        curve.AddKey(releaseStart, releaseLevel);
        
        curve.AddKey(1f, 0f); // Always end silent
        
        // Smooth out the curve
        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0.3f);
        }
        
        return curve;
    }
    
    AnimationCurve CreateRandomModulationEnvelope()
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Starting modulation intensity
        float startMod = Random.Range(0f, 0.4f);
        curve.AddKey(0f, startMod);
        
        // Create 2-4 interesting points in the modulation journey
        int numPoints = Random.Range(2, 5);
        for (int i = 0; i < numPoints; i++)
        {
            float time = (float)(i + 1) / (numPoints + 1);
            float intensity = Random.Range(0.1f, 1f);
            curve.AddKey(time, intensity);
        }
        
        // Ending modulation
        float endMod = Random.Range(0.2f, 0.8f);
        curve.AddKey(1f, endMod);
        
        // Smooth the transitions
        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0.2f);
        }
        
        return curve;
    }
    
    AnimationCurve CreateRandomOrganicCurve()
    {
        AnimationCurve curve = new AnimationCurve();
        
        // This curve represents the whale's "breathing" throughout the call
        // Some whales are steady, others are more expressive
        
        float expressiveness = Random.Range(0.3f, 1.5f); // How dramatic is this whale?
        
        curve.AddKey(0f, Random.Range(-0.5f, 0.5f) * expressiveness);
        
        // Add 1-3 organic variation points
        int variations = Random.Range(1, 4);
        for (int i = 0; i < variations; i++)
        {
            float time = Random.Range(0.2f, 0.8f);
            float variation = Random.Range(-1f, 1f) * expressiveness;
            curve.AddKey(time, variation);
        }
        
        curve.AddKey(1f, Random.Range(-0.5f, 0.5f) * expressiveness);
        
        // Make it smooth and organic
        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0.5f);
        }
        
        return curve;
    }
    
    void OnDestroy()
    {
        // Clean up when whale is destroyed to prevent stuck counters
        if (activeCalls > 0)
        {
            activeCalls--;
        }
    }
    
    // Visualize listening range in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, listenRange);
        
        // Show call status
        if (activeCalls > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2, 1f);
        }
    }
    
    // Helper method to create different organic presets
    [ContextMenu("Whale Song Preset")]
    void SetWhalePreset()
    {
        if (synthesizer != null)
        {
            synthesizer.CarrierFrequency = 180f;
            synthesizer.ModulatorFrequency = 45f;
            synthesizer.ModulationDepth = 120f;
            synthesizer.DriftAmount = 0.03f;
            synthesizer.BreathingRate = 0.3f;
        }
    }
    
    [ContextMenu("Bell-like Preset")]
    void SetBellPreset()
    {
        if (synthesizer != null)
        {
            synthesizer.CarrierFrequency = 440f;
            synthesizer.ModulatorFrequency = 880f;
            synthesizer.ModulationDepth = 200f;
            synthesizer.DriftAmount = 0.01f;
            synthesizer.BreathingRate = 0.1f;
        }
    }
    
    [ContextMenu("Growling Bass Preset")]
    void SetBassPreset()
    {
        if (synthesizer != null)
        {
            synthesizer.CarrierFrequency = 80f;
            synthesizer.ModulatorFrequency = 25f;
            synthesizer.ModulationDepth = 60f;
            synthesizer.DriftAmount = 0.05f;
            synthesizer.BreathingRate = 0.8f;
        }
    }
}