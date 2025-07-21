using Encounter.Runtime.Creatures;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Encounter.Runtime.Environments
{
    public class WhaleConductor : MonoBehaviour
    {
        [Header("Microphone Settings")]
        [SerializeField] private float volumeThreshold = 0.02f;    // How loud the mic needs to be
        [SerializeField] private float sustainedTime = 2f;        // How long to sustain (2 seconds)
        [SerializeField] private int sampleRate = 44100;          // Audio sample rate
        [SerializeField] private float updateRate = 0.1f;         // How often to check mic (10 times per second)
    
        [Header("Frequency-Based Detection")]
        [SerializeField] private float noiseThreshold = 0.15f;           // How "noisy" the sound needs to be (human voice has noise)
        [SerializeField] private float fundamentalFreqMin = 80f;         // Human voice fundamental range
        [SerializeField] private float fundamentalFreqMax = 400f;        // Human voice fundamental range
        [SerializeField] private int analysisWindowSize = 512;           // Smaller window for faster response
        [SerializeField] private bool showFrequencyDebug = true;         // Show detailed frequency analysis
        
        [Header("🎵 MELODIC LOCK SETTINGS! 🎵")]
        [SerializeField] private bool useMelodicLock = true;             // Toggle between sustained note and melody modes
        [SerializeField] private float[] targetIntervals = { 0, 4, 0 };  // C -> D (perfect 3rd) -> C
        [SerializeField] private float intervalTolerance = 2f;         // How many semitones off we allow (1.5 = pretty forgiving!)
        [SerializeField] private float noteHoldTime = 0.1f;              // How long they need to hold each note
        [SerializeField] private float maxPauseBetweenNotes = 10.0f;      // Reset if they pause too long
        [SerializeField] private int pitchSmoothingFrames = 5;           // How many frames we average for stable pitch
    
        [Header("Whale Response")]
        [SerializeField] private float pauseDuration = 1f;        // How long whales pause before responding
        [SerializeField] private float chorusSpread = 0.3f;       // Random delay between whale responses (0-0.3s)
        [SerializeField] private bool debugMicrophone = true;     // Show mic levels in console
    
        private AudioClip _microphoneClip;
        private string _microphoneName;
        private bool _isListening;
        private float _sustainStartTime = -1f;
        private bool _hasTriggeredChorus;
        private float _lastVolumeCheck;
    
        // Frequency analysis
        private float[] _analysisBuffer;
        private float[] _previousBuffer;
    
        // Whale management
        private WhaleCallBehavior[] _allWhales;
        private bool _chorusActive;
        
        // ✨ NEW MELODIC DETECTION STUFF! ✨
        private Queue<float> recentPitches = new Queue<float>();     // Rolling average of recent pitches
        private float currentStablePitch = 0f;                        // Our current "agreed upon" pitch
        private float basePitch = 0f;                                 // First note of the melody
        private List<float> detectedIntervals = new List<float>();    // Intervals we've detected so far
        private float currentNoteStartTime = -1f;                     // When did we start singing this note?
        private float lastNoteTime = 0f;                              // When did we last detect a note?
        private bool isCurrentlyVoicing = false;                      // Are we making sound right now?

        private void Start()
        {
            // Find all whales in the scene
            RefreshWhaleList();
        
            // Initialize frequency analysis
            _analysisBuffer = new float[analysisWindowSize];
            _previousBuffer = new float[analysisWindowSize];
        
            // Initialize microphone
            InitializeMicrophone();
        
            // Let's get EXCITED about what we're doing!
            Debug.Log($"🐋 Whale Conductor initialized! Found {_allWhales.Length} whales.");
            Debug.Log($"🎵 Using noise-based detection: Human voice (noisy) vs FM synth (pure)");
            
            if (useMelodicLock)
            {
                Debug.Log($"🎼 MELODIC MODE ACTIVE! Sing these intervals: {string.Join(", ", targetIntervals)} semitones");
                Debug.Log($"   (That's C → G → High C - a perfect fifth then an octave!)");
            }
        }

        private void Update()
        {
            // Check microphone input at specified rate
            if (Time.time - _lastVolumeCheck >= updateRate)
            {
                CheckMicrophoneInput();
                _lastVolumeCheck = Time.time;
            }
        
            // Refresh whale list periodically (in case whales are spawned/destroyed)
            if (Time.time % 5f < updateRate) // Every 5 seconds
            {
                RefreshWhaleList();
            }
            
            // Check for melodic timeout - if they stopped singing mid-melody
            if (useMelodicLock && detectedIntervals.Count > 0 && !isCurrentlyVoicing)
            {
                if (Time.time - lastNoteTime > maxPauseBetweenNotes)
                {
                    Debug.Log("🎵 Melody timed out! Starting over...");
                    ResetMelodicDetection();
                }
            }
        }

        private void InitializeMicrophone()
        {
            // Get the default microphone
            if (Microphone.devices.Length > 0)
            {
                _microphoneName = Microphone.devices[0];
                Debug.Log($"🎤 Using microphone: {_microphoneName}");
            
                // Start recording from microphone
                _microphoneClip = Microphone.Start(_microphoneName, true, 1, sampleRate);
                _isListening = true;
            }
            else
            {
                Debug.LogError("No microphone detected! Conductor system disabled.");
                enabled = false;
            }
        }

        private void CheckMicrophoneInput()
        {
            if (!_isListening || _microphoneClip == null) return;
        
            // Get current microphone position
            int micPosition = Microphone.GetPosition(_microphoneName);
            if (micPosition < 0) return;
        
            // Calculate the number of samples to analyze
            int startPosition = micPosition - analysisWindowSize;
            if (startPosition < 0) return;
        
            // Get audio data from microphone
            _microphoneClip.GetData(_analysisBuffer, startPosition);
        
            // Analyze the audio characteristics
            bool isHumanVoice = IsLikelyHumanVoice(_analysisBuffer);
            float rms = CalculateRms(_analysisBuffer);
        
            // ✨ NEW: If we're in melodic mode, also detect pitch! ✨
            if (useMelodicLock && rms >= volumeThreshold && isHumanVoice)
            {
                HandleMelodicDetection();
            }
            else if (!useMelodicLock)
            {
                // Original sustained note behavior
                HandleSustainedNote(rms, isHumanVoice);
            }
            else
            {
                // Not singing or not human voice
                isCurrentlyVoicing = false;
                currentNoteStartTime = -1f;
            }
        
            // Debug output
            if (debugMicrophone && Time.time % 0.5f < updateRate)
            {
                if (useMelodicLock && currentStablePitch > 0)
                {
                    Debug.Log($"🎵 Pitch: {currentStablePitch:F1}Hz | Intervals found: {detectedIntervals.Count}/{targetIntervals.Length}");
                }
                else
                {
                    Debug.Log($"Vol: {rms:F4} | Human: {isHumanVoice} | Threshold: {volumeThreshold:F4}");
                }
            }
        
            // Store current buffer for next comparison
            System.Array.Copy(_analysisBuffer, _previousBuffer, analysisWindowSize);
        }
        
        // 🎵 THE EXCITING NEW MELODIC DETECTION! 🎵
        void HandleMelodicDetection()
        {
            isCurrentlyVoicing = true;
            
            // Step 1: Get the current pitch using our enhanced method!
            float currentPitch = EstimateEnhancedPitch(_analysisBuffer);
            
            if (currentPitch <= 0) return; // Couldn't get a good pitch reading
            
            // Step 2: Add to our rolling average for stability
            // Think of this like a low-pass filter for pitch!
            recentPitches.Enqueue(currentPitch);
            while (recentPitches.Count > pitchSmoothingFrames)
            {
                recentPitches.Dequeue();
            }
            
            // Step 3: Get the stable pitch (median is more stable than mean for pitch!)
            currentStablePitch = GetMedianPitch();
            
            // Step 4: Check if we've held this note long enough
            if (currentNoteStartTime < 0)
            {
                currentNoteStartTime = Time.time;
                Debug.Log($"🎵 Started singing at {currentStablePitch:F1}Hz");
            }
            
            float noteHeldDuration = Time.time - currentNoteStartTime;
            
            // Step 5: If we've held the note long enough, process it!
            if (noteHeldDuration >= noteHoldTime)
            {
                ProcessMelodicNote(currentStablePitch);
                
                // Reset for next note
                currentNoteStartTime = -1f;
                lastNoteTime = Time.time;
                
                // Clear pitch history so next note starts fresh
                recentPitches.Clear();
            }
        }
        
        // 🎼 This is where we check if they sang the right interval! 🎼
        void ProcessMelodicNote(float pitch)
        {
            if (basePitch == 0)
            {
                // First note! This establishes our reference
                basePitch = pitch;
                detectedIntervals.Add(0); // First interval is always 0 (unison with itself!)
                Debug.Log($"🎵 BASE NOTE established: {pitch:F1}Hz (your 'C')");
                return;
            }
            
            // Calculate the interval from our base pitch
            // Music theory time! The ratio between frequencies tells us the interval
            float ratio = pitch / basePitch;
            
            // Convert frequency ratio to semitones
            // Why 12? Because there are 12 semitones in an octave!
            // Why log? Because pitch perception is logarithmic!
            float semitones = 12f * Mathf.Log(ratio) / Mathf.Log(2);
            
            // Which interval should we be checking?
            int expectedIndex = detectedIntervals.Count;
            if (expectedIndex >= targetIntervals.Length)
            {
                Debug.Log("🎵 Already completed the melody!");
                return;
            }
            
            float expectedInterval = targetIntervals[expectedIndex];
            float difference = Mathf.Abs(semitones - expectedInterval);
            
            Debug.Log($"🎵 Sung interval: {semitones:F1} semitones (expected {expectedInterval:F1}, difference: {difference:F1})");
            
            // Did they nail it? (Within tolerance)
            if (difference <= intervalTolerance)
            {
                detectedIntervals.Add(semitones);
                string noteName = expectedIndex == 1 ? "G" : "High C";
                Debug.Log($"✅ CORRECT! Note {detectedIntervals.Count}: {noteName}");
                
                // Check if melody is complete!
                if (detectedIntervals.Count == targetIntervals.Length)
                {
                    Debug.Log("🎊 MELODY COMPLETE! C-G-C sung perfectly! TRIGGERING WHALE CHORUS! 🎊");
                    TriggerWhaleChorus();
                    ResetMelodicDetection();
                }
            }
            else
            {
                // Wrong interval! Start over
                Debug.Log($"❌ Oops! That interval was off by {difference:F1} semitones. Try again!");
                ResetMelodicDetection();
            }
        }
        
        // 🔄 Reset our melodic detection state
        void ResetMelodicDetection()
        {
            basePitch = 0;
            detectedIntervals.Clear();
            recentPitches.Clear();
            currentStablePitch = 0;
            currentNoteStartTime = -1f;
        }
        
        // 🎯 Enhanced pitch detection using peak-finding instead of just zero crossings!
        float EstimateEnhancedPitch(float[] audioData)
        {
            // Let's find the peaks! Peaks are more reliable than zero crossings
            // because they're less affected by noise and DC offset
            List<int> peakPositions = new List<int>();
            
            // First, let's find the average amplitude to set a good threshold
            float avgAmplitude = 0;
            for (int i = 0; i < audioData.Length; i++)
            {
                avgAmplitude += Mathf.Abs(audioData[i]);
            }
            avgAmplitude /= audioData.Length;
            
            // Now find peaks that are significant (above 30% of average)
            float peakThreshold = avgAmplitude * 0.3f;
            
            for (int i = 1; i < audioData.Length - 1; i++)
            {
                // Is this a peak? Check if it's higher than its neighbors
                if (audioData[i] > audioData[i-1] && 
                    audioData[i] > audioData[i+1] && 
                    audioData[i] > peakThreshold)
                {
                    peakPositions.Add(i);
                }
            }
            
            // Need at least 2 peaks to measure period!
            if (peakPositions.Count < 2) return 0;
            
            // Calculate average period between peaks
            // This is like measuring the distance between wave crests!
            float totalPeriod = 0;
            int validPeriods = 0;
            
            for (int i = 1; i < peakPositions.Count; i++)
            {
                int period = peakPositions[i] - peakPositions[i-1];
                
                // Sanity check - is this a reasonable period for human voice?
                // Too small = probably noise, too big = probably not the fundamental
                if (period > sampleRate / fundamentalFreqMax && period < sampleRate / fundamentalFreqMin)
                {
                    totalPeriod += period;
                    validPeriods++;
                }
            }
            
            if (validPeriods == 0) return 0;
            
            float averagePeriod = totalPeriod / validPeriods;
            float frequency = sampleRate / averagePeriod;
            
            return frequency;
        }
        
        // 📊 Get the median of our recent pitches (more stable than average!)
        float GetMedianPitch()
        {
            if (recentPitches.Count == 0) return 0;
            
            // Sort the pitches to find the median
            List<float> sorted = new List<float>(recentPitches);
            sorted.Sort();
            
            // The median is the middle value - it ignores outliers!
            return sorted[sorted.Count / 2];
        }
        
        // Original sustained note handling (kept for non-melodic mode)
        void HandleSustainedNote(float rms, bool isHumanVoice)
        {
            if (rms >= volumeThreshold && isHumanVoice)
            {
                // Start or continue sustain timer
                if (_sustainStartTime < 0)
                {
                    _sustainStartTime = Time.time;
                    _hasTriggeredChorus = false;
                    Debug.Log("🎤 Human voice detected! Keep singing...");
                }
                
                // Check if we've sustained long enough
                float sustainedDuration = Time.time - _sustainStartTime;
                if (sustainedDuration >= sustainedTime && !_hasTriggeredChorus)
                {
                    TriggerWhaleChorus();
                    _hasTriggeredChorus = true;
                }
            }
            else
            {
                // Reset sustain timer if voice not detected
                if (_sustainStartTime >= 0)
                {
                    float sustainedDuration = Time.time - _sustainStartTime;
                    if (sustainedDuration < sustainedTime)
                    {
                        string reason = !isHumanVoice ? "too pure (not human voice)" : "volume too low";
                        if (showFrequencyDebug)
                        {
                            Debug.Log($"Voice lost ({sustainedDuration:F1}s) - {reason}");
                        }
                    }
                    _sustainStartTime = -1f;
                }
            }
        }

        private float CalculateRms(float[] audioData)
        {
            float sum = 0f;
            foreach (var t in audioData)
            {
                sum += t * t;
            }
            return Mathf.Sqrt(sum / audioData.Length);
        }

        private bool IsLikelyHumanVoice(float[] audioData)
        {
            // Human voices have noise/irregularities, FM synths are mathematically perfect
        
            // 1. Calculate "noise" - how much the signal deviates from being perfectly smooth
            float noiseLevel = CalculateNoiseLevel(audioData);
        
            // 2. Check frequency range (basic sanity check)
            float dominantFreq = EstimateEnhancedPitch(audioData); // Use our better pitch detection!
            bool inHumanRange = dominantFreq >= fundamentalFreqMin && dominantFreq <= fundamentalFreqMax;
        
            // 3. Check for basic energy
            float energy = CalculateRms(audioData);
            bool hasEnoughEnergy = energy > 0.001f;
        
            if (showFrequencyDebug && Time.time % 1f < updateRate)
            {
                Debug.Log($"Noise: {noiseLevel:F3} (need >{noiseThreshold:F3}) | " +
                         $"Freq: {dominantFreq:F1}Hz | Human range: {inHumanRange} | Energy: {hasEnoughEnergy}");
            }
        
            // Human voice = noisy enough + reasonable frequency + has energy
            return noiseLevel >= noiseThreshold && inHumanRange && hasEnoughEnergy;
        }

        private float CalculateNoiseLevel(float[] audioData)
        {
            // Measure how "rough" or "irregular" the signal is
            // Pure sine waves are very smooth, human voices are noisy
        
            float totalVariation = 0f;
            float totalEnergy = 0f;
        
            for (int i = 2; i < audioData.Length - 2; i++)
            {
                // Calculate how much each sample differs from the smooth trend
                float smoothed = (audioData[i-2] + audioData[i-1] + audioData[i] + audioData[i+1] + audioData[i+2]) / 5f;
                float deviation = Mathf.Abs(audioData[i] - smoothed);
            
                totalVariation += deviation;
                totalEnergy += Mathf.Abs(audioData[i]);
            }
        
            // Normalize by total energy to get relative noise level
            if (totalEnergy < 0.001f) return 0f;
            return totalVariation / totalEnergy;
        }

        private void RefreshWhaleList()
        {
            _allWhales = FindObjectsByType<WhaleCallBehavior>(FindObjectsSortMode.None);
        }

        private void TriggerWhaleChorus()
        {
            if (_chorusActive) return; // Prevent multiple simultaneous choruses
        
            Debug.Log($"🎵 WHALE CHORUS TRIGGERED! 🎵 Conducting {_allWhales.Length} whales!");
            
            // Pass the sung melody pitches to the chorus!
            float[] melodyPitches = new float[detectedIntervals.Count];
            melodyPitches[0] = basePitch; // Start with the base pitch (C)
            
            // Calculate the actual pitches for each interval
            for (int i = 1; i < detectedIntervals.Count; i++)
            {
                // Convert interval (in semitones) back to frequency ratio
                float ratio = Mathf.Pow(2f, targetIntervals[i] / 12f);
                melodyPitches[i] = basePitch * ratio;
            }
            
            StartCoroutine(ConductMelodicChorus(melodyPitches));
        }

        private System.Collections.IEnumerator ConductMelodicChorus(float[] melodyPitches)
        {
            _chorusActive = true;
        
            // First, pause all whales briefly
            foreach (var whale in _allWhales)
            {
                if (whale != null)
                {
                    whale.PauseForChorus(pauseDuration + (melodyPitches.Length * 2f)); // Longer pause for melody
                }
            }
        
            Debug.Log($"All whales pausing for {pauseDuration} seconds...");
            yield return new WaitForSeconds(pauseDuration);
        
            // Now the whales sing the melody back!
            Debug.Log("🐋 WHALE MELODIC CHORUS BEGINS! 🐋");
            
            if (useMelodicLock && melodyPitches.Length > 0)
            {
                // MELODIC MODE: Whales sing the notes in sequence!
                string[] noteNames = { "C", "G", "High C" };
                
                for (int noteIndex = 0; noteIndex < melodyPitches.Length; noteIndex++)
                {
                    Debug.Log($"🎵 Whales singing note {noteIndex + 1}: {noteNames[noteIndex]} ({melodyPitches[noteIndex]:F1}Hz)");
                    
                    // Each whale sings the same note, but with slight timing variation
                    foreach (var whale in _allWhales)
                    {
                        if (whale != null)
                        {
                            float delay = Random.Range(0f, chorusSpread);
                            StartCoroutine(DelayedMelodicWhaleCall(whale, delay, melodyPitches[noteIndex]));
                        }
                    }
                    
                    // Wait for this note to finish before the next one
                    yield return new WaitForSeconds(1.5f); // Time between melody notes
                }
                
                // Optional: Whales all sing the final note together as a big finish!
                yield return new WaitForSeconds(0.5f);
                Debug.Log("🎊 FINALE: All whales sing C together! 🎊");
                
                foreach (var whale in _allWhales)
                {
                    if (whale != null)
                    {
                        // All whales sing the tonic (base note) together
                        float delay = Random.Range(0f, chorusSpread * 0.5f); // Tighter timing for finale
                        StartCoroutine(DelayedMelodicWhaleCall(whale, delay, basePitch));
                    }
                }
            }
            else
            {
                // SUSTAINED MODE: Original behavior - all whales call at once
                foreach (var whale in _allWhales)
                {
                    if (whale != null)
                    {
                        float delay = Random.Range(0f, chorusSpread);
                        StartCoroutine(DelayedWhaleCall(whale, delay));
                    }
                }
            }
        
            // Wait for chorus to finish before allowing another
            yield return new WaitForSeconds(4f); // Buffer time
            _chorusActive = false;
        
            Debug.Log("Whale melodic chorus complete! The ocean remembers your C-G-C melody!");
        }

        private System.Collections.IEnumerator DelayedWhaleCall(WhaleCallBehavior whale, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (whale != null)
            {
                whale.TriggerChorusCall();
            }
        }
        
        // ✨ NEW: Make a whale sing a specific pitch! ✨
        System.Collections.IEnumerator DelayedMelodicWhaleCall(WhaleCallBehavior whale, float delay, float targetPitch)
        {
            yield return new WaitForSeconds(delay);
            if (whale != null)
            {
                whale.TriggerMelodicCall(targetPitch);
            }
        }

        private void OnDestroy()
        {
            // Clean up microphone
            if (_isListening)
            {
                Microphone.End(_microphoneName);
            }
        }
    
        // 🎨 GUI for debugging - now with melodic feedback!
        private void OnGUI()
        {
            if (!debugMicrophone) return;
        
            GUI.Box(new Rect(10, 10, 350, useMelodicLock ? 200 : 120), "🐋 Whale Conductor 🎵");
        
            GUI.Label(new Rect(20, 35, 320, 20), $"Whales found: {_allWhales?.Length ?? 0}");
            GUI.Label(new Rect(20, 55, 320, 20), $"Microphone: {(_isListening ? "Active" : "Inactive")}");
            GUI.Label(new Rect(20, 75, 320, 20), $"Mode: {(useMelodicLock ? "🎵 Melodic Lock (C-D-C)" : "🎤 Sustained Note")}");
            
            if (useMelodicLock)
            {
                // Show melodic progress!
                GUI.Label(new Rect(20, 95, 320, 20), $"Current Pitch: {(currentStablePitch > 0 ? currentStablePitch.ToString("F1") + " Hz" : "Not singing")}");
                
                // Visual representation of the melody
                float startX = 20;
                float startY = 120;
                float boxWidth = 100;
                float boxHeight = 25;
                
                string[] noteLabels = { "C", "D", "C" };
                
                for (int i = 0; i < targetIntervals.Length; i++)
                {
                    // Position boxes at different heights to show pitch relationships!
                    float yOffset = -targetIntervals[i] * 3; // Visual height represents pitch
                    
                    // Color coding: green = completed, yellow = current target, gray = not yet
                    Color boxColor = Color.gray;
                    if (i < detectedIntervals.Count) boxColor = Color.green;
                    else if (i == detectedIntervals.Count && isCurrentlyVoicing) boxColor = Color.yellow;
                    
                    GUI.color = boxColor;
                    GUI.Box(new Rect(startX + i * (boxWidth + 10), startY + yOffset, boxWidth, boxHeight), noteLabels[i]);
                    GUI.color = Color.white;
                }
                
                // Reset hint
                if (detectedIntervals.Count > 0 && detectedIntervals.Count < targetIntervals.Length)
                {
                    GUI.Label(new Rect(20, 160, 320, 20), 
                             $"Sing the next note! ({noteLabels[detectedIntervals.Count]})");
                }
            }
            else
            {
                // Original sustained note display
                if (_sustainStartTime >= 0)
                {
                    float progress = (Time.time - _sustainStartTime) / sustainedTime;
                    GUI.Label(new Rect(20, 95, 320, 20), $"🎤 Sustaining: {progress * 100:F1}%");
            
                    // Progress bar
                    GUI.Box(new Rect(20, 115, 320, 20), "");
                    GUI.Box(new Rect(20, 115, 320 * progress, 20), "");
                }
                else
                {
                    GUI.Label(new Rect(20, 95, 320, 20), "Sing to conduct the whale chorus!");
                }
            }
        }
    }
}