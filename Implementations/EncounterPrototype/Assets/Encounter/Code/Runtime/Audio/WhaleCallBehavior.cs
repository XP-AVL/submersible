using UnityEngine;
using System.Collections;
using Encounter.Runtime.Environments;

namespace Encounter.Runtime.Creatures
{
    public enum NoteDuration
    {
        Whole = 4,      // 4 beats
        Half = 2,       // 2 beats  
        Quarter = 1,    // 1 beat
        Eighth = 0,     // 0.5 beats (handled specially)
        Sixteenth = -1  // 0.25 beats (handled specially)
    }

    public class WhaleCallBehavior : MonoBehaviour
    {
        [Header("🎵 Musical Personality")]
        [SerializeField] private NoteDuration noteDuration = NoteDuration.Quarter;
        [SerializeField] private int octaveOffset = 0; // -2 to +2 octaves
        [SerializeField] private float pitchVariation = 0.02f; // Slight voice-like pitch drift
        
        [Header("🎭 Response Behavior")]
        [SerializeField] private float baseInterval = 25f; // When player NOT singing (very sparse!)
        [SerializeField] private float activeInterval = 1f; // Not used in new system
        [SerializeField] private float intervalDecayRate = 1f; // Not used in new system
        
        [Header("🌟 Visual Effects")]
        [SerializeField] private JellyfishGill gillSystem;
        [SerializeField] private string glowPropertyName = "_GlowIntensity";
        [SerializeField] private float baseGlowIntensity = 4f;
        
        // Private variables
        private WhaleCallSynthesizer _synthesizer;
        private Material _uniqueGillMaterial;
        private Coroutine _currentGlowCoroutine;
        
        // Musical timing
        private float _beatsUntilNextCall;
        private float _currentInterval;
        private bool _lastPlayerSingingState = false;
        private float[] _melody; // Cache the conductor's melody
        private bool _isPlayingMelody = false;
        private bool _isContinuousMode = false; // New: loop mode when player sings
        private Coroutine _continuousMelodyCoroutine;
        
        // Beat subdivision handling
        private int _subdivisionCounter = 0;

        private void Start()
        {
            // Get synthesizer component
            _synthesizer = GetComponent<WhaleCallSynthesizer>();
            
            // Initialize gill glow system
            InitializeGillGlow();
            
            // Randomize musical personality
            RandomizePersonality();
            
            // Subscribe to conductor beats
            WhaleConductor.OnBeat += HandleBeat;
            
            // Initialize timing
            _currentInterval = baseInterval;
            _beatsUntilNextCall = Random.Range(1f, baseInterval);
            
            // Get melody from conductor (we'll update this periodically)
            RefreshMelodyFromConductor();
            
            Debug.Log($"🐙 Jelly {gameObject.name} initialized: {noteDuration} notes, octave {octaveOffset:+0;-0;0}");
        }

        private void OnDestroy()
        {
            // Unsubscribe from conductor beats
            WhaleConductor.OnBeat -= HandleBeat;
            
            // Clean up continuous melody coroutine
            if (_continuousMelodyCoroutine != null)
            {
                StopCoroutine(_continuousMelodyCoroutine);
            }
        }

        private void HandleBeat(int beatNumber, float currentNote, bool isPlayerSinging)
        {
            // Handle the dramatic difference based on player singing
            if (isPlayerSinging && !_lastPlayerSingingState)
            {
                // Player just started singing - BEGIN CONTINUOUS MELODY LOOP!
                StartContinuousMelodyMode();
                Debug.Log($"🎤 {gameObject.name} PLAYER STARTED SINGING - Beginning continuous melody loop!");
            }
            else if (!isPlayerSinging && _lastPlayerSingingState)
            {
                // Player stopped singing - RETURN TO SPARSE MODE
                StopContinuousMelodyMode();
                Debug.Log($"🎤 {gameObject.name} Player stopped singing - Returning to sparse mode");
            }
            
            // Only do sparse timing when player is NOT singing
            if (!isPlayerSinging && !_isContinuousMode)
            {
                // Original sparse behavior for when player is silent
                HandleSparseTiming(beatNumber);
            }
            
            _lastPlayerSingingState = isPlayerSinging;
        }

        private void HandleSparseTiming(int beatNumber)
        {
            // Handle subdivision timing for eighth and sixteenth notes
            bool shouldProcessBeat = ShouldProcessThisBeat(beatNumber);
            if (!shouldProcessBeat) return;
            
            // Countdown to next call (very sparse)
            _beatsUntilNextCall -= GetBeatSubdivision();
            
            // Time to sing the melody? (rarely)
            if (_beatsUntilNextCall <= 0)
            {
                StartCoroutine(PlayMelodySequence());
                ResetCallTimer();
            }
        }

        private void StartContinuousMelodyMode()
        {
            if (_isContinuousMode) return; // Already in continuous mode
            
            _isContinuousMode = true;
            
            // Stop any existing melody
            if (_continuousMelodyCoroutine != null)
            {
                StopCoroutine(_continuousMelodyCoroutine);
            }
            
            // Start continuous melody loop
            _continuousMelodyCoroutine = StartCoroutine(ContinuousMelodyLoop());
        }

        private void StopContinuousMelodyMode()
        {
            _isContinuousMode = false;
            
            if (_continuousMelodyCoroutine != null)
            {
                StopCoroutine(_continuousMelodyCoroutine);
                _continuousMelodyCoroutine = null;
            }
            
            // Reset sparse timing
            ResetCallTimer();
        }

        private IEnumerator ContinuousMelodyLoop()
        {
            while (_isContinuousMode)
            {
                // Play the full melody
                yield return StartCoroutine(PlayMelodySequence());
                
                // Brief pause before next loop (based on note duration personality)
                float pauseDuration = GetLoopPauseDuration();
                yield return new WaitForSeconds(pauseDuration);
            }
        }

        private float GetLoopPauseDuration()
        {
            // Different jellies have different loop speeds based on their note duration
            switch (noteDuration)
            {
                case NoteDuration.Whole: return 3f;      // Slow, majestic
                case NoteDuration.Half: return 2f;       // Medium-slow
                case NoteDuration.Quarter: return 1f;    // Regular pace
                case NoteDuration.Eighth: return 0.5f;   // Quick
                case NoteDuration.Sixteenth: return 0.25f; // Very quick
                default: return 1f;
            }
        }

        private bool ShouldProcessThisBeat(int beatNumber)
        {
            switch (noteDuration)
            {
                case NoteDuration.Eighth:
                    // Process every beat (twice as fast as quarter notes)
                    return true;
                    
                case NoteDuration.Sixteenth:
                    // We'll simulate this by processing every beat but with quarter timing
                    // This is a simplification for the MVP
                    return true;
                    
                default:
                    // Whole, Half, Quarter notes process every beat normally
                    return true;
            }
        }

        private float GetBeatSubdivision()
        {
            switch (noteDuration)
            {
                case NoteDuration.Eighth:
                    return 0.5f; // Half beat
                case NoteDuration.Sixteenth:
                    return 0.25f; // Quarter beat
                default:
                    return 1f; // Full beat
            }
        }

        private void AdjustIntervalBasedOnPlayer(bool isPlayerSinging)
        {
            if (isPlayerSinging && !_lastPlayerSingingState)
            {
                // Player just started singing - get more active!
                Debug.Log($"🎤 {gameObject.name} heard the player start singing!");
            }
            
            if (isPlayerSinging)
            {
                // Gradually get more active while player sings
                _currentInterval = Mathf.Lerp(_currentInterval, activeInterval, intervalDecayRate * Time.deltaTime);
            }
            else
            {
                // Gradually return to base interval when player stops
                _currentInterval = Mathf.Lerp(_currentInterval, baseInterval, intervalDecayRate * Time.deltaTime * 0.5f);
            }
            
            _lastPlayerSingingState = isPlayerSinging;
        }

        private IEnumerator PlayMelodySequence()
        {
            if (_isPlayingMelody || _melody == null || _melody.Length == 0) yield break;
            
            _isPlayingMelody = true;
            
            Debug.Log($"🎵 {gameObject.name} starting melody sequence!");
            
            // Play through each note in the melody
            for (int noteIndex = 0; noteIndex < _melody.Length; noteIndex++)
            {
                float baseFrequency = _melody[noteIndex];
                SingNote(baseFrequency, noteIndex);
                
                // Wait for this note to finish before the next one
                // Use a fraction of the synthesizer's call duration so melody plays smoothly
                float noteDuration = (_synthesizer?.CallDuration ?? 2f) / _melody.Length;
                yield return new WaitForSeconds(noteDuration);
            }
            
            _isPlayingMelody = false;
            Debug.Log($"🎵 {gameObject.name} finished melody sequence!");
        }

        private void SingNote(float baseFrequency, int noteIndex = 0)
        {
            if (_synthesizer == null) return;
            
            // Calculate frequency with octave offset
            float octaveMultiplier = Mathf.Pow(2f, octaveOffset);
            float targetFrequency = baseFrequency * octaveMultiplier;
            
            // Add slight pitch variation for voice-like quality
            float pitchDrift = Random.Range(-pitchVariation, pitchVariation);
            float finalFrequency = targetFrequency * (1f + pitchDrift);
            
            // Set synthesizer frequency and trigger
            _synthesizer.CarrierFrequency = finalFrequency;
            _synthesizer.TriggerCall();
            
            // Start visual glow effect
            StartGillGlow(finalFrequency);
            
            string noteName = GetNoteName(finalFrequency);
            Debug.Log($"🎵 {gameObject.name} sings note {noteIndex + 1}: {noteName} ({finalFrequency:F1}Hz)");
        }

        private void RefreshMelodyFromConductor()
        {
            // Get melody from any WhaleConductor in the scene
            WhaleConductor conductor = FindFirstObjectByType<WhaleConductor>();
            if (conductor != null)
            {
                _melody = conductor.Melody;
                Debug.Log($"🎼 {gameObject.name} got melody: {_melody.Length} notes");
            }
            else
            {
                // Fallback default melody if no conductor found
                _melody = new float[] { 261.63f, 329.63f, 392.00f, 261.63f }; // C-E-G-C
                Debug.LogWarning($"🎼 {gameObject.name} no conductor found, using default melody");
            }
        }

        private void ResetCallTimer()
        {
            // Set next call time based on note duration and current interval
            float baseDuration = (int)noteDuration;
            
            // Handle special cases for subdivisions
            if (noteDuration == NoteDuration.Eighth)
                baseDuration = 0.5f;
            else if (noteDuration == NoteDuration.Sixteenth)
                baseDuration = 0.25f;
            
            // Apply current interval multiplier and some randomness
            float randomMultiplier = Random.Range(0.8f, 1.2f);
            _beatsUntilNextCall = (baseDuration * _currentInterval * randomMultiplier);
            
            Debug.Log($"🕐 {gameObject.name} next call in {_beatsUntilNextCall:F1} beats (interval: {_currentInterval:F1})");
        }

        private void RandomizePersonality()
        {
            // Randomize note duration
            NoteDuration[] durations = {NoteDuration.Whole, NoteDuration.Half, NoteDuration.Quarter, NoteDuration.Eighth, NoteDuration.Sixteenth};
            noteDuration = durations[Random.Range(0, durations.Length)];
            
            // Randomize octave within musical range
            octaveOffset = Random.Range(-2, 3); // -2 to +2 octaves
            
            // Slight variation in personality parameters
            baseInterval = Random.Range(20f, 35f); // Very sparse when silent
            pitchVariation = Random.Range(0.01f, 0.04f);
            
            // 🎵 The synthesizer handles its own musical voice setup automatically! 🎵
            
            Debug.Log($"🎭 {gameObject.name} personality: {noteDuration} notes, octave {octaveOffset:+0;-0;0}, intervals {baseInterval:F1}→{activeInterval:F1}");
        }

        private void InitializeGillGlow()
        {
            if (gillSystem != null && gillSystem.gillMaterial != null)
            {
                _uniqueGillMaterial = new Material(gillSystem.gillMaterial);
                gillSystem.gillMaterial = _uniqueGillMaterial;
                Debug.Log($"🌟 {gameObject.name} gill glow system initialized");
            }
            else
            {
                Debug.LogWarning($"⚠️ {gameObject.name}: GillSystem or gillMaterial is missing!");
            }
        }

        private void StartGillGlow(float frequency)
        {
            if (_currentGlowCoroutine != null)
                StopCoroutine(_currentGlowCoroutine);
            
            Color glowColor = GetColorFromFrequency(frequency);
            _currentGlowCoroutine = StartCoroutine(GlowEffect(glowColor));
        }

        private IEnumerator GlowEffect(Color targetColor)
        {
            float elapsed = 0f;
            float duration = _synthesizer?.CallDuration ?? 2f;
            
            while (elapsed < duration + 0.5f)
            {
                float progress = elapsed / duration;
                
                // Create a pulsing glow that follows the call envelope
                float intensity = Mathf.Sin(progress * Mathf.PI) * baseGlowIntensity;
                
                // Apply to gill system
                if (gillSystem != null)
                {
                    for (int i = 0; i < gillSystem.GetGillCount(); i++)
                    {
                        LineRenderer gill = gillSystem.GetGill(i);
                        if (gill != null && gill.material != null)
                        {
                            gill.material.SetFloat(glowPropertyName, intensity);
                            gill.material.SetColor("_Color", targetColor * intensity);
                        }
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Fade out
            if (gillSystem != null)
            {
                for (int i = 0; i < gillSystem.GetGillCount(); i++)
                {
                    LineRenderer gill = gillSystem.GetGill(i);
                    if (gill != null && gill.material != null)
                    {
                        gill.material.SetFloat(glowPropertyName, 0f);
                    }
                }
            }
            
            _currentGlowCoroutine = null;
        }

        private Color GetColorFromFrequency(float frequency)
        {
            // Map frequency to hue - lower frequencies are warmer (red/orange), higher are cooler (blue/purple)
            float normalizedFreq = Mathf.InverseLerp(80f, 800f, frequency);
            float hue = Mathf.Lerp(0f, 0.7f, normalizedFreq); // Red to blue spectrum
            
            return Color.HSVToRGB(hue, 0.8f, 1f);
        }

        private string GetNoteName(float frequency)
        {
            // Simple note name approximation
            float c4 = 261.63f;
            float ratio = frequency / c4;
            float semitones = 12f * Mathf.Log(ratio) / Mathf.Log(2f);
            
            string[] noteNames = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"};
            int noteIndex = Mathf.RoundToInt(semitones) % 12;
            if (noteIndex < 0) noteIndex += 12;
            
            int octave = 4 + Mathf.FloorToInt(semitones / 12f);
            
            return $"{noteNames[noteIndex]}{octave}";
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            // Show continuous mode state
            if (_isContinuousMode)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position + Vector3.up * 3f, 1f);
            }
            
            // Show timing state
            Gizmos.color = _beatsUntilNextCall <= 1f ? Color.yellow : Color.blue;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.5f);
            
            // Show note duration
            Gizmos.color = Color.white;
            float gizmoSize = (float)noteDuration * 0.2f;
            if (noteDuration == NoteDuration.Eighth) gizmoSize = 0.1f;
            if (noteDuration == NoteDuration.Sixteenth) gizmoSize = 0.05f;
            Gizmos.DrawWireSphere(transform.position, gizmoSize + 1f);
        }
    }
}