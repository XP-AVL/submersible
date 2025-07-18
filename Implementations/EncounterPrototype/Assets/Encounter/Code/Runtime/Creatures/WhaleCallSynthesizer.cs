using UnityEngine;

namespace Encounter.Runtime.Creatures
{
    public class WhaleCallSynthesizer : MonoBehaviour
    {
        [Header("Carrier Wave (The Main Voice)")]
        [SerializeField] private float carrierFrequency = 200f;
        [SerializeField] private float callDuration = 5f;
        [SerializeField] private AnimationCurve volumeEnvelope = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
        // Public property so other scripts can access call duration
        public float CallDuration => callDuration;
    
        [Header("Modulator Wave (The Voice Shaper)")]
        [SerializeField] private float modulatorFrequency = 80f;
        [SerializeField] private float modulationDepth = 100f; // How much the modulator affects the carrier
        [SerializeField] private AnimationCurve modulationEnvelope = AnimationCurve.EaseInOut(0, 0.2f, 1, 1);
    
        [Header("Organic Variation")]
        [SerializeField] private float driftAmount = 0.02f; // Natural pitch drift
        [SerializeField] private float breathingRate = 0.5f; // Slow modulation like breathing
        [SerializeField] private AnimationCurve organicCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
        // Expose these publicly so WhaleCallBehavior can randomize them
        public float CarrierFrequency { get => carrierFrequency; set => carrierFrequency = value; }
        public float ModulatorFrequency { get => modulatorFrequency; set => modulatorFrequency = value; }
        public float ModulationDepth { get => modulationDepth; set => modulationDepth = value; }
        public float DriftAmount { get => driftAmount; set => driftAmount = value; }
        public float BreathingRate { get => breathingRate; set => breathingRate = value; }
        public AnimationCurve VolumeEnvelope { get => volumeEnvelope; set => volumeEnvelope = value; }
        public AnimationCurve ModulationEnvelope { get => modulationEnvelope; set => modulationEnvelope = value; }
        public AnimationCurve OrganicCurve { get => organicCurve; set => organicCurve = value; }
    
        private float _sampleRate;
        private float _carrierPhase;
        private float _modulatorPhase;
        private float _organicPhase; // For the slow breathing-like modulation
        private bool _isPlaying;
        private int _sampleIndex;
    
        // Anti-pop protection
        private bool _isFadingOut;
        private bool _isFadingIn;
        private const float FadeInDuration = 0.02f; // 20ms fade in (shorter than fade out)
        private const float FadeOutDuration = 0.05f; // 50ms fade out
        private int _fadeInSamples;
        private int _fadeOutSamples;
        private int _fadeInCounter;
        private int _fadeOutCounter;
    
        private void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            _fadeInSamples = Mathf.RoundToInt(FadeInDuration * _sampleRate);
            _fadeOutSamples = Mathf.RoundToInt(FadeOutDuration * _sampleRate);
            GetComponent<AudioSource>().Play();
        }
    
        public void TriggerCall()
        {
            _isPlaying = true;
            _isFadingOut = false;
            _isFadingIn = true;   // Start with fade in
            _fadeInCounter = 0;
            _fadeOutCounter = 0;
            _sampleIndex = 0;
            _carrierPhase = 0f;
            _modulatorPhase = 0f;
            _organicPhase = Random.Range(0f, 1f); // Start at random point for variation
        }
    
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_isPlaying && !_isFadingOut) 
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0f;
                }
                return;
            }
        
            for (int i = 0; i < data.Length; i += channels)
            {
                // Calculate progress through the call
                float callProgress = _sampleIndex / (_sampleRate * callDuration);
            
                // Check if we need to start fading out
                if (callProgress >= 1f && !_isFadingOut)
                {
                    _isFadingOut = true;
                    _fadeOutCounter = 0;
                }
            
                // Handle fade out to prevent pops
                if (_isFadingOut)
                {
                    if (_fadeOutCounter >= _fadeOutSamples)
                    {
                        // Fade out complete, stop playing
                        _isPlaying = false;
                        _isFadingOut = false;
                        for (int j = i; j < data.Length; j++)
                        {
                            data[j] = 0f;
                        }
                        return;
                    }
                }
            
                // === THE ORGANIC LAYER ===
                // Slow breathing-like variation that evolves over time
                float organicModulation = organicCurve.Evaluate(callProgress) * 
                                          Mathf.Sin(_organicPhase * 2f * Mathf.PI) * driftAmount;
            
                // === THE MODULATOR WAVE ===
                // This wave will distort our main carrier frequency
                float modEnvelope = modulationEnvelope.Evaluate(callProgress);
                float modulatorSample = Mathf.Sin(_modulatorPhase * 2f * Mathf.PI);
            
                // The modulator creates frequency deviation - this is the FM magic!
                float frequencyDeviation = modulatorSample * modulationDepth * modEnvelope;
            
                // === THE CARRIER WAVE ===
                // Our main frequency, but now it's being pushed around by the modulator
                float currentCarrierFreq = carrierFrequency + frequencyDeviation + 
                                           (organicModulation * carrierFrequency);
            
                // Generate the main carrier wave (what we actually hear)
                float carrierSample = Mathf.Sin(_carrierPhase * 2f * Mathf.PI);
            
                // Apply volume envelope
                float volume = volumeEnvelope.Evaluate(Mathf.Clamp01(callProgress));
            
                // Apply fade IN if we're in that phase
                if (_isFadingIn)
                {
                    if (_fadeInCounter >= _fadeInSamples)
                    {
                        _isFadingIn = false; // Fade in complete
                    }
                    else
                    {
                        float fadeInProgress = (float)_fadeInCounter / _fadeInSamples;
                        float fadeInMultiplier = fadeInProgress; // Linear fade from 0 to 1
                        volume *= fadeInMultiplier;
                        _fadeInCounter++;
                    }
                }
            
                // Apply fade OUT if we're in that phase
                if (_isFadingOut)
                {
                    if (_fadeOutCounter >= _fadeOutSamples)
                    {
                        // Fade out complete, stop playing
                        _isPlaying = false;
                        _isFadingOut = false;
                        for (int j = i; j < data.Length; j++)
                        {
                            data[j] = 0f;
                        }
                        return;
                    }
                    else
                    {
                        float fadeOutProgress = (float)_fadeOutCounter / _fadeOutSamples;
                        float fadeOutMultiplier = 1f - fadeOutProgress; // Linear fade from 1 to 0
                        volume *= fadeOutMultiplier;
                        _fadeOutCounter++;
                    }
                }
            
                float finalSample = carrierSample * volume * 0.2f;
            
                // Apply to all channels
                for (int channel = 0; channel < channels; channel++)
                {
                    data[i + channel] = finalSample;
                }
            
                // === ADVANCE ALL OUR PHASES ===
                // Carrier phase advances based on the FM-modulated frequency
                _carrierPhase += currentCarrierFreq / _sampleRate;
                if (_carrierPhase > 1f) _carrierPhase -= 1f;
            
                // Modulator phase advances at its own rate
                _modulatorPhase += modulatorFrequency / _sampleRate;
                if (_modulatorPhase > 1f) _modulatorPhase -= 1f;
            
                // Organic phase advances very slowly for breathing effect
                _organicPhase += breathingRate / _sampleRate;
                if (_organicPhase > 1f) _organicPhase -= 1f;
            
                _sampleIndex++;
            }
        }
    
        // Helper method to create different organic presets
        [ContextMenu("Whale Song Preset")]
        private void SetWhalePreset()
        {
            carrierFrequency = 180f;
            modulatorFrequency = 45f;
            modulationDepth = 120f;
            driftAmount = 0.03f;
            breathingRate = 0.3f;
        }
    
        [ContextMenu("Bell-like Preset")]
        private void SetBellPreset()
        {
            carrierFrequency = 440f;
            modulatorFrequency = 880f;
            modulationDepth = 200f;
            driftAmount = 0.01f;
            breathingRate = 0.1f;
        }
    
        [ContextMenu("Growling Bass Preset")]
        private void SetBassPreset()
        {
            carrierFrequency = 80f;
            modulatorFrequency = 25f;
            modulationDepth = 60f;
            driftAmount = 0.05f;
            breathingRate = 0.8f;
        }
    }
}