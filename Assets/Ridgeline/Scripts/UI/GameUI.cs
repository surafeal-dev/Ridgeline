using System;
using System.Collections.Generic;
using Ridgeline.Game;
using Ridgeline.InputControl;
using Ridgeline.Localization;
using Ridgeline.Save;
using Ridgeline.Vehicle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Ridgeline.UI.UIFactory;

namespace Ridgeline.UI
{
    /// <summary>
    /// All screens, built from code: Boot, Garage (one screen: Jeep, coins, 3 upgrades, Drive, Settings),
    /// HUD (fuel, distance, coins, pedals), Results, Settings modal.
    /// Rules followed: no exit/quit button, no share prompts, no external links, no global mute,
    /// nothing that looks like YouTube's own close/mute/menu controls, Esc closes modals.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        // Callbacks wired by GameManager.
        public Action OnDrive;
        public Action OnContinue;
        public Action<UpgradeSlot> OnBuyUpgrade;
        public Action<float, float> OnVolumesChanged;
        public Action OnClick;

        RectTransform _safe;
        GameObject _boot, _garage, _hud, _results, _settings;

        // Garage
        Text _garageCoins, _garageBest, _saveWarning;

        /// <summary>Optional coin icon for the HUD (set before Build()).</summary>
        public Sprite coinIcon;
        readonly Dictionary<UpgradeSlot, (Text level, Button buy, Text buyLabel)> _upgradeRows = new Dictionary<UpgradeSlot, (Text, Button, Text)>();

        // HUD
        Image _fuelFill;
        Text _fuelLabel, _hudDistance, _hudCoins, _bonusText, _hintText;
        float _bonusTimer, _hintTimer;

        // Results
        Text _resTitle, _resDistance, _resCoins, _resBonus, _resTotal, _resBest;

        // Settings
        SimpleSlider _music, _sfx;

        public bool SettingsOpen => _settings != null && _settings.activeSelf;

        public void Build()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // "Expand" guarantees the whole 900x600 reference area fits at ANY aspect ratio (9:32 .. 32:9).
            scaler.referenceResolution = new Vector2(900, 600);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            _safe = Stretch(Rect("SafeArea", canvasGo.transform));
            _safe.gameObject.AddComponent<SafeArea>();

            BuildBoot();
            BuildHud();
            BuildGarage();
            BuildResults();
            BuildSettings();
            ShowOnly(_boot);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }

        void ShowOnly(GameObject screen)
        {
            _boot.SetActive(screen == _boot);
            _garage.SetActive(screen == _garage);
            _hud.SetActive(screen == _hud);
            _results.SetActive(screen == _results);
            _settings.SetActive(false);
        }

        // ---------------- Boot ----------------

        void BuildBoot()
        {
            var bg = Image("Boot", _safe, new Color(0.11f, 0.16f, 0.23f));
            Stretch(bg.rectTransform, -2000f); // cover the safe-area margins too
            var title = Text("Title", bg.transform, Strings.Get("title"), 64, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(800, 90));
            var sub = Text("Loading", bg.transform, Strings.Get("loading"), 26, TextDim);
            Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(600, 40));
            _boot = bg.gameObject;
        }

        public void ShowBoot() => ShowOnly(_boot);

        // ---------------- Garage ----------------

        void BuildGarage()
        {
            var root = Stretch(Rect("Garage", _safe));
            _garage = root.gameObject;

            // Panel sits in the lower part; the parked Jeep stays visible above it.
            var panel = Image("Panel", root, Panel, Art.PlaceholderArt.Square);
            Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(860, 400));
            VStack(panel.rectTransform, 10, new RectOffset(24, 24, 18, 18));

            // Header: title + coins + best
            var header = Rect("Header", panel.transform);
            HStack(header, 16, new RectOffset(0, 0, 0, 0));
            Layout(header, minH: 48, prefH: 48);
            var title = Text("Title", header, Strings.Get("garage"), 36, TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            Layout(title, flexW: 1);
            _garageBest = Text("Best", header, "", 24, TextDim, TextAnchor.MiddleRight);
            Layout(_garageBest, prefW: 220);
            _garageCoins = Text("Coins", header, "", 30, Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            Layout(_garageCoins, prefW: 220);

            AddUpgradeRow(panel.transform, UpgradeSlot.Engine, "engine");
            AddUpgradeRow(panel.transform, UpgradeSlot.Suspension, "suspension");
            AddUpgradeRow(panel.transform, UpgradeSlot.Tires, "tires");

            _saveWarning = Text("SaveWarning", panel.transform, Strings.Get("save_off"), 18, Bad);
            Layout(_saveWarning, minH: 22, prefH: 22);
            _saveWarning.gameObject.SetActive(false);

            // Footer: Settings (small) + DRIVE (big)
            var footer = Rect("Footer", panel.transform);
            HStack(footer, 16, new RectOffset(0, 0, 4, 0));
            Layout(footer, minH: 74, prefH: 74);
            var settings = Button("Settings", footer, Strings.Get("settings"), 24, PanelLight, () => { OnClick?.Invoke(); OpenSettings(); }, out _);
            Layout(settings, prefW: 200);
            var drive = Button("Drive", footer, Strings.Get("drive"), 40, Good, () => { OnClick?.Invoke(); OnDrive?.Invoke(); }, out _);
            Layout(drive, flexW: 1);
        }

        void AddUpgradeRow(Transform parent, UpgradeSlot slot, string key)
        {
            var row = Image(key, parent, PanelLight, Art.PlaceholderArt.Square);
            HStack(row.rectTransform, 14, new RectOffset(16, 10, 6, 6));
            Layout(row, minH: 64, prefH: 64);

            var names = Rect("Names", row.transform);
            VStack(names, 0, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
            Layout(names, flexW: 1);
            var n = Text("Name", names, Strings.Get(key), 26, TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            Layout(n, prefH: 30);
            var d = Text("Desc", names, Strings.Get(key + "_desc"), 18, TextDim, TextAnchor.MiddleLeft);
            Layout(d, prefH: 22);

            var level = Text("Level", row.transform, "", 22, TextMain);
            Layout(level, prefW: 150);

            var buy = Button("Buy", row.transform, "", 24, Accent, () => { OnClick?.Invoke(); OnBuyUpgrade?.Invoke(slot); }, out var buyLabel);
            Layout(buy, prefW: 170);
            _upgradeRows[slot] = (level, buy, buyLabel);
        }

        public void ShowGarage(SaveData data, bool saveDisabled)
        {
            RefreshGarage(data, saveDisabled);
            ShowOnly(_garage);
        }

        public void RefreshGarage(SaveData data, bool saveDisabled)
        {
            _garageCoins.text = data.coins.ToString("N0") + " ¢";
            _garageBest.text = Strings.Get("best") + ": " + Strings.Format("meters", data.bestDistance.ToString("N0"));
            _saveWarning.gameObject.SetActive(saveDisabled);
            foreach (var kv in _upgradeRows)
            {
                int lvl = data.upgrades[(int)kv.Key];
                int cost = UpgradeMath.Cost(lvl);
                kv.Value.level.text = Strings.Format("level", lvl, UpgradeMath.MaxLevel);
                if (cost < 0)
                {
                    kv.Value.buyLabel.text = Strings.Get("max");
                    kv.Value.buy.interactable = false;
                }
                else
                {
                    kv.Value.buyLabel.text = cost.ToString("N0") + " ¢";
                    kv.Value.buy.interactable = data.coins >= cost;
                }
            }
        }

        // ---------------- HUD ----------------

        void BuildHud()
        {
            var root = Stretch(Rect("HUD", _safe));
            _hud = root.gameObject;

            // Fuel gauge (top-left)
            var fuelBg = Image("Fuel", root, Panel, Art.PlaceholderArt.Square);
            Place(fuelBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(260, 44));
            _fuelLabel = Text("Label", fuelBg.transform, Strings.Get("fuel"), 18, TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(_fuelLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(60, 30));
            var track = Image("Track", fuelBg.transform, new Color(1, 1, 1, 0.12f), Art.PlaceholderArt.Square);
            Place(track.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(74, 0), new Vector2(174, 20));
            _fuelFill = Image("Fill", track.transform, Good, Art.PlaceholderArt.Square);
            _fuelFill.rectTransform.anchorMin = Vector2.zero;
            _fuelFill.rectTransform.anchorMax = Vector2.one;
            _fuelFill.rectTransform.offsetMin = _fuelFill.rectTransform.offsetMax = Vector2.zero;
            _fuelFill.raycastTarget = false;

            // Distance (top-centre) and coins (top-right)
            // Dark pills behind the numbers so they stay readable over a bright sky.
            var distBg = Image("DistancePill", root, Panel, Art.PlaceholderArt.Square);
            Place(distBg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(200, 44));
            _hudDistance = Text("Distance", distBg.transform, "0 m", 30, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(_hudDistance.rectTransform, 4f);
            var coinBg = Image("CoinsPill", root, Panel, Art.PlaceholderArt.Square);
            Place(coinBg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(180, 44));
            _hudCoins = Text("Coins", coinBg.transform, "0", 28, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(_hudCoins.rectTransform, 4f);
            if (coinIcon != null)
            {
                var icon = Image("CoinIcon", coinBg.transform, Color.white, coinIcon);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(38, 38));
                _hudCoins.rectTransform.offsetMin = new Vector2(40f, 4f);
            }

            // Bonus popup and first-run hint
            _bonusText = Text("Bonus", root, "", 34, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(_bonusText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 50));
            AddShadow(_bonusText);
            var hintBg = Image("HintPill", root, Panel, Art.PlaceholderArt.Square);
            Place(hintBg.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 56));
            hintBg.raycastTarget = false;
            _hintText = Text("Hint", hintBg.transform, Strings.Get("controls_hint"), 26, TextMain);
            Stretch(_hintText.rectTransform, 8f);

            // Pedals: brake bottom-left, gas bottom-right. Big touch targets.
            AddPedal(root, PedalButton.Pedal.Brake, Strings.Get("brake"), new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 16), Bad);
            AddPedal(root, PedalButton.Pedal.Gas, Strings.Get("gas"), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 16), Good);
        }

        static void AddShadow(Text t)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.6f);
            s.effectDistance = new Vector2(2, -2);
        }

        void AddPedal(RectTransform root, PedalButton.Pedal pedal, string label, Vector2 anchor, Vector2 pivot, Vector2 pos, Color color)
        {
            var img = Image(pedal.ToString(), root, new Color(color.r, color.g, color.b, 0.85f), Art.PlaceholderArt.Square);
            Place(img.rectTransform, anchor, pivot, pos, new Vector2(170, 170));
            var t = Text("Label", img.transform, label, 28, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform);
            img.gameObject.AddComponent<PedalButton>().pedal = pedal;
        }

        public void ShowHud(bool showHint)
        {
            ShowOnly(_hud);
            _bonusText.text = "";
            _bonusTimer = 0f;
            _hintText.transform.parent.gameObject.SetActive(showHint);
            _hintTimer = showHint ? 4f : 0f;
        }

        public void UpdateHud(float fuel01, int distance, int coins)
        {
            _fuelFill.rectTransform.anchorMax = new Vector2(fuel01, 1f);
            _fuelFill.color = fuel01 < 0.2f ? Bad : (fuel01 < 0.45f ? Accent : Good);
            _fuelLabel.text = fuel01 < 0.2f && Mathf.Repeat(Time.unscaledTime, 0.6f) < 0.3f ? "" : Strings.Get("fuel");
            _hudDistance.text = Strings.Format("meters", distance.ToString("N0"));
            _hudCoins.text = coinIcon != null ? coins.ToString("N0") : coins.ToString("N0") + " ¢";
        }

        public void ShowBonus(string label)
        {
            _bonusText.text = label;
            _bonusTimer = 1.6f;
        }

        void Update()
        {
            if (_bonusTimer > 0f)
            {
                _bonusTimer -= Time.deltaTime;
                var c = _bonusText.color;
                c.a = Mathf.Clamp01(_bonusTimer / 0.4f);
                _bonusText.color = c;
            }
            if (_hintTimer > 0f)
            {
                _hintTimer -= Time.deltaTime;
                if (_hintTimer <= 0f) _hintText.transform.parent.gameObject.SetActive(false);
            }
        }

        // ---------------- Results ----------------

        void BuildResults()
        {
            var root = Stretch(Rect("Results", _safe));
            _results = root.gameObject;
            Blocker(root, 0.45f);

            var panel = Image("Panel", root, Panel, Art.PlaceholderArt.Square);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 470));
            VStack(panel.rectTransform, 8, new RectOffset(28, 28, 22, 22));

            _resTitle = Text("Title", panel.transform, "", 44, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            Layout(_resTitle, prefH: 60);
            _resBest = Text("Best", panel.transform, Strings.Get("new_best"), 28, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Layout(_resBest, prefH: 34);
            _resDistance = Row(panel.transform, "distance");
            _resCoins = Row(panel.transform, "collected");
            _resBonus = Row(panel.transform, "bonuses");
            _resTotal = Row(panel.transform, "total", Accent);

            var spacer = Rect("Spacer", panel.transform);
            Layout(spacer, minH: 6, prefH: 6);
            var cont = Button("Continue", panel.transform, Strings.Get("continue"), 34, Good, () => { OnClick?.Invoke(); OnContinue?.Invoke(); }, out _);
            Layout(cont, minH: 70, prefH: 70);
        }

        Text Row(Transform parent, string key, Color? valueColor = null)
        {
            var row = Rect(key, parent);
            HStack(row, 10, new RectOffset(0, 0, 0, 0));
            Layout(row, prefH: 40);
            var l = Text("Label", row, Strings.Get(key), 24, TextDim, TextAnchor.MiddleLeft);
            Layout(l, flexW: 1);
            var v = Text("Value", row, "", 28, valueColor ?? TextMain, TextAnchor.MiddleRight, FontStyle.Bold);
            Layout(v, prefW: 220);
            return v;
        }

        public void ShowResults(RunResult r, bool newBest)
        {
            _resTitle.text = Strings.Get(r.reason == EndReason.DriverDown ? "driver_down" : "out_of_fuel");
            _resBest.gameObject.SetActive(newBest);
            _resDistance.text = Strings.Format("meters", r.distance.ToString("N0"));
            _resCoins.text = r.coinsCollected.ToString("N0") + " ¢";
            _resBonus.text = r.bonusCoins.ToString("N0") + " ¢";
            _resTotal.text = "+" + r.Total.ToString("N0") + " ¢";
            ShowOnly(_results);
        }

        // ---------------- Settings ----------------

        void BuildSettings()
        {
            var root = Stretch(Rect("Settings", _safe));
            _settings = root.gameObject;
            Blocker(root, 0.55f);

            var panel = Image("Panel", root, Panel, Art.PlaceholderArt.Square);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 330));
            VStack(panel.rectTransform, 10, new RectOffset(28, 28, 22, 22));

            var title = Text("Title", panel.transform, Strings.Get("settings"), 36, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            Layout(title, prefH: 48);

            var ml = Text("MusicLabel", panel.transform, Strings.Get("music"), 22, TextDim, TextAnchor.MiddleLeft);
            Layout(ml, prefH: 26);
            _music = SimpleSlider.Create(panel.transform, 36);
            var sl = Text("SfxLabel", panel.transform, Strings.Get("sfx"), 22, TextDim, TextAnchor.MiddleLeft);
            Layout(sl, prefH: 26);
            _sfx = SimpleSlider.Create(panel.transform, 36);

            _music.Changed += _ => OnVolumesChanged?.Invoke(_music.Value, _sfx.Value);
            _sfx.Changed += _ => OnVolumesChanged?.Invoke(_music.Value, _sfx.Value);

            var done = Button("Done", panel.transform, Strings.Get("done"), 28, Accent, () => { OnClick?.Invoke(); CloseSettings(); }, out _);
            Layout(done, minH: 60, prefH: 60);
        }

        public void SetVolumeSliders(float music, float sfx)
        {
            _music.SetValueWithoutNotify(music);
            _sfx.SetValueWithoutNotify(sfx);
        }

        public void OpenSettings() => _settings.SetActive(true);
        public void CloseSettings() => _settings.SetActive(false);

        /// <summary>Esc: close the top-most modal. Returns true if something was closed.</summary>
        public bool HandleEscape()
        {
            if (SettingsOpen) { CloseSettings(); return true; }
            return false;
        }
    }
}
