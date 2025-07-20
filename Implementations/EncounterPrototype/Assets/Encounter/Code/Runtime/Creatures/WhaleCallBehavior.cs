using UnityEngine;
using System.Collections;

namespace Encounter.Runtime.Creatures
{
    public class WhaleCallBehavior : MonoBehaviour
    {
        [Header("Call Timing")]
        [SerializeField] private float minCallInterval = 3f;
        [SerializeField] private float maxCallInterval = 12f;
        [SerializeField] private float responseChance = 0.3f;
        [SerializeField] private float listenRange = 80f;
    
        [Header("Individual Personality")]
        [SerializeField] private float chattiness = 1f;
        [SerializeField] private float shyness;
    
        [Header("Gill Glow System")]
        [SerializeField] private JellyfishGill gillSystem;
        [SerializeField] private string glowPropertyName = "_GlowIntensity";
        [SerializeField] private float baseGlowIntensity = 4f;
        [SerializeField] private bool useNoteBasedColors = true;
        
        [Header("🎵 Melodic Singing")]
        [SerializeField] private bool canSingMelodies = true;
        [SerializeField] private float pitchAccuracy = 0.95f;
        [SerializeField] private float melodicCallDuration = 1.2f;
        [SerializeField] private AnimationCurve melodicVolumeEnvelope;
    
        private WhaleCallSynthesizer _synthesizer;
        private float _nextCallTime;
        private float _lastCallTime = -999f;
    
        // Gill glow system variables
        private Coroutine _currentGlowCoroutine;
        private Material _uniqueGillMaterial;
        private float _currentGlowIntensity;
        private Color _whaleNoteColor;
    
        // Conductor system variables
        private bool _isPausedForChorus;
        private float _pauseEndTime;
        
        // Melodic singing variables
        private float originalCarrierFrequency;
        private bool isSingingMelody = false;
    
        // Static variables to manage the pod's overall behavior
        private static float _lastAnyWhaleCallTime = -999f;
        private static int _activeCalls;
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        private const float PodQuietPeriod = 2f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStaticVariables()
        {
            _lastAnyWhaleCallTime = -999f;
            _activeCalls = 0;
        }
    
        private void InitializeGillGlow()
        {
            if (gillSystem != null && gillSystem.gillMaterial != null)
            {
                Debug.Log($"Original material: {gillSystem.gillMaterial.name}");
                _uniqueGillMaterial = new Material(gillSystem.gillMaterial);
                Debug.Log($"Created unique material: {_uniqueGillMaterial.name}");
                gillSystem.gillMaterial = _uniqueGillMaterial;
            }
            else
            {
                Debug.LogError($"Whale {gameObject.name}: GillSystem or gillMaterial is null!");
            }
        }
        
        void InitializeMelodicCapabilities()
        {
            if (_synthesizer != null)
            {
                originalCarrierFrequency = _synthesizer.CarrierFrequency;
            }
            
            if (melodicVolumeEnvelope == null || melodicVolumeEnvelope.length == 0)
            {
                melodicVolumeEnvelope = CreateMelodicEnvelope();
            }
        }
        
        // 🎵 THE MAGIC METHOD - Make whale sing specific pitch!
        public void TriggerMelodicCall(float targetFrequency)
        {
            if (!canSingMelodies || _synthesizer == null) 
            {
                TriggerChorusCall();
                return;
            }
            
            Debug.Log($"🎵 Whale {gameObject.name} singing: {targetFrequency:F1}Hz");
            
            isSingingMelody = true;
            
            float originalFreq = _synthesizer.CarrierFrequency;
            AnimationCurve originalEnvelope = _synthesizer.VolumeEnvelope;
            float originalModDepth = _synthesizer.ModulationDepth;
            
            float accuracyVariation = Random.Range(pitchAccuracy, 1f / pitchAccuracy);
            float actualFrequency = targetFrequency * accuracyVariation;
            
            _synthesizer.CarrierFrequency = actualFrequency;
            _synthesizer.VolumeEnvelope = melodicVolumeEnvelope;
            _synthesizer.ModulationDepth *= 0.5f;
            
            _synthesizer.TriggerCall();
            StartMelodicGillGlow(targetFrequency);
            
            _lastCallTime = Time.time;
            _lastAnyWhaleCallTime = Time.time;
            
            StartCoroutine(RestoreNaturalVoice(originalFreq, originalEnvelope, originalModDepth, melodicCallDuration));
        }
        
        IEnumerator RestoreNaturalVoice(float origFreq, AnimationCurve origEnv, float origMod, float wait)
        {
            yield return new WaitForSeconds(wait + 0.5f);
            
            if (_synthesizer != null)
            {
                _synthesizer.CarrierFrequency = origFreq;
                _synthesizer.VolumeEnvelope = origEnv;
                _synthesizer.ModulationDepth = origMod;
            }
            
            isSingingMelody = false;
            Debug.Log($"🐋 Whale {gameObject.name} returns to natural voice");
        }
        
        void StartMelodicGillGlow(float sungFrequency)
        {
            float pitchDiff = Mathf.Log(sungFrequency / originalCarrierFrequency) / Mathf.Log(2f);
            
            Color melodicColor;
            if (pitchDiff < -0.5f)
                melodicColor = Color.Lerp(Color.blue, Color.cyan, (pitchDiff + 2f) / 1.5f);
            else if (pitchDiff > 0.5f)
                melodicColor = Color.Lerp(Color.yellow, Color.red, Mathf.Min(pitchDiff - 0.5f, 1f));
            else
                melodicColor = Color.Lerp(Color.green, Color.cyan, Mathf.Abs(pitchDiff) * 2f);
            
            if (_currentGlowCoroutine != null)
                StopCoroutine(_currentGlowCoroutine);
            
            _currentGlowCoroutine = StartCoroutine(MelodicGlowEffect(melodicColor));
        }
        
        IEnumerator MelodicGlowEffect(Color targetColor)
        {
            float elapsed = 0f;
            float glowDuration = melodicCallDuration;
            
            while (elapsed < glowDuration + 0.5f)
            {
                float progress = elapsed / glowDuration;
                float pulse = Mathf.Sin(progress * Mathf.PI * 2f) * 0.3f + 0.7f;
                _currentGlowIntensity = baseGlowIntensity * pulse * 1.5f;
                
                if (gillSystem != null)
                {
                    for (int i = 0; i < gillSystem.GetGillCount(); i++)
                    {
                        LineRenderer gill = gillSystem.GetGill(i);
                        if (gill != null && gill.material != null)
                        {
                            gill.material.SetFloat(glowPropertyName, _currentGlowIntensity);
                            gill.material.SetColor(Color1, targetColor * _currentGlowIntensity);
                        }
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            _currentGlowIntensity = 0f;
            _currentGlowCoroutine = null;
        }
        
        AnimationCurve CreateMelodicEnvelope()
        {
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(0.1f, 0.8f);
            curve.AddKey(0.2f, 1f);
            curve.AddKey(0.7f, 0.9f);
            curve.AddKey(0.9f, 0.3f);
            curve.AddKey(1f, 0f);
            
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0.5f);
            
            return curve;
        }
    
        public void PauseForChorus(float duration)
        {
            _isPausedForChorus = true;
            _pauseEndTime = Time.time + duration;
            Debug.Log($"Whale {gameObject.name} paused for chorus (duration: {duration:F1}s)");
        }
    
        public void TriggerChorusCall()
        {
            if (isSingingMelody) return;
            
            Debug.Log($"🎵 Whale {gameObject.name} joins the chorus! 🎵");
        
            if (_synthesizer != null)
            {
                _synthesizer.TriggerCall();
                _lastCallTime = Time.time;
                _lastAnyWhaleCallTime = Time.time;
                StartGillGlow();
                _nextCallTime = Time.time + Random.Range(8f, 15f);
            }
        }
    
        public float GetLastCallTime() => _lastCallTime;

        private Color GetNoteColor(float frequency)
        {
            if (!useNoteBasedColors) return Color.cyan;
            
            if (Mathf.Approximately(frequency, 110.00f)) return new Color(0.2f, 0.4f, 1f, 1f);
            if (Mathf.Approximately(frequency, 123.47f)) return new Color(0.4f, 1f, 0.6f, 1f);
            if (Mathf.Approximately(frequency, 164.81f)) return new Color(1f, 0.6f, 0.2f, 1f);
            if (Mathf.Approximately(frequency, 220.00f)) return new Color(0.6f, 0.2f, 1f, 1f);
            if (Mathf.Approximately(frequency, 246.94f)) return new Color(1f, 1f, 0.4f, 1f);
            
            return Color.cyan;
        }

        private void StartGillGlow()
        {
            if (_currentGlowCoroutine != null)
                StopCoroutine(_currentGlowCoroutine);
            
            _currentGlowCoroutine = StartCoroutine(SynchronizedGlowEffect());
        }

        private IEnumerator SynchronizedGlowEffect()
        {
            float elapsed = 0f;
            float callDuration = _synthesizer.CallDuration;
        
            while (elapsed < callDuration + 0.5f)
            {
                float callProgress = elapsed / callDuration;
                float volumeLevel = 1f;
                
                if (_synthesizer.VolumeEnvelope != null)
                    volumeLevel = _synthesizer.VolumeEnvelope.Evaluate(Mathf.Clamp01(callProgress));
                
                _currentGlowIntensity = volumeLevel * baseGlowIntensity;
                
                if (elapsed < 0.02f)
                    _currentGlowIntensity *= elapsed / 0.02f;
                else if (elapsed > callDuration + 0.4f)
                    _currentGlowIntensity *= Mathf.Max(0f, 1f - ((elapsed - callDuration - 0.4f) / 0.1f));
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        
            _currentGlowIntensity = 0f;
            _currentGlowCoroutine = null;
        }

        private void UpdateGillGlow()
        {
            if (gillSystem == null) return;
        
            for (int i = 0; i < gillSystem.GetGillCount(); i++)
            {
                LineRenderer gill = gillSystem.GetGill(i);
                if (gill != null && gill.material != null)
                {
                    gill.material.SetFloat(glowPropertyName, _currentGlowIntensity);
                    
                    if (useNoteBasedColors && _currentGlowIntensity > 0f)
                    {
                        Color finalColor = _whaleNoteColor * Mathf.Min(_currentGlowIntensity, 1f);
                        gill.material.SetColor(Color1, finalColor);
                    }
                }
            }
        }

        private void Start()
        {
            _synthesizer = GetComponent<WhaleCallSynthesizer>();
            InitializeGillGlow();
            InitializeMelodicCapabilities();
            RandomizePersonality();
            ScheduleNextCall();
            _nextCallTime += Random.Range(0f, 5f);
        }

        private void Update()
        {
            if (_isPausedForChorus)
            {
                if (Time.time >= _pauseEndTime)
                {
                    _isPausedForChorus = false;
                    Debug.Log($"Whale {gameObject.name} resume from chorus pause");
                }
                else
                {
                    UpdateGillGlow();
                    return;
                }
            }
            
            if (Time.time >= _nextCallTime)
                TryToCall();
            
            CheckForNearbyCallsToRespondTo();
            UpdateGillGlow();
            
            if (_activeCalls > 10)
            {
                Debug.LogWarning("Resetting stuck activeCalls counter!");
                _activeCalls = 0;
            }
            
            if (Time.time % 10f < Time.deltaTime)
                Debug.Log($"Pod Status: Active calls: {_activeCalls}, Last call: {Time.time - _lastAnyWhaleCallTime:F1}s ago");
        }

        private void TryToCall()
        {
            string debugReason;
            
            if (Time.time - _lastAnyWhaleCallTime < PodQuietPeriod)
            {
                debugReason = $"Pod quiet period (last call {Time.time - _lastAnyWhaleCallTime:F1}s ago)";
                _nextCallTime = Time.time + Random.Range(0.5f, 2f);
                if (Random.value < 0.1f) Debug.Log($"Whale {gameObject.name}: {debugReason}");
                return;
            }
            
            if (_activeCalls >= 2)
            {
                debugReason = $"Too many active calls ({_activeCalls})";
                _nextCallTime = Time.time + Random.Range(1f, 3f);
                if (Random.value < 0.1f) Debug.Log($"Whale {gameObject.name}: {debugReason}");
                return;
            }
            
            if (_activeCalls > 0 && Random.value < shyness)
            {
                debugReason = $"Being shy (shyness={shyness:F2}, activeCalls={_activeCalls})";
                _nextCallTime = Time.time + Random.Range(2f, 5f);
                if (Random.value < 0.1f) Debug.Log($"Whale {gameObject.name}: {debugReason}");
                return;
            }
            
            MakeCall();
        }

        private void MakeCall()
        {
            if (_synthesizer != null)
            {
                _synthesizer.TriggerCall();
                _lastCallTime = Time.time;
                _lastAnyWhaleCallTime = Time.time;
                _activeCalls++;
                ScheduleNextCall();
                StartGillGlow();
                StartCoroutine(TrackCallDuration());
                Debug.Log($"Whale {gameObject.name} calls out! Active calls: {_activeCalls}");
            }
        }

        private IEnumerator TrackCallDuration()
        {
            yield return new WaitForSeconds(_synthesizer.CallDuration + 0.5f);
            _activeCalls = Mathf.Max(0, _activeCalls - 1);
            Debug.Log($"Whale {gameObject.name} finished calling. Active calls now: {_activeCalls}");
        }

        private void ScheduleNextCall()
        {
            float baseInterval = Random.Range(minCallInterval, maxCallInterval);
            float personalityModifier = 1f / chattiness;
            _nextCallTime = Time.time + (baseInterval * personalityModifier);
        }

        private void CheckForNearbyCallsToRespondTo()
        {
            if (Time.time % 0.5f < Time.deltaTime)
            {
                WhaleCallBehavior[] otherWhales = FindObjectsByType<WhaleCallBehavior>(FindObjectsSortMode.None);
                
                foreach (var otherWhale in otherWhales)
                {
                    if (otherWhale == this) continue;
                    
                    float distance = Vector3.Distance(transform.position, otherWhale.transform.position);
                    
                    if (distance <= listenRange && 
                        Time.time - otherWhale._lastCallTime < 3f &&
                        Time.time - otherWhale._lastCallTime > 0.5f &&
                        Time.time - _lastCallTime > 5f)
                    {
                        if (Random.value < responseChance)
                        {
                            _nextCallTime = Time.time + Random.Range(1f, 4f);
                            Debug.Log($"Whale {gameObject.name} decides to respond to {otherWhale.gameObject.name}");
                            break;
                        }
                    }
                }
            }
        }

        private void RandomizePersonality()
        {
            chattiness = Random.Range(0.7f, 1.5f);
            shyness = Random.Range(0f, 0.8f);
            responseChance = Random.Range(0.1f, 0.5f);
            
            if (_synthesizer != null)
            {
                RandomizeVoiceParameters();
                CreateUniqueAnimationCurves();
            }
        }

        private void RandomizeVoiceParameters()
        {
            float[] aminorSus2Notes = {
                110.00f, 123.47f, 164.81f, 220.00f, 246.94f
            };
            
            _synthesizer.CarrierFrequency = aminorSus2Notes[Random.Range(0, aminorSus2Notes.Length)];
            _synthesizer.ModulatorFrequency = Random.Range(35f, 70f);
            _synthesizer.ModulationDepth = Random.Range(80f, 140f);
            _synthesizer.DriftAmount = Random.Range(0.015f, 0.035f);
            _synthesizer.BreathingRate = Random.Range(0.3f, 0.6f);
            
            string noteName = GetNoteName(_synthesizer.CarrierFrequency);
            _whaleNoteColor = GetNoteColor(_synthesizer.CarrierFrequency);
            
            Debug.Log($"Whale {gameObject.name} voice: {noteName} ({_synthesizer.CarrierFrequency:F1}Hz)");
        }
        
        private string GetNoteName(float frequency)
        {
            if (Mathf.Approximately(frequency, 110.00f)) return "A2";
            if (Mathf.Approximately(frequency, 123.47f)) return "B2";
            if (Mathf.Approximately(frequency, 164.81f)) return "E3";
            if (Mathf.Approximately(frequency, 220.00f)) return "A3";
            if (Mathf.Approximately(frequency, 246.94f)) return "B3";
            return $"{frequency:F1}Hz";
        }

        private void CreateUniqueAnimationCurves()
        {
            _synthesizer.VolumeEnvelope = CreateRandomVolumeEnvelope();
            _synthesizer.ModulationEnvelope = CreateRandomModulationEnvelope();
            _synthesizer.OrganicCurve = CreateRandomOrganicCurve();
        }

        private AnimationCurve CreateRandomVolumeEnvelope()
        {
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            
            float attackTime = Random.Range(0.05f, 0.3f);
            float attackLevel = Random.Range(0.7f, 1f);
            curve.AddKey(attackTime, attackLevel);
            
            if (Random.value > 0.3f)
            {
                float sustainTime = Random.Range(0.4f, 0.8f);
                float sustainLevel = Random.Range(0.6f, 1f);
                curve.AddKey(sustainTime, sustainLevel);
            }
            
            float releaseStart = Random.Range(0.75f, 0.95f);
            float releaseLevel = Random.Range(0.1f, 0.5f);
            curve.AddKey(releaseStart, releaseLevel);
            curve.AddKey(1f, 0f);
            
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0.3f);
            
            return curve;
        }

        private AnimationCurve CreateRandomModulationEnvelope()
        {
            AnimationCurve curve = new AnimationCurve();
            float startMod = Random.Range(0f, 0.4f);
            curve.AddKey(0f, startMod);
            
            int numPoints = Random.Range(2, 5);
            for (int i = 0; i < numPoints; i++)
            {
                float time = (float)(i + 1) / (numPoints + 1);
                float intensity = Random.Range(0.1f, 1f);
                curve.AddKey(time, intensity);
            }
            
            float endMod = Random.Range(0.2f, 0.8f);
            curve.AddKey(1f, endMod);
            
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0.2f);
            
            return curve;
        }

        private AnimationCurve CreateRandomOrganicCurve()
        {
            AnimationCurve curve = new AnimationCurve();
            float expressiveness = Random.Range(0.3f, 1.5f);
            
            curve.AddKey(0f, Random.Range(-0.5f, 0.5f) * expressiveness);
            
            int variations = Random.Range(1, 4);
            for (int i = 0; i < variations; i++)
            {
                float time = Random.Range(0.2f, 0.8f);
                float variation = Random.Range(-1f, 1f) * expressiveness;
                curve.AddKey(time, variation);
            }
            
            curve.AddKey(1f, Random.Range(-0.5f, 0.5f) * expressiveness);
            
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0.5f);
            
            return curve;
        }

        private void OnDestroy()
        {
            if (_activeCalls > 0)
                _activeCalls--;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, listenRange);
            
            if (_activeCalls > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position + Vector3.up * 2, 1f);
            }
            
            if (isSingingMelody)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.position + Vector3.up * 4, 1.5f);
            }
        }
        
        [ContextMenu("Whale Song Preset")]
        private void SetWhalePreset()
        {
            if (_synthesizer != null)
            {
                _synthesizer.CarrierFrequency = 180f;
                _synthesizer.ModulatorFrequency = 45f;
                _synthesizer.ModulationDepth = 120f;
                _synthesizer.DriftAmount = 0.03f;
                _synthesizer.BreathingRate = 0.3f;
            }
        }
    
        [ContextMenu("Bell-like Preset")]
        private void SetBellPreset()
        {
            if (_synthesizer != null)
            {
                _synthesizer.CarrierFrequency = 440f;
                _synthesizer.ModulatorFrequency = 880f;
                _synthesizer.ModulationDepth = 200f;
                _synthesizer.DriftAmount = 0.01f;
                _synthesizer.BreathingRate = 0.1f;
            }
        }
    
        [ContextMenu("Growling Bass Preset")]
        private void SetBassPreset()
        {
            if (_synthesizer != null)
            {
                _synthesizer.CarrierFrequency = 80f;
                _synthesizer.ModulatorFrequency = 25f;
                _synthesizer.ModulationDepth = 60f;
                _synthesizer.DriftAmount = 0.05f;
                _synthesizer.BreathingRate = 0.8f;
            }
        }
    }
}