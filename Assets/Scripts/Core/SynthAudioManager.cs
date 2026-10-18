using System;
using UnityEngine;
using Vortex.Core;

namespace Vortex.Core
{
    /// <summary>
    /// Gerenciador de áudio procedural Sci-Fi sintetizado 100% em memória no Awake().
    /// Zero dependência de arquivos de áudio externos (.wav/.mp3/.ogg).
    /// Gera formas de onda senoidais, ruído filtrado e envoltórias exponenciais limpas.
    /// </summary>
    public class SynthAudioManager : MonoBehaviour
    {
        public static SynthAudioManager Instance { get; private set; }

        private const int SampleRate = 44100;

        private AudioClip _shootClip;
        private AudioClip _hitClip;
        private AudioClip _xpPickupClip;
        private AudioClip _levelUpClip;
        private AudioClip _shockwaveClip;

        private AudioSource _sfxSource;
        private AudioSource _xpSource;
        private AudioSource _levelUpSource;

        private float _lastPickupTime = -999f;
        private int _pickupStreak = 0;
        private const float StreakResetWindow = 0.85f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            SetupAudioSources();
            GenerateAllProceduralClips();
        }

        private void OnEnable()
        {
            EventBus.OnPlayerLevelUp += HandlePlayerLevelUp;
        }

        private void OnDisable()
        {
            EventBus.OnPlayerLevelUp -= HandlePlayerLevelUp;
        }

        private void SetupAudioSources()
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f; // 2D som estéreo direto
            _sfxSource.volume = 0.7f;

            _xpSource = gameObject.AddComponent<AudioSource>();
            _xpSource.playOnAwake = false;
            _xpSource.spatialBlend = 0f;
            _xpSource.volume = 0.65f;

            _levelUpSource = gameObject.AddComponent<AudioSource>();
            _levelUpSource.playOnAwake = false;
            _levelUpSource.spatialBlend = 0f;
            _levelUpSource.volume = 0.85f;
        }

        private void GenerateAllProceduralClips()
        {
            _shootClip = CreateShootClip();
            _hitClip = CreateHitClip();
            _xpPickupClip = CreateXPPickupClip();
            _levelUpClip = CreateLevelUpClip();
            _shockwaveClip = CreateShockwaveClip();
        }

        #region Procedural Audio Generators

        /// <summary>
        /// Laser curto e agudo: sweep exponencial de frequência descendente (1050 Hz -> 260 Hz) em 0.11s.
        /// </summary>
        private AudioClip CreateShootClip()
        {
            float duration = 0.11f;
            int samples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                // Sweep de 1100 Hz para 250 Hz
                float freq = Mathf.Lerp(1100f, 250f, t * t);
                phase += 2f * Mathf.PI * freq / SampleRate;

                // Envoltória rápida: ataque de 2ms e decaimento exponencial
                float attack = Mathf.Clamp01((float)i / (SampleRate * 0.003f));
                float decay = Mathf.Exp(-6f * t);

                data[i] = Mathf.Sin(phase) * attack * decay * 0.6f;
            }

            AudioClip clip = AudioClip.Create("SFX_LaserShoot", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Pulso grave de impacto: sub-grave de 140 Hz caindo para 35 Hz com rajada sutil de ruído para dar crunch em 0.13s.
        /// </summary>
        private AudioClip CreateHitClip()
        {
            float duration = 0.13f;
            int samples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(140f, 35f, t);
                phase += 2f * Mathf.PI * freq / SampleRate;

                float attack = Mathf.Clamp01((float)i / (SampleRate * 0.002f));
                float decay = Mathf.Exp(-7f * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-18f * t) * 0.35f;

                data[i] = (Mathf.Sin(phase) * 0.75f + noise) * attack * decay * 0.8f;
            }

            AudioClip clip = AudioClip.Create("SFX_HitImpact", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Som cristalino de XP: tom senoidal puro com harmônico de oitava alta em 0.12s.
        /// </summary>
        private AudioClip CreateXPPickupClip()
        {
            float duration = 0.12f;
            int samples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[samples];

            const float fundamentalFreq = 1174.66f; // D6
            const float octaveFreq = 2349.32f;      // D7

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float attack = Mathf.Clamp01((float)i / (SampleRate * 0.004f));
                float decay = Mathf.Exp(-6.5f * t);

                float wave1 = Mathf.Sin(2f * Mathf.PI * fundamentalFreq * (i / (float)SampleRate));
                float wave2 = Mathf.Sin(2f * Mathf.PI * octaveFreq * (i / (float)SampleRate)) * 0.35f;

                data[i] = (wave1 + wave2) * attack * decay * 0.65f;
            }

            AudioClip clip = AudioClip.Create("SFX_XPPickup", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Arpejo Sci-Fi Ascendente de Level Up: sequência rápida de notas (C5, E5, G5, C6, E6) em 0.6s.
        /// </summary>
        private AudioClip CreateLevelUpClip()
        {
            float noteDuration = 0.10f;
            float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f }; // C5, E5, G5, C6, E6
            float totalDuration = (noteDuration * freqs.Length) + 0.18f;
            int samples = Mathf.CeilToInt(SampleRate * totalDuration);
            float[] data = new float[samples];

            for (int n = 0; n < freqs.Length; n++)
            {
                int startSample = Mathf.RoundToInt(n * noteDuration * SampleRate);
                int noteSamples = Mathf.RoundToInt(0.22f * SampleRate);
                float freq = freqs[n];

                for (int i = 0; i < noteSamples && (startSample + i) < samples; i++)
                {
                    float t = (float)i / noteSamples;
                    float attack = Mathf.Clamp01((float)i / (SampleRate * 0.003f));
                    float decay = Mathf.Exp(-4.5f * t);

                    float fundamental = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)SampleRate));
                    float sparkle = Mathf.Sin(2f * Mathf.PI * (freq * 2f) * (i / (float)SampleRate)) * 0.25f;

                    data[startSample + i] += (fundamental + sparkle) * attack * decay * 0.45f;
                }
            }

            AudioClip clip = AudioClip.Create("SFX_LevelUpFanfare", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Pulso grave expansivo da Shockwave: estrondo sonoro com varredura de sub-grave (90 Hz -> 30 Hz) em 0.38s.
        /// </summary>
        private AudioClip CreateShockwaveClip()
        {
            float duration = 0.38f;
            int samples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(90f, 30f, t);
                phase += 2f * Mathf.PI * freq / SampleRate;

                float envelope = Mathf.Sin(t * Mathf.PI); // Inchaço e caimento
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.25f * Mathf.Exp(-4f * t);

                data[i] = (Mathf.Sin(phase) * 0.8f + noise) * envelope * 0.85f;
            }

            AudioClip clip = AudioClip.Create("SFX_VortexShockwave", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion

        #region Public Playback Methods

        /// <summary>
        /// Dispara o efeito sonoro de tiro a laser.
        /// </summary>
        public void PlayShootSFX()
        {
            if (_shootClip != null && _sfxSource != null)
            {
                _sfxSource.PlayOneShot(_shootClip, 0.7f);
            }
        }

        /// <summary>
        /// Dispara o efeito sonoro de impacto de dano no inimigo.
        /// </summary>
        public void PlayHitSFX()
        {
            if (_hitClip != null && _sfxSource != null)
            {
                _sfxSource.PlayOneShot(_hitClip, 0.65f);
            }
        }

        /// <summary>
        /// Dispara o efeito de coleta de XP, incrementando o tom musical se coletados em sequência.
        /// </summary>
        public void PlayXPPickupSFX()
        {
            if (_xpPickupClip == null || _xpSource == null) return;

            if (Time.time - _lastPickupTime > StreakResetWindow)
            {
                _pickupStreak = 0;
            }
            else
            {
                _pickupStreak = Mathf.Min(_pickupStreak + 1, 9);
            }
            _lastPickupTime = Time.time;

            // Escala pentatônica / semitons ascendentes (1.0 até ~1.8x pitch)
            float pitchMultiplier = Mathf.Pow(1.05946f, _pickupStreak * 1.5f);
            _xpSource.pitch = pitchMultiplier;
            _xpSource.PlayOneShot(_xpPickupClip, 0.75f);
        }

        /// <summary>
        /// Dispara o arpejo sci-fi de Level Up.
        /// </summary>
        public void PlayLevelUpSFX()
        {
            if (_levelUpClip != null && _levelUpSource != null)
            {
                _levelUpSource.PlayOneShot(_levelUpClip, 0.9f);
            }
        }

        /// <summary>
        /// Dispara o pulso sonoro grave da Vortex Shockwave.
        /// </summary>
        public void PlayShockwaveSFX()
        {
            if (_shockwaveClip != null && _sfxSource != null)
            {
                _sfxSource.PlayOneShot(_shockwaveClip, 0.95f);
            }
        }

        #endregion

        private void HandlePlayerLevelUp(int newLevel)
        {
            PlayLevelUpSFX();
        }
    }
}
