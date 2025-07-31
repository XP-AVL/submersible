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
    
        [Header("🎵 Musical FM Voice Settings")]
        [SerializeField] private MusicalVoiceType voiceType = MusicalVoiceType.Random;
        [SerializeField] private bool randomizeOnStart = true;
        
        [Header("Advanced FM Parameters (Auto-set by Voice Type)")]
        [SerializeField] private float modulatorFrequency = 80f;
        [SerializeField] private float modulationDepth = 100f;
        [SerializeField] private AnimationCurve modulationEnvelope = AnimationCurve.EaseInOut(0, 0.2f, 1, 1);
    
        [Header("Organic Variation")]
        [SerializeField] private float driftAmount = 0.02f;
        [SerializeField] private float breathingRate = 0.5f;
        [SerializeField] private AnimationCurve organicCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
        // Expose these publicly so WhaleCallBehavior can set frequency
        public float CarrierFrequency { 
            get => carrierFrequency; 
            set { 
                carrierFrequency = value;
                UpdateModulatorForMusicalRatio(); // Keep musical ratios when frequency changes!
            } 
        }
        
        // Keep other properties for backward compatibility
        public float ModulatorFrequency { get => modulatorFrequency; set => modulatorFrequency = value; }
        public float ModulationDepth { get => modulationDepth; set => modulationDepth = value; }
        public float DriftAmount { get => driftAmount; set => driftAmount = value; }
        public float BreathingRate { get => breathingRate; set => breathingRate = value; }
        public AnimationCurve VolumeEnvelope { get => volumeEnvelope; set => volumeEnvelope = value; }
        public AnimationCurve ModulationEnvelope { get => modulationEnvelope; set => modulationEnvelope = value; }
        public AnimationCurve OrganicCurve { get => organicCurve; set => organicCurve = value; }
    
        // Musical voice system
        private MusicalVoicePreset currentVoicePreset;
        private float baseModulatorRatio;
        
        private float _sampleRate;
        private float _carrierPhase;
        private float _modulatorPhase;
        private float _organicPhase;
        private bool _isPlaying;
        private int _sampleIndex;
    
        // Anti-pop protection
        private bool _isFadingOut;
        private bool _isFadingIn;
        private const float FadeInDuration = 0.02f;
        private const float FadeOutDuration = 0.05f;
        private int _fadeInSamples;
        private int _fadeOutSamples;
        private int _fadeInCounter;
        private int _fadeOutCounter;

        private void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            _fadeInSamples = Mathf.RoundToInt(FadeInDuration * _sampleRate);
            _fadeOutSamples = Mathf.RoundToInt(FadeOutDuration * _sampleRate);
            
            // Setup musical voice
            if (randomizeOnStart)
            {
                SetupRandomMusicalVoice();
            }
            else
            {
                SetupMusicalVoice(voiceType);
            }
            
            GetComponent<AudioSource>().Play();
        }

        public void SetupRandomMusicalVoice()
        {
            // Pick a random voice type (excluding Random)
            MusicalVoiceType[] types = {
                MusicalVoiceType.Flute, MusicalVoiceType.Bell, MusicalVoiceType.Brass,
                MusicalVoiceType.Ethereal, MusicalVoiceType.Woody, MusicalVoiceType.Crystal,
                MusicalVoiceType.Oceanic
            };
            
            MusicalVoiceType randomType = types[Random.Range(0, types.Length)];
            SetupMusicalVoice(randomType);
        }

        public void SetupMusicalVoice(MusicalVoiceType type)
        {
            currentVoicePreset = GetVoicePreset(type);
            
            // Apply the musical preset
            baseModulatorRatio = currentVoicePreset.modulatorRatio;
            modulationDepth = currentVoicePreset.baseModDepth * Random.Range(0.8f, 1.2f);
            driftAmount = currentVoicePreset.driftAmount * Random.Range(0.7f, 1.3f);
            breathingRate = currentVoicePreset.breathingRate * Random.Range(0.8f, 1.2f);
            
            // Update modulator frequency based on current carrier
            UpdateModulatorForMusicalRatio();
            
            // Create musical envelopes
            CreateMusicalEnvelopes(currentVoicePreset);
            
            Debug.Log($"🎵 {gameObject.name} voice: {currentVoicePreset.name} (ratio: {baseModulatorRatio:F2}, depth: {modulationDepth:F1})");
        }

        private void UpdateModulatorForMusicalRatio()
        {
            if (currentVoicePreset != null)
            {
                // Keep musical relationship between carrier and modulator
                modulatorFrequency = carrierFrequency * baseModulatorRatio;
            }
        }

        private MusicalVoicePreset GetVoicePreset(MusicalVoiceType type)
        {
            switch (type)
            {
                case MusicalVoiceType.Flute:
                    return new MusicalVoicePreset("Flute", 1f, 30f, 0.01f, 0.2f);
                    
                case MusicalVoiceType.Bell:
                    return new MusicalVoicePreset("Bell", 2f, 80f, 0.005f, 0.1f);
                    
                case MusicalVoiceType.Brass:
                    return new MusicalVoicePreset("Brass", 1.5f, 60f, 0.02f, 0.3f);
                    
                case MusicalVoiceType.Ethereal:
                    return new MusicalVoicePreset("Ethereal", 0.5f, 20f, 0.015f, 0.25f);
                    
                case MusicalVoiceType.Woody:
                    return new MusicalVoicePreset("Woody", 1.33f, 45f, 0.025f, 0.4f);
                    
                case MusicalVoiceType.Crystal:
                    return new MusicalVoicePreset("Crystal", 3f, 25f, 0.008f, 0.15f);
                    
                case MusicalVoiceType.Oceanic:
                    return new MusicalVoicePreset("Oceanic", 0.75f, 70f, 0.03f, 0.6f);
                    
                default:
                    return new MusicalVoicePreset("Default", 1f, 50f, 0.02f, 0.3f);
            }
        }

        private void CreateMusicalEnvelopes(MusicalVoicePreset preset)
        {
            // Volume envelope - different shapes for different instruments
            AnimationCurve volumeCurve = new AnimationCurve();
            
            switch (preset.name)
            {
                case "Bell":
                    // Sharp attack, long decay like a bell
                    volumeCurve.AddKey(0f, 0f);
                    volumeCurve.AddKey(0.02f, 1f);
                    volumeCurve.AddKey(0.3f, 0.6f);
                    volumeCurve.AddKey(1f, 0f);
                    break;
                    
                case "Flute":
                    // Smooth attack and release
                    volumeCurve.AddKey(0f, 0f);
                    volumeCurve.AddKey(0.1f, 0.8f);
                    volumeCurve.AddKey(0.8f, 0.7f);
                    volumeCurve.AddKey(1f, 0f);
                    break;
                    
                case "Brass":
                    // Strong attack, sustained body
                    volumeCurve.AddKey(0f, 0f);
                    volumeCurve.AddKey(0.05f, 1f);
                    volumeCurve.AddKey(0.7f, 0.9f);
                    volumeCurve.AddKey(1f, 0f);
                    break;
                    
                case "Crystal":
                    // Very pure, sustained tone
                    volumeCurve.AddKey(0f, 0f);
                    volumeCurve.AddKey(0.08f, 1f);
                    volumeCurve.AddKey(0.9f, 0.95f);
                    volumeCurve.AddKey(1f, 0f);
                    break;
                    
                default:
                    // Default smooth envelope
                    volumeCurve.AddKey(0f, 0f);
                    volumeCurve.AddKey(0.1f, 1f);
                    volumeCurve.AddKey(0.9f, 0.8f);
                    volumeCurve.AddKey(1f, 0f);
                    break;
            }
            
            // Smooth all keyframes
            for (int i = 0; i < volumeCurve.length; i++)
                volumeCurve.SmoothTangents(i, 0.3f);
            
            volumeEnvelope = volumeCurve;
            
            // Modulation envelope - controls how the FM effect changes over time
            AnimationCurve modCurve = new AnimationCurve();
            modCurve.AddKey(0f, 0.2f);
            modCurve.AddKey(0.3f, 1f);
            modCurve.AddKey(0.8f, 0.6f);
            modCurve.AddKey(1f, 0.3f);
            
            for (int i = 0; i < modCurve.length; i++)
                modCurve.SmoothTangents(i, 0.3f);
                
            modulationEnvelope = modCurve;
            
            // Organic curve for natural pitch variation
            AnimationCurve organicCurveNew = new AnimationCurve();
            organicCurveNew.AddKey(0f, Random.Range(-0.5f, 0.5f));
            organicCurveNew.AddKey(0.3f, Random.Range(-0.8f, 0.8f));
            organicCurveNew.AddKey(0.7f, Random.Range(-0.6f, 0.6f));
            organicCurveNew.AddKey(1f, Random.Range(-0.3f, 0.3f));
            
            for (int i = 0; i < organicCurveNew.length; i++)
                organicCurveNew.SmoothTangents(i, 0.5f);
                
            organicCurve = organicCurveNew;
        }

        public void TriggerCall()
        {
            _isPlaying = true;
            _isFadingOut = false;
            _isFadingIn = true;
            _fadeInCounter = 0;
            _fadeOutCounter = 0;
            _sampleIndex = 0;
            _carrierPhase = 0f;
            _modulatorPhase = 0f;
            _organicPhase = Random.Range(0f, 1f);
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
                float organicModulation = organicCurve.Evaluate(callProgress) * 
                                          Mathf.Sin(_organicPhase * 2f * Mathf.PI) * driftAmount;
            
                // === THE MODULATOR WAVE ===
                float modEnvelope = modulationEnvelope.Evaluate(callProgress);
                float modulatorSample = Mathf.Sin(_modulatorPhase * 2f * Mathf.PI);
            
                // The modulator creates frequency deviation
                float frequencyDeviation = modulatorSample * modulationDepth * modEnvelope;
            
                // === THE CARRIER WAVE ===
                float currentCarrierFreq = carrierFrequency + frequencyDeviation + 
                                           (organicModulation * carrierFrequency);
            
                float carrierSample = Mathf.Sin(_carrierPhase * 2f * Mathf.PI);
            
                // Apply volume envelope
                float volume = volumeEnvelope.Evaluate(Mathf.Clamp01(callProgress));
            
                // Apply fade IN
                if (_isFadingIn)
                {
                    if (_fadeInCounter >= _fadeInSamples)
                    {
                        _isFadingIn = false;
                    }
                    else
                    {
                        float fadeInProgress = (float)_fadeInCounter / _fadeInSamples;
                        volume *= fadeInProgress;
                        _fadeInCounter++;
                    }
                }
            
                // Apply fade OUT
                if (_isFadingOut)
                {
                    if (_fadeOutCounter >= _fadeOutSamples)
                    {
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
                        volume *= (1f - fadeOutProgress);
                        _fadeOutCounter++;
                    }
                }
            
                float finalSample = carrierSample * volume * 0.2f;
            
                // Apply to all channels
                for (int channel = 0; channel < channels; channel++)
                {
                    data[i + channel] = finalSample;
                }
            
                // === ADVANCE ALL PHASES ===
                _carrierPhase += currentCarrierFreq / _sampleRate;
                if (_carrierPhase > 1f) _carrierPhase -= 1f;
            
                _modulatorPhase += modulatorFrequency / _sampleRate;
                if (_modulatorPhase > 1f) _modulatorPhase -= 1f;
            
                _organicPhase += breathingRate / _sampleRate;
                if (_organicPhase > 1f) _organicPhase -= 1f;
            
                _sampleIndex++;
            }
        }

        // Context menu methods for testing different voices
        [ContextMenu("🪈 Set Flute Voice")]
        private void SetFluteVoice() => SetupMusicalVoice(MusicalVoiceType.Flute);
        
        [ContextMenu("🔔 Set Bell Voice")]
        private void SetBellVoice() => SetupMusicalVoice(MusicalVoiceType.Bell);
        
        [ContextMenu("🎺 Set Brass Voice")]
        private void SetBrassVoice() => SetupMusicalVoice(MusicalVoiceType.Brass);
        
        [ContextMenu("✨ Set Ethereal Voice")]
        private void SetEtherealVoice() => SetupMusicalVoice(MusicalVoiceType.Ethereal);
        
        [ContextMenu("🌳 Set Woody Voice")]
        private void SetWoodyVoice() => SetupMusicalVoice(MusicalVoiceType.Woody);
        
        [ContextMenu("💎 Set Crystal Voice")]
        private void SetCrystalVoice() => SetupMusicalVoice(MusicalVoiceType.Crystal);
        
        [ContextMenu("🌊 Set Oceanic Voice")]
        private void SetOceanicVoice() => SetupMusicalVoice(MusicalVoiceType.Oceanic);
        
        [ContextMenu("🎲 Randomize Voice")]
        private void RandomizeVoice() => SetupRandomMusicalVoice();
    }

    // 🎵 Musical Voice Types
    public enum MusicalVoiceType
    {
        Random,
        Flute,      // 🪈 Pure, breathy
        Bell,       // 🔔 Sharp attack, harmonic decay
        Brass,      // 🎺 Bold, sustained
        Ethereal,   // ✨ Shimmery, otherworldly
        Woody,      // 🌳 Like wooden wind instruments
        Crystal,    // 💎 Pure, high harmonics
        Oceanic     // 🌊 Deep, whale-like
    }

    // 🎼 Musical Voice Preset Data
    [System.Serializable]
    public class MusicalVoicePreset
    {
        public string name;
        public float modulatorRatio;     // Musical ratio to carrier frequency
        public float baseModDepth;       // Constrained modulation depth
        public float driftAmount;        // Natural pitch variation
        public float breathingRate;      // Slow organic modulation
        
        public MusicalVoicePreset(string n, float modRatio, float modDepth, float drift, float breathing)
        {
            name = n;
            modulatorRatio = modRatio;
            baseModDepth = modDepth;
            driftAmount = drift;
            breathingRate = breathing;
        }
    }
}