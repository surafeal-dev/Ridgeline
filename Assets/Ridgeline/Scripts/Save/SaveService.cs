using System;
using Ridgeline.Platform;
using UnityEngine;

namespace Ridgeline.Save
{
    /// <summary>
    /// YouTube cloud save is the ONLY place progress is stored (no PlayerPrefs / IndexedDB / localStorage).
    /// Rules enforced here:
    ///  - saveData is never called before loadData has succeeded (it would be rejected).
    ///  - if loadData fails we keep playing but do not save, so we never overwrite real progress.
    ///  - saves are coalesced: at most one in flight; a newer request is sent when it finishes.
    /// </summary>
    public class SaveService
    {
        public SaveData Data { get; private set; } = new SaveData();
        public bool Loaded { get; private set; }
        public bool LoadFailed { get; private set; }

        public event Action LoadedEvent;

        readonly PlayablesBridge _bridge;
        bool _inFlight;
        bool _pending;

        public SaveService(PlayablesBridge bridge)
        {
            _bridge = bridge;
            _bridge.Wrapper.SetOnSaveResultCallback(OnSaveResult);
            _bridge.Wrapper.SetOnLoadErrorCallback(OnLoadError);
        }

        public void Load()
        {
            _bridge.Wrapper.LoadGameSaveData(json =>
            {
                Data = SaveData.FromJson(json);
                Loaded = true;
                LoadFailed = false;
                LoadedEvent?.Invoke();
            });
        }

        void OnLoadError()
        {
            // Play on with defaults, but never save over a cloud save we could not read.
            Data = new SaveData();
            Loaded = false;
            LoadFailed = true;
            _bridge.LogWarning("loadData failed; saving disabled for this session");
            LoadedEvent?.Invoke();
        }

        /// <summary>Requests a save of the current data. Safe to call often.</summary>
        public void Save()
        {
            if (!Loaded) return;
            if (_inFlight) { _pending = true; return; }
            _inFlight = true;
            _pending = false;
            string json = Data.ToJson();
            _bridge.Wrapper.SendGameSaveData(json);
#if UNITY_EDITOR || !UNITY_WEBGL
            // Outside WebGL there is no async callback; complete immediately.
            OnSaveResult(true);
#endif
        }

        void OnSaveResult(bool ok)
        {
            _inFlight = false;
            if (!ok) Debug.LogWarning("[Ridgeline] Cloud save failed.");
            if (_pending) Save();
        }
    }
}
