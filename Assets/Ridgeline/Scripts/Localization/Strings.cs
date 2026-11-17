using System.Collections.Generic;

namespace Ridgeline.Localization
{
    /// <summary>
    /// Minimal string table. English is required and is the fallback.
    /// The language comes from YouTube's getLanguage() (never navigator.language).
    /// Add a language by adding a table and a case in <see cref="SetLanguage"/>.
    /// </summary>
    public static class Strings
    {
        static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            ["title"] = "Ridgeline",
            ["loading"] = "Loading…",
            ["drive"] = "DRIVE",
            ["garage"] = "Garage",
            ["coins"] = "Coins",
            ["best"] = "Best",
            ["engine"] = "Engine",
            ["engine_desc"] = "More power and top speed",
            ["suspension"] = "Suspension",
            ["suspension_desc"] = "Softer, steadier landings",
            ["tires"] = "Tires",
            ["tires_desc"] = "More grip",
            ["level"] = "Level {0}/{1}",
            ["max"] = "MAX",
            ["settings"] = "Settings",
            ["music"] = "Music",
            ["sfx"] = "Sound effects",
            ["done"] = "Done",
            ["gas"] = "GAS",
            ["brake"] = "BRAKE",
            ["fuel"] = "FUEL",
            ["low_fuel"] = "Low fuel!",
            ["driver_down"] = "Driver down!",
            ["out_of_fuel"] = "Out of fuel!",
            ["distance"] = "Distance",
            ["collected"] = "Coins collected",
            ["bonuses"] = "Bonuses",
            ["total"] = "Total earned",
            ["new_best"] = "New best!",
            ["continue"] = "CONTINUE",
            ["flip"] = "Flip! +{0}",
            ["double_flip"] = "{0}x Flip! +{1}",
            ["air_time"] = "Air time! +{0}",
            ["milestone"] = "{0} m! +{1}",
            ["meters"] = "{0} m",
            ["save_off"] = "Progress can't be saved right now.",
            ["controls_hint"] = "Hold GAS to drive. Balance with GAS and BRAKE.",
        };

        static Dictionary<string, string> s_Active = En;

        public static string Language { get; private set; } = "en";

        public static void SetLanguage(string bcp47)
        {
            Language = string.IsNullOrEmpty(bcp47) ? "en" : bcp47;
            // Only English for the MVP; everything falls back to it.
            s_Active = En;
        }

        public static string Get(string key)
        {
            if (s_Active.TryGetValue(key, out var v)) return v;
            if (En.TryGetValue(key, out v)) return v;
            return key;
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);
    }
}
