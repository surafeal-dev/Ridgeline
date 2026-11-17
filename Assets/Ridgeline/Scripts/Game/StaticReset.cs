using UnityEngine;

namespace Ridgeline.Game
{
    /// <summary>
    /// The editor is set to skip domain reload on Play, so static state survives between play sessions.
    /// Reset every game static here at the start of each session. Harmless in builds.
    /// </summary>
    static class StaticReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            InputControl.PedalInput.ReleaseAll();
            World.Pickup.ResetStatics();
            Localization.Strings.SetLanguage("en");
        }
    }
}
