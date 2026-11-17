using System.Collections;
using Ridgeline.Audio;
using Ridgeline.CameraControl;
using Ridgeline.InputControl;
using Ridgeline.Localization;
using Ridgeline.Platform;
using Ridgeline.Save;
using Ridgeline.UI;
using Ridgeline.Vehicle;
using Ridgeline.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ridgeline.Game
{
    public enum GameState { Boot, Garage, Run, Results }

    /// <summary>
    /// Flow: Boot -> Garage -> Run -> Results -> Garage ...
    ///
    /// Boot order (Playables certification):
    ///   1. boot screen renders            -> firstFrameReady()
    ///   2. getLanguage(), loadData()      (no saving before load succeeds)
    ///   3. world + Jeep built, garage shown -> gameReady()
    /// Pause/resume come only from the SDK (never the Page Visibility API).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Content")]
        public VehicleConfig vehicleConfig;
        public StageConfig stageConfig;
        public Material spriteMaterial;
        public Material groundMaterial;
        [Tooltip("CC0 art and audio. Null = placeholder visuals and synthesised sounds.")]
        public Art.ArtSet art;

        [Header("Scene refs")]
        public PlayablesBridge bridge;
        public FollowCamera followCamera;
        public ParallaxBackground background;
        public TerrainGenerator terrain;
        public RunManager run;
        public AudioService audioService;
        public GameUI ui;

        public GameState State { get; private set; } = GameState.Boot;

        SaveService _save;
        VehicleController _vehicle;
        bool _paused;

        /// <summary>The live vehicle (rebuilt at each run start).</summary>
        public VehicleController Vehicle => _vehicle;
        public SaveData CurrentSave => _save?.Data;
        LayerMask _groundMask;

        const float GarageCameraAnchor = 0.78f;
        const float RunCameraAnchor = 0.42f;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = 1f / 60f;
            _groundMask = LayerMask.GetMask("Ground");
            if (_groundMask == 0) _groundMask = 1; // Default as a fallback

            if (art != null)
            {
                UIFactory.CustomFont = art.font;
                ui.coinIcon = art.hudCoin;
                audioService.UseClips(art);
                if (followCamera != null) followCamera.GetComponent<Camera>().backgroundColor = art.skyColor;
            }
            ui.Build();
            ui.OnDrive = StartRun;
            ui.OnContinue = BackToGarage;
            ui.OnBuyUpgrade = BuyUpgrade;
            ui.OnVolumesChanged = OnVolumesChanged;
            ui.OnClick = audioService.PlayClick;

            run.Ended += OnRunEnded;
            run.Bonus += (label, coins) => { ui.ShowBonus(label); audioService.PlayBonus(); };
            run.PickedUp += p => { if (p.kind == PickupKind.Coin) audioService.PlayCoin(); else audioService.PlayFuel(); };

            bridge.Paused += OnYouTubePause;
            bridge.Resumed += OnYouTubeResume;
            bridge.AudioEnabledChanged += audioService.SetYouTubeAudioEnabled;
            bridge.EscapePressed += () => ui.HandleEscape();
            audioService.SetYouTubeAudioEnabled(bridge.IsAudioEnabled);
        }

        IEnumerator Start()
        {
            // 1. Boot screen is up -> firstFrameReady.
            ui.ShowBoot();
            bridge.SendFirstFrameReady();
            yield return null;

            // 2. Language + cloud save.
            bool languageDone = false;
            bridge.RequestLanguage(lang => { Strings.SetLanguage(lang); languageDone = true; });

            _save = new SaveService(bridge);
            bool loadDone = false;
            _save.LoadedEvent += () => loadDone = true;
            _save.Load();

            float timeout = 8f;
            while ((!loadDone || !languageDone) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (!loadDone) bridge.LogWarning("loadData timed out");

            var data = _save.Data;
            audioService.SetVolumes(data.musicVolume, data.sfxVolume);
            ui.SetVolumeSliders(data.musicVolume, data.sfxVolume);
            // The score YouTube shows must match the best score in the save.
            if (data.bestDistance > 0) bridge.SendScore(data.bestDistance);

            // 3. World + Jeep, garage visible -> gameReady.
            terrain.stage = stageConfig;
            terrain.groundMaterial = groundMaterial;
            terrain.spriteMaterial = spriteMaterial;
            terrain.art = art;
            SpawnVehicle();
            terrain.ResetRun();
            followCamera.verticalAnchor = GarageCameraAnchor;
            followCamera.SnapToTarget();
            if (background != null) background.ResetOrigin();

            EnterGarage();
            yield return null; // let the garage render before declaring ready
            bridge.SendGameReady();
        }

        // ---------------- States ----------------

        void EnterGarage()
        {
            State = GameState.Garage;
            _vehicle.controlsEnabled = false;
            audioService.SetEngine(false, 0, 0);
            followCamera.verticalAnchor = GarageCameraAnchor;
            ui.ShowGarage(_save.Data, _save.LoadFailed);
        }

        void StartRun()
        {
            if (State != GameState.Garage) return;
            // Fresh Jeep at the start line with the current upgrades; same seeded terrain.
            SpawnVehicle();
            terrain.ResetRun();
            followCamera.verticalAnchor = RunCameraAnchor;
            followCamera.SnapToTarget();
            if (background != null) background.ResetOrigin();

            PedalInput.ReleaseAll();
            _vehicle.controlsEnabled = true;
            run.Begin(_vehicle, stageConfig);
            ui.ShowHud(showHint: _save.Data.totalRuns < 2);
            State = GameState.Run;
        }

        void Update()
        {
            if (State == GameState.Run && _vehicle != null)
            {
                ui.UpdateHud(run.Fuel01, run.Distance, run.Coins);
                float wheelSpeed01 = Mathf.Clamp01(Mathf.Abs(_vehicle.rearWheel.angularVelocity) / 1500f);
                audioService.SetEngine(run.Running && run.Fuel01 > 0f && !_vehicle.IsCrashed, _vehicle.Throttle01, wheelSpeed01);
            }
        }

        void OnRunEnded(RunResult r)
        {
            State = GameState.Results;
            _vehicle.controlsEnabled = false;
            audioService.SetEngine(false, 0, 0);
            if (r.reason == EndReason.DriverDown) audioService.PlayCrash();

            var data = _save.Data;
            data.coins += r.Total;
            data.totalRuns++;
            bool newBest = r.distance > data.bestDistance;
            if (newBest)
            {
                data.bestDistance = r.distance;
                bridge.SendScore(data.bestDistance);
            }
            _save.Save();
            ui.ShowResults(r, newBest);
        }

        void BackToGarage()
        {
            if (State != GameState.Results) return;
            SpawnVehicle();
            terrain.ResetRun();
            followCamera.SnapToTarget();
            if (background != null) background.ResetOrigin();
            EnterGarage();
        }

        void BuyUpgrade(UpgradeSlot slot)
        {
            var data = _save.Data;
            int lvl = data.upgrades[(int)slot];
            int cost = UpgradeMath.Cost(lvl);
            if (cost < 0 || data.coins < cost) return;
            data.coins -= cost;
            data.upgrades[(int)slot] = lvl + 1;
            _save.Save();
            ApplyUpgrades();
            ui.RefreshGarage(data, _save.LoadFailed);
        }

        void OnVolumesChanged(float music, float sfx)
        {
            _save.Data.musicVolume = music;
            _save.Data.sfxVolume = sfx;
            audioService.SetVolumes(music, sfx);
            _save.Save();
        }

        // ---------------- Vehicle ----------------

        void SpawnVehicle()
        {
            if (_vehicle != null)
            {
                _vehicle.gameObject.SetActive(false); // out of physics immediately; Destroy is deferred
                Destroy(_vehicle.gameObject);
            }
            float x = 4f;
            // Drop in from just above the ground: wheel anchor depth + wheel radius + a small gap.
            float clearance = -vehicleConfig.rearWheelAnchor.y + vehicleConfig.wheelRadius + 0.15f;
            var pos = new Vector2(x, stageConfig.HeightAt(x) + clearance);
            _vehicle = VehicleBuilder.Build(vehicleConfig, pos, spriteMaterial, _groundMask, art);
            ApplyUpgrades();
            Pickup.VehicleRoot = _vehicle.transform;
            followCamera.target = _vehicle.chassis;
            terrain.target = _vehicle.chassis.transform;
            if (_paused) _vehicle.SetSimulated(false);
        }

        void ApplyUpgrades()
        {
            var u = _save != null ? _save.Data.upgrades : new int[3];
            _vehicle.ApplyUpgrades(u[0], u[1], u[2]);
        }

        // ---------------- YouTube pause / resume ----------------

        void OnYouTubePause()
        {
            if (_paused) return;
            _paused = true;
            PedalInput.ReleaseAll();
            Time.timeScale = 0f;
            if (_vehicle) _vehicle.SetSimulated(false);
            audioService.SetPaused(true);
            // Render (almost) nothing while paused.
            OnDemandRendering.renderFrameInterval = 30;
            _save?.Save(); // short window to persist before eviction
        }

        void OnYouTubeResume()
        {
            if (!_paused) return;
            _paused = false;
            OnDemandRendering.renderFrameInterval = 1;
            Time.timeScale = 1f;
            if (_vehicle) _vehicle.SetSimulated(true);
            audioService.SetPaused(false);
        }
    }
}
