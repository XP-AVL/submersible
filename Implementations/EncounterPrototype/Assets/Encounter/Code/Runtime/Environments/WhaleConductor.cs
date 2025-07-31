using Encounter.Runtime.Creatures;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Encounter.Runtime.Environments
{
    public class WhaleConductor : MonoBehaviour
    {
        [Header("🎵 Musical Settings")]
        [SerializeField] private float bpm = 80f;
        [SerializeField] private MusicalNote[] melody = {
            new MusicalNote(NoteName.C, 4),
            new MusicalNote(NoteName.E, 4), 
            new MusicalNote(NoteName.G, 4),
            new MusicalNote(NoteName.C, 4)
        }; // C4-E4-G4-C4 by default
        
        // Convert musical notes to frequencies for the jellies
        public float[] Melody 
        { 
            get 
            {
                float[] frequencies = new float[melody.Length];
                for (int i = 0; i < melody.Length; i++)
                {
                    frequencies[i] = melody[i].ToFrequency();
                }
                return frequencies;
            } 
        }
        
        [Header("🎤 Voice Detection")]
        [SerializeField] private float volumeThreshold = 0.015f; // Lower threshold (was 0.02)
        [SerializeField] private float fundamentalFreqMin = 60f;  // Lower range (was 80)
        [SerializeField] private float fundamentalFreqMax = 500f; // Higher range (was 400)
        [SerializeField] private float noiseThreshold = 0.1f;     // Lower threshold (was 0.15)
        [SerializeField] private int analysisWindowSize = 512;
        [SerializeField] private bool debugMicrophone = true;
        
        [Header("🎛️ Audio Settings")]
        [SerializeField] private int sampleRate = 44100;
        [SerializeField] private float updateRate = 0.1f;
        
        // Private variables
        private AudioClip _microphoneClip;
        private string _microphoneName;
        private bool _isListening;
        private float _lastVolumeCheck;
        private float[] _analysisBuffer;
        
        // Musical timing
        private int _currentBeat = 0;
        private int _currentMelodyIndex = 0;
        private bool _isPlayerSinging = false;
        
        // Jelly management
        private WhaleCallBehavior[] _allJellies;
        
        // Events for jellies to subscribe to
        public static System.Action<int, float, bool> OnBeat; // beatNumber, currentNote, isPlayerSinging

        private void Start()
        {
            Debug.Log("🎵 Whale Conductor starting up!");
            
            // Find all jellies in the scene
            RefreshJellyList();
            
            // Initialize audio analysis
            _analysisBuffer = new float[analysisWindowSize];
            InitializeMicrophone();
            
            // Start the musical beat
            StartCoroutine(BeatCoroutine());
            
            Debug.Log($"🎵 Conductor initialized! Found {_allJellies.Length} jellies. Melody: {string.Join("-", System.Array.ConvertAll(melody, note => note.ToString()))} at {bpm} BPM");
        }

        private void Update()
        {
            // Check microphone input at specified rate
            if (Time.time - _lastVolumeCheck >= updateRate)
            {
                CheckForMelodicVoice();
                _lastVolumeCheck = Time.time;
            }
            
            // Refresh jelly list periodically
            if (Time.time % 5f < Time.deltaTime)
            {
                RefreshJellyList();
            }
        }

        private IEnumerator BeatCoroutine()
        {
            while (true)
            {
                // Calculate beat interval from BPM
                float beatInterval = 60f / bpm;
                
                // Send beat to all jellies
                float currentNote = melody[_currentMelodyIndex].ToFrequency();
                OnBeat?.Invoke(_currentBeat, currentNote, _isPlayerSinging);
                
                if (debugMicrophone)
                {
                    string noteName = melody[_currentMelodyIndex].ToString();
                    Debug.Log($"🎵 Beat {_currentBeat}: Playing {noteName} ({currentNote:F1}Hz) | Player singing: {_isPlayerSinging}");
                }
                
                // Advance to next beat and melody note
                _currentBeat++;
                _currentMelodyIndex = (_currentMelodyIndex + 1) % melody.Length;
                
                yield return new WaitForSeconds(beatInterval);
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
                Debug.LogError("No microphone detected! Voice detection disabled.");
                _isListening = false;
            }
        }

        private void CheckForMelodicVoice()
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
            
            // Check if this sounds like melodic human voice
            bool isMelodicVoice = IsLikelyMelodicVoice(_analysisBuffer);
            _isPlayerSinging = isMelodicVoice;
            
            // Debug output (less frequent to reduce spam)
            if (debugMicrophone && Time.time % 1f < updateRate) // Every 1 second instead of 0.5
            {
                float rms = CalculateRms(_analysisBuffer);
                float pitch = EstimatePitch(_analysisBuffer);
                string status = _isPlayerSinging ? "🎤 SINGING!" : "🎤 Silent";
                Debug.Log($"{status} | Vol: {rms:F4} | Pitch: {pitch:F1}Hz");
            }
        }

        private bool IsLikelyMelodicVoice(float[] audioData)
        {
            // Check basic volume level
            float rms = CalculateRms(audioData);
            if (rms < volumeThreshold) return false;
            
            // Check if frequency is in human vocal range (now more forgiving)
            float pitch = EstimatePitch(audioData);
            if (pitch < fundamentalFreqMin || pitch > fundamentalFreqMax) return false;
            
            // Check for voice-like noise characteristics (more forgiving)
            float noiseLevel = CalculateNoiseLevel(audioData);
            if (noiseLevel < noiseThreshold) return false;
            
            // Additional check: make sure we have some sustained energy
            bool hasSustainedEnergy = rms > volumeThreshold * 0.5f;
            
            return hasSustainedEnergy;
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

        private float EstimatePitch(float[] audioData)
        {
            // Simple peak-based pitch detection
            List<int> peakPositions = new List<int>();
            
            float avgAmplitude = 0;
            for (int i = 0; i < audioData.Length; i++)
            {
                avgAmplitude += Mathf.Abs(audioData[i]);
            }
            avgAmplitude /= audioData.Length;
            
            float peakThreshold = avgAmplitude * 0.3f;
            
            for (int i = 1; i < audioData.Length - 1; i++)
            {
                if (audioData[i] > audioData[i-1] && 
                    audioData[i] > audioData[i+1] && 
                    audioData[i] > peakThreshold)
                {
                    peakPositions.Add(i);
                }
            }
            
            if (peakPositions.Count < 2) return 0;
            
            float totalPeriod = 0;
            int validPeriods = 0;
            
            for (int i = 1; i < peakPositions.Count; i++)
            {
                int period = peakPositions[i] - peakPositions[i-1];
                
                if (period > sampleRate / fundamentalFreqMax && period < sampleRate / fundamentalFreqMin)
                {
                    totalPeriod += period;
                    validPeriods++;
                }
            }
            
            if (validPeriods == 0) return 0;
            
            float averagePeriod = totalPeriod / validPeriods;
            return sampleRate / averagePeriod;
        }

        private float CalculateNoiseLevel(float[] audioData)
        {
            float totalVariation = 0f;
            float totalEnergy = 0f;
            
            for (int i = 2; i < audioData.Length - 2; i++)
            {
                float smoothed = (audioData[i-2] + audioData[i-1] + audioData[i] + audioData[i+1] + audioData[i+2]) / 5f;
                float deviation = Mathf.Abs(audioData[i] - smoothed);
                
                totalVariation += deviation;
                totalEnergy += Mathf.Abs(audioData[i]);
            }
            
            if (totalEnergy < 0.001f) return 0f;
            return totalVariation / totalEnergy;
        }

        private void RefreshJellyList()
        {
            _allJellies = FindObjectsByType<WhaleCallBehavior>(FindObjectsSortMode.None);
        }

        private string GetNoteName(float frequency)
        {
            // Convert frequency back to note name for display
            MusicalNote note = MusicalNote.FromFrequency(frequency);
            return note.ToString();
        }

        private void OnDestroy()
        {
            if (_isListening && !string.IsNullOrEmpty(_microphoneName))
            {
                Microphone.End(_microphoneName);
            }
        }

        // 🎨 Debug GUI
        private void OnGUI()
        {
            if (!debugMicrophone) return;
            
            GUI.Box(new Rect(10, 10, 350, 140), "🎵 Whale Conductor Debug");
            
            GUI.Label(new Rect(20, 35, 320, 20), $"BPM: {bpm} | Beat: {_currentBeat}");
            GUI.Label(new Rect(20, 55, 320, 20), $"Current Note: {melody[_currentMelodyIndex].ToString()}");
            GUI.Label(new Rect(20, 75, 320, 20), $"Jellies Found: {_allJellies?.Length ?? 0}");
            GUI.Label(new Rect(20, 95, 320, 20), $"Microphone: {(_isListening ? "Active" : "Inactive")}");
            
            // Player singing indicator
            GUI.color = _isPlayerSinging ? Color.green : Color.gray;
            GUI.Box(new Rect(20, 115, 320, 25), _isPlayerSinging ? "🎤 PLAYER SINGING!" : "🎤 No voice detected");
            GUI.color = Color.white;
        }
    }

    // 🎼 Musical Note System for Easy Melody Input!
    [System.Serializable]
    public class MusicalNote
    {
        [SerializeField] private NoteName noteName = NoteName.C;
        [SerializeField] private int octave = 4;
        
        public MusicalNote(NoteName name, int oct)
        {
            noteName = name;
            octave = oct;
        }
        
        public float ToFrequency()
        {
            // Calculate frequency using A4 = 440Hz as reference
            // Formula: freq = 440 * 2^((semitones from A4) / 12)
            
            int semitonesFromC = GetSemitonesFromC(noteName);
            int totalSemitones = (octave - 4) * 12 + semitonesFromC - 9; // -9 because A4 is 9 semitones above C4
            
            return 440f * Mathf.Pow(2f, totalSemitones / 12f);
        }
        
        public static MusicalNote FromFrequency(float frequency)
        {
            // Convert frequency back to note (approximate)
            float semitonesFromA4 = 12f * Mathf.Log(frequency / 440f) / Mathf.Log(2f);
            int totalSemitones = Mathf.RoundToInt(semitonesFromA4);
            
            int octave = 4 + (totalSemitones + 9) / 12;
            int noteIndex = ((totalSemitones + 9) % 12 + 12) % 12;
            
            NoteName[] noteNames = {NoteName.C, NoteName.CSharp, NoteName.D, NoteName.DSharp, 
                                   NoteName.E, NoteName.F, NoteName.FSharp, NoteName.G, 
                                   NoteName.GSharp, NoteName.A, NoteName.ASharp, NoteName.B};
            
            return new MusicalNote(noteNames[noteIndex], octave);
        }
        
        private int GetSemitonesFromC(NoteName note)
        {
            switch (note)
            {
                case NoteName.C: return 0;
                case NoteName.CSharp: return 1;
                case NoteName.D: return 2;
                case NoteName.DSharp: return 3;
                case NoteName.E: return 4;
                case NoteName.F: return 5;
                case NoteName.FSharp: return 6;
                case NoteName.G: return 7;
                case NoteName.GSharp: return 8;
                case NoteName.A: return 9;
                case NoteName.ASharp: return 10;
                case NoteName.B: return 11;
                default: return 0;
            }
        }
        
        public override string ToString()
        {
            string noteStr = noteName.ToString().Replace("Sharp", "#");
            return $"{noteStr}{octave}";
        }
    }

    public enum NoteName
    {
        C,
        CSharp,  // C#
        D,
        DSharp,  // D#
        E,
        F,
        FSharp,  // F#
        G,
        GSharp,  // G#
        A,
        ASharp,  // A#
        B
    }
}