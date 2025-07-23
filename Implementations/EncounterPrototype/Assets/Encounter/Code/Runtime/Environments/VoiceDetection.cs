using UnityEngine;
using System;

namespace Encounter.Runtime.Audio
{
    public class HumanVoiceDetector : MonoBehaviour
    {
        [Header("🎤 Microphone Setup")]
        [SerializeField] private float volumeThreshold = 0.02f;        // How loud the mic needs to be
        [SerializeField] private int sampleRate = 44100;              // Audio sample rate
        [SerializeField] private float updateRate = 0.1f;             // How often to check mic (10 times per second)
        [SerializeField] private int analysisWindowSize = 512;        // Smaller window for faster response
        
        [Header("🗣️ Voice Detection - The Big Three")]
        [SerializeField] private float noiseThreshold = 0.15f;        // How "noisy" the sound needs to be (MAIN TEST)
        [SerializeField] private float fundamentalFreqMin = 80f;      // Human voice fundamental range
        [SerializeField] private float fundamentalFreqMax = 400f;     // Human voice fundamental range
        [SerializeField] private float confidenceThreshold = 0.7f;    // How confident we need to be (0-1)
        
        [Header("🔧 Stability & Tuning")]
        [SerializeField] private int stabilityFrames = 3;             // How many frames to smooth over
        [SerializeField] private float noiseWeight = 0.5f;            // How much we trust the noise test (MAIN)
        [SerializeField] private float frequencyWeight = 0.3f;        // How much we trust frequency range
        [SerializeField] private float energyWeight = 0.2f;           // How much we trust energy level
        
        [Header("🐛 Debug")]
        [SerializeField] private bool debugOutput = true;             // Show detection events
        [SerializeField] private bool showDetailedMetrics = false;    // Show all the numbers
        
        // Events - this is how other scripts will listen to us!
        public event Action<float[], float> OnHumanVoiceDetected;     // Sends audio data + RMS volume
        public event Action OnHumanVoiceStopped;
        public event Action<float[], float> OnNonHumanAudioDetected;  // For whale sounds, etc.
        
        // Microphone management
        private AudioClip _microphoneClip;
        private string _microphoneName;
        private bool _isListening;
        private float _lastUpdateTime;
        
        // Audio analysis buffers
        private float[] _analysisBuffer;
        
        // Detection state
        private bool _wasDetectingVoice = false;
        private float[] _recentConfidenceScores;
        private int _confidenceIndex = 0;
        
        // Properties for other scripts to check
        public bool IsDetectingHumanVoice { get; private set; }
        public float CurrentVolume { get; private set; }
        public float CurrentConfidence { get; private set; }
        public bool IsMicrophoneActive => _isListening && _microphoneClip != null;
        
        private void Start()
        {
            InitializeMicrophone();
            InitializeAnalysis();
            
            Debug.Log("🎤 Human Voice Detector initialized! (Lean & Mean Edition)");
            Debug.Log($"   The Big Three: Noise({noiseWeight*100}%) + Frequency({frequencyWeight*100}%) + Energy({energyWeight*100}%)");
            Debug.Log($"   Noise threshold: {noiseThreshold} | Frequency: {fundamentalFreqMin}-{fundamentalFreqMax}Hz");
        }
        
        private void Update()
        {
            if (Time.time - _lastUpdateTime >= updateRate)
            {
                AnalyzeMicrophoneInput();
                _lastUpdateTime = Time.time;
            }
        }
        
        private void InitializeMicrophone()
        {
            if (Microphone.devices.Length > 0)
            {
                _microphoneName = Microphone.devices[0];
                Debug.Log($"🎤 Using microphone: {_microphoneName}");
                
                _microphoneClip = Microphone.Start(_microphoneName, true, 1, sampleRate);
                _isListening = true;
            }
            else
            {
                Debug.LogError("❌ No microphone detected! Voice detection disabled.");
                enabled = false;
            }
        }
        
        private void InitializeAnalysis()
        {
            _analysisBuffer = new float[analysisWindowSize];
            _recentConfidenceScores = new float[stabilityFrames];
            
            // Initialize confidence array
            for (int i = 0; i < stabilityFrames; i++)
            {
                _recentConfidenceScores[i] = 0f;
            }
        }
        
        private void AnalyzeMicrophoneInput()
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
            
            // Calculate basic audio properties
            float rms = CalculateRms(_analysisBuffer);
            CurrentVolume = rms;
            
            // Only analyze if we have enough volume
            if (rms >= volumeThreshold)
            {
                // THE BIG THREE TESTS!
                float confidence = CalculateVoiceConfidence(_analysisBuffer, rms);
                UpdateConfidenceHistory(confidence);
                
                // Use stabilized confidence for final decision
                float stabilizedConfidence = GetStabilizedConfidence();
                CurrentConfidence = stabilizedConfidence;
                
                bool isHumanVoice = stabilizedConfidence >= confidenceThreshold;
                
                // Handle voice detection state changes
                if (isHumanVoice && !_wasDetectingVoice)
                {
                    // Started detecting voice!
                    IsDetectingHumanVoice = true;
                    _wasDetectingVoice = true;
                    OnHumanVoiceDetected?.Invoke(_analysisBuffer, rms);
                    
                    if (debugOutput)
                    {
                        Debug.Log($"🗣️ Human voice detected! (Confidence: {stabilizedConfidence:F2})");
                    }
                }
                else if (isHumanVoice && _wasDetectingVoice)
                {
                    // Continue detecting voice
                    IsDetectingHumanVoice = true;
                    OnHumanVoiceDetected?.Invoke(_analysisBuffer, rms);
                }
                else if (!isHumanVoice && _wasDetectingVoice)
                {
                    // Stopped detecting voice
                    IsDetectingHumanVoice = false;
                    _wasDetectingVoice = false;
                    OnHumanVoiceStopped?.Invoke();
                    
                    if (debugOutput)
                    {
                        Debug.Log($"❌ Human voice lost (Confidence: {stabilizedConfidence:F2})");
                    }
                }
                else if (!isHumanVoice)
                {
                    // Not human voice, but has volume - might be whale or other sound
                    IsDetectingHumanVoice = false;
                    OnNonHumanAudioDetected?.Invoke(_analysisBuffer, rms);
                }
            }
            else
            {
                // Volume too low - reset everything
                if (_wasDetectingVoice)
                {
                    IsDetectingHumanVoice = false;
                    _wasDetectingVoice = false;
                    OnHumanVoiceStopped?.Invoke();
                    
                    if (debugOutput)
                    {
                        Debug.Log("🔇 Voice lost - volume too low");
                    }
                }
                
                CurrentConfidence = 0f;
                UpdateConfidenceHistory(0f);
            }
            
            // Debug output for tuning
            if (showDetailedMetrics && Time.time % 0.5f < updateRate)
            {
                Debug.Log($"🎤 Vol: {rms:F4} | Conf: {CurrentConfidence:F3} | Human: {IsDetectingHumanVoice}");
            }
        }
        
        /// <summary>
        /// 🧠 THE BIG THREE! Simple but effective voice detection
        /// </summary>
        private float CalculateVoiceConfidence(float[] audioData, float rms)
        {
            // Test 1: NOISE LEVEL (the star of the show!)
            // Human voices are messy, FM synths are perfect
            float noiseScore = CalculateNoiseScore(audioData);
            
            // Test 2: FREQUENCY RANGE
            // Is the pitch in human vocal range?
            float frequencyScore = CalculateFrequencyScore(audioData);
            
            // Test 3: ENERGY LEVEL  
            // Just making sure we have real signal
            float energyScore = Mathf.Clamp01(rms / volumeThreshold);
            
            // Combine with weights
            float totalConfidence = (noiseScore * noiseWeight) + 
                                   (frequencyScore * frequencyWeight) + 
                                   (energyScore * energyWeight);
            
            if (showDetailedMetrics && Time.time % 1f < updateRate)
            {
                Debug.Log($"🔍 Noise:{noiseScore:F2} Freq:{frequencyScore:F2} Energy:{energyScore:F2} = {totalConfidence:F2}");
            }
            
            return Mathf.Clamp01(totalConfidence);
        }
        
        /// <summary>
        /// 🌊 The main event! Measures how "noisy" vs "pure" the signal is
        /// </summary>
        private float CalculateNoiseScore(float[] audioData)
        {
            float totalVariation = 0f;
            float totalEnergy = 0f;
            
            // Look at how much each sample deviates from a smooth trend
            // This is what separates human voice from FM synthesis!
            for (int i = 2; i < audioData.Length - 2; i++)
            {
                // 5-point moving average for smoothness comparison
                float smoothed = (audioData[i-2] + audioData[i-1] + audioData[i] + audioData[i+1] + audioData[i+2]) / 5f;
                float deviation = Mathf.Abs(audioData[i] - smoothed);
                
                totalVariation += deviation;
                totalEnergy += Mathf.Abs(audioData[i]);
            }
            
            if (totalEnergy < 0.001f) return 0f;
            
            float noiseLevel = totalVariation / totalEnergy;
            
            // Convert to score: more noise = more human-like
            // This is the magic threshold that separates voice from FM!
            return Mathf.Clamp01(noiseLevel / noiseThreshold);
        }
        
        /// <summary>
        /// 🎵 Simple frequency range check - is this in human voice territory?
        /// </summary>
        private float CalculateFrequencyScore(float[] audioData)
        {
            float dominantFreq = EstimatePitch(audioData);
            
            if (dominantFreq <= 0) return 0.5f; // Neutral if we can't detect pitch
            
            // Perfect score if in range
            if (dominantFreq >= fundamentalFreqMin && dominantFreq <= fundamentalFreqMax)
            {
                return 1f;
            }
            
            // Gradual falloff outside range
            if (dominantFreq < fundamentalFreqMin)
            {
                float ratio = dominantFreq / fundamentalFreqMin;
                return Mathf.Clamp01(ratio);
            }
            else
            {
                float ratio = fundamentalFreqMax / dominantFreq;
                return Mathf.Clamp01(ratio);
            }
        }
        
        /// <summary>
        /// 🎯 Your enhanced pitch detection - keeping this exactly as it was!
        /// </summary>
        private float EstimatePitch(float[] audioData)
        {
            // Find significant peaks in the waveform
            var peakPositions = new System.Collections.Generic.List<int>();
            
            // Calculate dynamic threshold based on signal energy
            float avgAmplitude = 0;
            for (int i = 0; i < audioData.Length; i++)
            {
                avgAmplitude += Mathf.Abs(audioData[i]);
            }
            avgAmplitude /= audioData.Length;
            
            float peakThreshold = avgAmplitude * 0.3f;
            
            // Find peaks
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
        
        private void UpdateConfidenceHistory(float confidence)
        {
            _recentConfidenceScores[_confidenceIndex] = confidence;
            _confidenceIndex = (_confidenceIndex + 1) % stabilityFrames;
        }
        
        private float GetStabilizedConfidence()
        {
            // Use median of recent scores to avoid brief glitches
            var sortedScores = new float[stabilityFrames];
            Array.Copy(_recentConfidenceScores, sortedScores, stabilityFrames);
            Array.Sort(sortedScores);
            
            return sortedScores[stabilityFrames / 2]; // Median value
        }
        
        private float CalculateRms(float[] audioData)
        {
            float sum = 0f;
            foreach (var sample in audioData)
            {
                sum += sample * sample;
            }
            return Mathf.Sqrt(sum / audioData.Length);
        }
        
        private void OnDestroy()
        {
            if (_isListening && !string.IsNullOrEmpty(_microphoneName))
            {
                Microphone.End(_microphoneName);
            }
        }
        
        // 🎨 Focused debug GUI
        private void OnGUI()
        {
            if (!debugOutput) return;
            
            GUI.Box(new Rect(10, 10, 320, 100), "🎤 Human Voice Detector (Big Three)");
            
            GUI.Label(new Rect(20, 35, 280, 20), $"Microphone: {(_isListening ? "Active" : "Inactive")}");
            GUI.Label(new Rect(20, 55, 280, 20), $"Confidence: {CurrentConfidence:F3} (need >{confidenceThreshold:F3})");
            GUI.Label(new Rect(20, 75, 280, 20), $"Human Voice: {(IsDetectingHumanVoice ? "✅ YES" : "❌ NO")}");
        }
    }
}