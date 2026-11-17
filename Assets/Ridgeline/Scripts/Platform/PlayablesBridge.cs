using System;
using System.Collections;
using UnityEngine;
using YTGameSDK;

namespace Ridgeline.Platform
{
    /// <summary>
    /// Game-facing facade over Google's YTGameWrapper. Lives on the GameObject named exactly
    /// "YTGameWrapper" (the JS side calls SendMessage on that name).
    ///
    /// Certification rules handled here:
    ///  - firstFrameReady() once the boot screen has rendered; gameReady() only when interactive.
    ///  - onPause / onResume from the SDK only (never the Page Visibility API).
    ///  - isAudioEnabled / onAudioEnabledChange drive the master mute.
    ///  - getLanguage() for the locale (never navigator.language).
    /// </summary>
    [RequireComponent(typeof(YTGameWrapper))]
    public class PlayablesBridge : MonoBehaviour
    {
        public static PlayablesBridge Instance { get; private set; }

        public event Action Paused;
        public event Action Resumed;
        public event Action<bool> AudioEnabledChanged;

        public YTGameWrapper Wrapper { get; private set; }
        public bool IsAudioEnabled { get; private set; } = true;
        public bool IsPaused { get; private set; }
        public string Language { get; private set; } = "en";
        public bool FirstFrameSent { get; private set; }
        public bool GameReadySent { get; private set; }
        public bool InPlayables { get; private set; }

        void Awake()
        {
            // Single-scene game: the bridge lives and dies with the scene. The newest one wins,
            // so a scene reload never ends up talking to a stale or destroyed bridge.
            Instance = this;
            gameObject.name = "YTGameWrapper"; // required by the jslib SendMessage calls
            Wrapper = GetComponent<YTGameWrapper>();
            InPlayables = Wrapper.InPlayablesEnv();

            IsAudioEnabled = Wrapper.IsYTGameAudioEnabled();
            Wrapper.SetOnAudioEnabledChangeCallback(OnAudioChanged);
            Wrapper.SetOnPauseCallback(OnPause);
            Wrapper.SetOnResumeCallback(OnResume);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Call once the boot/loading screen is on screen. Waits for the frame to finish rendering.</summary>
        public void SendFirstFrameReady() => StartCoroutine(FirstFrameRoutine());

        IEnumerator FirstFrameRoutine()
        {
            yield return new WaitForEndOfFrame();
            if (FirstFrameSent) yield break;
            FirstFrameSent = true;
            Wrapper.SendGameFirstFrameReady();
        }

        /// <summary>Call when the player can interact (garage visible, no loading screen). Only once.</summary>
        public void SendGameReady()
        {
            if (GameReadySent) return;
            if (!FirstFrameSent)
            {
                // firstFrameReady MUST come before gameReady.
                FirstFrameSent = true;
                Wrapper.SendGameFirstFrameReady();
            }
            GameReadySent = true;
            Wrapper.SendGameIsReady();
        }

        public void RequestLanguage(Action<string> onLanguage)
        {
            Wrapper.RequestLanguage(lang =>
            {
                Language = string.IsNullOrEmpty(lang) ? "en" : lang;
                onLanguage?.Invoke(Language);
            });
        }

        public void SendScore(int score) => Wrapper.SendGameScore(score);
        public void LogError(string message) => Wrapper.SendYTGameError(message);
        public void LogWarning(string message) => Wrapper.SendYTGameWarning(message);

        /// <summary>Esc pressed. In WebGL the page forwards it (see index.html); in the editor we poll the keyboard.</summary>
        public event Action EscapePressed;

        /// <summary>Called from the page via SendMessage.</summary>
        public void OnEscapeKey() => EscapePressed?.Invoke();

#if UNITY_EDITOR || !UNITY_WEBGL
        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) OnEscapeKey();
        }
#endif

        void OnAudioChanged(bool enabled)
        {
            IsAudioEnabled = enabled;
            AudioEnabledChanged?.Invoke(enabled);
        }

        void OnPause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Paused?.Invoke();
        }

        void OnResume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Resumed?.Invoke();
        }
    }
}
