using UnityEngine;
using System;
using System.Collections.Generic;

namespace Encounter.Runtime.Audio
{
    public class PitchAnalyzer : MonoBehaviour
    {
        [Header("🔗 Dependencies")]
        [SerializeField] private HumanVoiceDetector voiceDetector;

        [Header("🎵 Pitch Analysis Settings")]
        [SerializeField] private int pitchSmoothingFrames = 5;        // How many frames to average for stable pitch
        [SerializeField] private float pitchChangeThreshold = 5f;     // Hz difference to register as pitch change
        [SerializeField] private float minConfidence = 0.6f;          // Minimum confidence to report pitch
        [SerializeField] private int sampleRate = 44100;              // Should match voice detector
        
        [Header("🎼 Musical Note Settings")]
        [SerializeField] private float a4Frequency = 440f;            // A4 reference frequency
        [SerializeField] private bool useFlats = false;               // Use flats instead of sharps (Bb vs A#)
        
        [Header("🔧 Detection Tuning")]
        [SerializeField] private float fundamentalFreqMin = 80f;      // Same as voice detector
        [SerializeField] private float fundamentalFreqMax = 400f;     // Same as voice detector
        [SerializeField] private float minPitchStability = 0.8f;      // How stable pitch needs to be (0-1)
        
        [Header("🐛 Debug")]
        [SerializeField] private bool debugOutput = true;
        [SerializeField] private bool showDetailedPitch = false;      // Show Hz values and confidence
        
        // Events - how other scripts listen to us!
        public event Action<float, string, int> OnPitchDetected;      // frequency, note name, octave
        public event Action<float, string, int> OnPitchChanged;       // New pitch after change
        public event Action<float, string, int> OnStablePitchHeld;    // Pitch held steadily
        public event Action OnPitchLost;                              // No reliable pitch detected
        
        
        
        // Pitch tracking
        private Queue<float> _recentPitches = new Queue<float>();
        private float _currentStablePitch = 0f;
        private float _lastReportedPitch = 0f;
        private string _currentNoteName = "";
        private int _currentOctave = 0;
        private float _pitchConfidence = 0f;
        
        // Stability tracking
        private float _stablePitchStartTime = -1f;
        private bool _hasStablePitch = false;
        
        // Note names for conversion
        private readonly string[] _noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        private readonly string[] _noteNamesFlat = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
        
        // Properties for other scripts
        public float CurrentPitch => _currentStablePitch;
        public string CurrentNoteName => _currentNoteName;
        public int CurrentOctave => _currentOctave;
        public float PitchConfidence => _pitchConfidence;
        public bool HasStablePitch => _hasStablePitch;
        public bool IsAnalyzing { get; private set; }
        
        private void Start()
        {
            // Find and connect to the voice detector
            if (voiceDetector == null)
            {
                // Fallback to finding it
                voiceDetector = FindAnyObjectByType<HumanVoiceDetector>();
            }
            
            if (voiceDetector == null)
            {
                Debug.LogError("❌ PitchAnalyzer needs a HumanVoiceDetector!");
                enabled = false;
                return;
            }
            
            // Subscribe to voice detection events
            voiceDetector.OnHumanVoiceDetected += AnalyzeVoicePitch;
            voiceDetector.OnHumanVoiceStopped += HandleVoiceStopped;
            
            Debug.Log("🎵 Pitch Analyzer initialized!");
            Debug.Log($"   Smoothing over {pitchSmoothingFrames} frames");
            Debug.Log($"   Using {(useFlats ? "flats" : "sharps")} for note names");
            Debug.Log($"   A4 = {a4Frequency}Hz");
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (voiceDetector != null)
            {
                voiceDetector.OnHumanVoiceDetected -= AnalyzeVoicePitch;
                voiceDetector.OnHumanVoiceStopped -= HandleVoiceStopped;
            }
        }
        
        /// <summary>
        /// 🎤 Main analysis method - called when voice detector finds human voice
        /// </summary>
        private void AnalyzeVoicePitch(float[] audioData, float volume)
        {
            IsAnalyzing = true;
            
            // Step 1: Extract raw pitch from audio data
            float rawPitch = EstimatePitch(audioData);
            
            if (rawPitch <= 0 || rawPitch < fundamentalFreqMin || rawPitch > fundamentalFreqMax)
            {
                // No valid pitch detected
                HandleInvalidPitch();
                return;
            }
            
            // Step 2: Add to smoothing buffer
            _recentPitches.Enqueue(rawPitch);
            while (_recentPitches.Count > pitchSmoothingFrames)
            {
                _recentPitches.Dequeue();
            }
            
            // Step 3: Get smoothed, stable pitch
            float smoothedPitch = GetStablePitch();
            float pitchStability = CalculatePitchStability();
            
            // Step 4: Calculate confidence based on stability and consistency
            _pitchConfidence = CalculatePitchConfidence(pitchStability, volume);
            
            if (_pitchConfidence < minConfidence)
            {
                HandleInvalidPitch();
                return;
            }
            
            // Step 5: Convert to musical note
            var (noteName, octave) = FrequencyToNote(smoothedPitch);
            
            // Step 6: Check for pitch changes and stability
            bool pitchChanged = Mathf.Abs(smoothedPitch - _lastReportedPitch) > pitchChangeThreshold;
            
            if (pitchChanged || _currentStablePitch == 0)
            {
                // New pitch detected!
                _currentStablePitch = smoothedPitch;
                _currentNoteName = noteName;
                _currentOctave = octave;
                _lastReportedPitch = smoothedPitch;
                _stablePitchStartTime = Time.time;
                _hasStablePitch = false;
                
                if (pitchChanged && _currentStablePitch > 0)
                {
                    OnPitchChanged?.Invoke(smoothedPitch, noteName, octave);
                    
                    if (debugOutput)
                    {
                        Debug.Log($"🎵 Pitch changed to: {noteName}{octave} ({smoothedPitch:F1}Hz, confidence: {_pitchConfidence:F2})");
                    }
                }
                else
                {
                    OnPitchDetected?.Invoke(smoothedPitch, noteName, octave);
                    
                    if (debugOutput)
                    {
                        Debug.Log($"🎵 Pitch detected: {noteName}{octave} ({smoothedPitch:F1}Hz, confidence: {_pitchConfidence:F2})");
                    }
                }
            }
            else if (pitchStability >= minPitchStability && !_hasStablePitch)
            {
                // Pitch is now stable!
                _hasStablePitch = true;
                OnStablePitchHeld?.Invoke(smoothedPitch, noteName, octave);
                
                if (debugOutput)
                {
                    Debug.Log($"🎯 Stable pitch: {noteName}{octave} ({smoothedPitch:F1}Hz) held for {Time.time - _stablePitchStartTime:F1}s");
                }
            }
            
            // Debug detailed output
            if (showDetailedPitch && Time.time % 0.2f < 0.1f)
            {
                Debug.Log($"🔍 Raw: {rawPitch:F1}Hz | Smooth: {smoothedPitch:F1}Hz | Stability: {pitchStability:F2} | Conf: {_pitchConfidence:F2}");
            }
        }
        
        /// <summary>
        /// 🎯 Enhanced pitch detection using peak-finding (from your original code!)
        /// </summary>
        private float EstimatePitch(float[] audioData)
        {
            // Find significant peaks in the waveform
            var peakPositions = new List<int>();
            
            // Calculate dynamic threshold based on signal energy
            float avgAmplitude = 0;
            for (int i = 0; i < audioData.Length; i++)
            {
                avgAmplitude += Mathf.Abs(audioData[i]);
            }
            avgAmplitude /= audioData.Length;
            
            float peakThreshold = avgAmplitude * 0.3f;
            
            // Find peaks that are significant
            for (int i = 1; i < audioData.Length - 1; i++)
            {
                if (audioData[i] > audioData[i-1] && 
                    audioData[i] > audioData[i+1] && 
                    audioData[i] > peakThreshold)
                {
                    peakPositions.Add(i);
                }
            }
            
            if (peakPositions.Count < 2) return 0f;
            
            // Calculate average period between peaks
            float totalPeriod = 0f;
            int validPeriods = 0;
            
            for (int i = 1; i < peakPositions.Count; i++)
            {
                int period = peakPositions[i] - peakPositions[i-1];
                
                // Sanity check for human vocal range
                if (period > sampleRate / fundamentalFreqMax && period < sampleRate / fundamentalFreqMin)
                {
                    totalPeriod += period;
                    validPeriods++;
                }
            }
            
            if (validPeriods == 0) return 0f;
            
            float averagePeriod = totalPeriod / validPeriods;
            return sampleRate / averagePeriod;
        }
        
        /// <summary>
        /// 📊 Get stable pitch using median (more stable than average!)
        /// </summary>
        private float GetStablePitch()
        {
            if (_recentPitches.Count == 0) return 0f;
            
            // Convert to array and sort for median
            var pitchArray = new float[_recentPitches.Count];
            _recentPitches.CopyTo(pitchArray, 0);
            Array.Sort(pitchArray);
            
            // Return median value (ignores outliers!)
            return pitchArray[pitchArray.Length / 2];
        }
        
        /// <summary>
        /// 🎯 Calculate how stable the pitch is (0 = very unstable, 1 = rock solid)
        /// </summary>
        private float CalculatePitchStability()
        {
            if (_recentPitches.Count < 2) return 0f;
            
            // Calculate variance of recent pitches
            float mean = 0f;
            foreach (float pitch in _recentPitches)
            {
                mean += pitch;
            }
            mean /= _recentPitches.Count;
            
            float variance = 0f;
            foreach (float pitch in _recentPitches)
            {
                float diff = pitch - mean;
                variance += diff * diff;
            }
            variance /= _recentPitches.Count;
            
            float standardDeviation = Mathf.Sqrt(variance);
            
            // Convert to stability score (lower variance = higher stability)
            // A standard deviation of 10Hz or less = very stable
            float stability = Mathf.Clamp01(1f - (standardDeviation / 10f));
            
            return stability;
        }
        
        /// <summary>
        /// 💪 Calculate overall confidence in this pitch reading
        /// </summary>
        private float CalculatePitchConfidence(float stability, float volume)
        {
            // Base confidence on pitch stability
            float confidence = stability * 0.7f;
            
            // Add volume factor (louder = more confident)
            float volumeFactor = Mathf.Clamp01(volume / 0.05f); // Normalize to reasonable range
            confidence += volumeFactor * 0.3f;
            
            return Mathf.Clamp01(confidence);
        }
        
        /// <summary>
        /// 🎼 Convert frequency to musical note name and octave
        /// </summary>
        private (string noteName, int octave) FrequencyToNote(float frequency)
        {
            // Calculate how many semitones above/below A4 (440Hz)
            float semitonesFromA4 = 12f * Mathf.Log(frequency / a4Frequency) / Mathf.Log(2f);
            
            // Round to nearest semitone
            int semitones = Mathf.RoundToInt(semitonesFromA4);
            
            // A4 is note index 9 (A), octave 4
            int noteIndex = (9 + semitones) % 12;
            if (noteIndex < 0) noteIndex += 12; // Handle negative modulo
            
            int octave = 4 + (9 + semitones) / 12;
            
            // Get note name
            string[] noteArray = useFlats ? _noteNamesFlat : _noteNames;
            string noteName = noteArray[noteIndex];
            
            return (noteName, octave);
        }
        
        /// <summary>
        /// ❌ Handle when we can't detect valid pitch
        /// </summary>
        private void HandleInvalidPitch()
        {
            if (_hasStablePitch || _currentStablePitch > 0)
            {
                // We had a pitch, now we lost it
                _currentStablePitch = 0f;
                _currentNoteName = "";
                _currentOctave = 0;
                _hasStablePitch = false;
                _pitchConfidence = 0f;
                _recentPitches.Clear();
                
                OnPitchLost?.Invoke();
                
                if (debugOutput)
                {
                    Debug.Log("🔇 Pitch lost - no reliable reading");
                }
            }
        }
        
        /// <summary>
        /// 🛑 Called when voice detector stops detecting voice
        /// </summary>
        private void HandleVoiceStopped()
        {
            IsAnalyzing = false;
            HandleInvalidPitch();
        }
        
        /// <summary>
        /// 🎵 Public method to get the current pitch as a musical interval from a reference
        /// </summary>
        public float GetIntervalFromFrequency(float referenceFrequency)
        {
            if (_currentStablePitch <= 0 || referenceFrequency <= 0) return 0f;
            
            // Calculate interval in semitones
            float ratio = _currentStablePitch / referenceFrequency;
            return 12f * Mathf.Log(ratio) / Mathf.Log(2f);
        }
        
        /// <summary>
        /// 🎯 Check if current pitch matches a target frequency within tolerance
        /// </summary>
        public bool IsPitchNear(float targetFrequency, float toleranceInSemitones = 0.5f)
        {
            if (_currentStablePitch <= 0 || !_hasStablePitch) return false;
            
            float interval = GetIntervalFromFrequency(targetFrequency);
            return Mathf.Abs(interval) <= toleranceInSemitones;
        }
        
        // 🎨 Debug GUI
        private void OnGUI()
        {
            if (!debugOutput) return;
            
            GUI.Box(new Rect(10, 130, 320, 140), "🎵 Pitch Analyzer");
            
            GUI.Label(new Rect(20, 155, 280, 20), $"Analyzing: {(IsAnalyzing ? "YES" : "NO")}");
            GUI.Label(new Rect(20, 175, 280, 20), $"Current Note: {(_currentStablePitch > 0 ? $"{_currentNoteName}{_currentOctave}" : "None")}");
            GUI.Label(new Rect(20, 195, 280, 20), $"Frequency: {(_currentStablePitch > 0 ? $"{_currentStablePitch:F1} Hz" : "N/A")}");
            GUI.Label(new Rect(20, 215, 280, 20), $"Confidence: {_pitchConfidence:F2} (need >{minConfidence:F2})");
            GUI.Label(new Rect(20, 235, 280, 20), $"Stable: {(_hasStablePitch ? "✅ YES" : "❌ NO")}");
            GUI.Label(new Rect(20, 255, 280, 20), $"Smoothing: {_recentPitches.Count}/{pitchSmoothingFrames} frames");
        }
    }
}