using UnityEngine;

namespace Ridgeline.Audio
{
    /// <summary>
    /// All game audio goes through here.
    ///  - YouTube mute (isAudioEnabled == false) forces silence; in-game sliders cannot override it.
    ///  - In-game Music / SFX sliders only scale volume while YouTube audio is enabled.
    ///  - No global mute button in game (Playables rule); YouTube owns that.
    ///  - Paused: AudioListener.pause.
    /// Placeholder sounds are synthesised at startup (tiny, no asset files).
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        AudioSource _engine;
        AudioSource _sfx;
        AudioClip _coin, _fuel, _crash, _click, _bonus;

        bool _ytAudioEnabled = true;
        float _sfxVolume = 1f;
        float _musicVolume = 0.7f;
        bool _engineOn;

        void Awake()
        {
            _engine = gameObject.AddComponent<AudioSource>();
            _engine.loop = true;
            _engine.playOnAwake = false;
            _engine.clip = MakeEngineLoop();

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;

            // Placeholders only for slots UseClips() has not already filled (Awake order between components is not fixed).
            if (_coin == null) _coin = MakeTone("coin", 0.09f, 1320f, 1760f, 0.35f);
            if (_fuel == null) _fuel = MakeTone("fuel", 0.25f, 440f, 880f, 0.4f);
            if (_bonus == null) _bonus = MakeTone("bonus", 0.3f, 660f, 1320f, 0.35f);
            if (_click == null) _click = MakeTone("click", 0.04f, 900f, 700f, 0.25f);
            if (_crash == null) _crash = MakeNoise("crash", 0.45f, 0.6f);
            ApplyMaster();
        }

        /// <summary>Swaps the synthesised placeholders for real clips where the art set has them.</summary>
        public void UseClips(Art.ArtSet art)
        {
            if (art == null) return;
            if (art.coin) _coin = art.coin;
            if (art.fuel) _fuel = art.fuel;
            if (art.bonus) _bonus = art.bonus;
            if (art.click) _click = art.click;
            if (art.crash) _crash = art.crash;
        }

        public void SetYouTubeAudioEnabled(bool enabled)
        {
            _ytAudioEnabled = enabled;
            ApplyMaster();
        }

        public void SetVolumes(float music, float sfx)
        {
            _musicVolume = Mathf.Clamp01(music);
            _sfxVolume = Mathf.Clamp01(sfx);
            ApplyMaster();
        }

        public void SetPaused(bool paused) => AudioListener.pause = paused;

        void ApplyMaster()
        {
            // YouTube mute wins over everything.
            AudioListener.volume = _ytAudioEnabled ? 1f : 0f;
            if (_sfx) _sfx.volume = _sfxVolume;
            // _musicVolume is applied to the music source once music is added.
        }

        public void SetEngine(bool on, float throttle01, float wheelSpeed01)
        {
            if (on != _engineOn)
            {
                _engineOn = on;
                if (on) _engine.Play(); else _engine.Stop();
            }
            if (!on) return;
            _engine.pitch = 0.7f + 0.5f * wheelSpeed01 + 0.25f * throttle01;
            _engine.volume = _sfxVolume * (0.25f + 0.25f * throttle01);
        }

        public void PlayCoin() => _sfx.PlayOneShot(_coin, 0.8f);
        public void PlayFuel() => _sfx.PlayOneShot(_fuel, 1f);
        public void PlayBonus() => _sfx.PlayOneShot(_bonus, 0.9f);
        public void PlayClick() => _sfx.PlayOneShot(_click, 0.8f);
        public void PlayCrash() => _sfx.PlayOneShot(_crash, 1f);

        // ---------- tiny synth ----------
        const int Rate = 22050;

        static AudioClip MakeTone(string name, float seconds, float f0, float f1, float amp)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var d = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2 * Mathf.PI * f / Rate;
                float env = Mathf.Min(1f, i / (Rate * 0.005f)) * (1f - t);
                d[i] = Mathf.Sin((float)phase) * amp * env;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }

        static AudioClip MakeNoise(string name, float seconds, float amp)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var d = new float[n];
            var rng = new System.Random(7);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float white = (float)(rng.NextDouble() * 2 - 1);
                lp += (white - lp) * 0.08f; // low-passed thud
                d[i] = lp * amp * 3f * (1f - t) * (1f - t);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }

        static AudioClip MakeEngineLoop()
        {
            // 0.5 s loop of a rough 55 Hz "engine": fundamental + harmonics, integer cycles so it loops cleanly.
            const float f = 56f;
            int cycles = 28;
            int n = Mathf.RoundToInt(cycles / f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float ph = 2 * Mathf.PI * f * i / Rate;
                d[i] = 0.35f * Mathf.Sin(ph) + 0.2f * Mathf.Sin(2 * ph + 0.3f) + 0.12f * Mathf.Sign(Mathf.Sin(3 * ph)) * 0.5f;
            }
            var clip = AudioClip.Create("engine", n, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
