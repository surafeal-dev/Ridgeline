using UnityEngine;

namespace Ridgeline.Art
{
    /// <summary>
    /// Every imported art/audio asset the game uses, in one place. Filled by the editor menu
    /// Ridgeline > Build Art Set (see ArtSetBuilder). Any empty slot falls back to placeholder art,
    /// so the game still runs if a file is missing.
    /// Sources and licences: Assets/Ridgeline/Art/CREDITS.md (all CC0).
    /// </summary>
    [CreateAssetMenu(menuName = "Ridgeline/Art Set", fileName = "ArtSet")]
    public class ArtSet : ScriptableObject
    {
        [Header("Vehicle")]
        public Sprite jeepBody;
        public Sprite jeepTire;

        [Header("Driver")]
        public Sprite driverHead;
        public Sprite driverHeadHurt;
        public Sprite driverBody;
        public Sprite driverArm;

        [Header("Terrain (textures set to Repeat)")]
        public Material grassMaterial;
        public Material dirtMaterial;
        [Tooltip("World size in metres of one terrain texture tile.")]
        public float terrainTileSize = 1.6f;

        [Header("Background")]
        public Material farBackgroundMaterial;
        public Color skyColor = new Color(0.82f, 0.93f, 0.98f);

        [Header("Pickups")]
        public Sprite coinBronze;
        public Sprite coinSilver;
        public Sprite coinGold;
        public Sprite coinGem;
        public Sprite fuelCan;

        [Header("Decor")]
        public Sprite[] trees;
        public Sprite[] bushes;
        public Sprite[] smallProps;
        public Sprite milestoneFlag;

        [Header("UI")]
        public Font font;
        public Sprite hudCoin;

        [Header("Audio")]
        public AudioClip coin;
        public AudioClip fuel;
        public AudioClip bonus;
        public AudioClip click;
        public AudioClip crash;
        public AudioClip bump;

        public Sprite CoinFor(int value)
        {
            Sprite s = value >= 500 ? coinGem : value >= 100 ? coinGold : value >= 25 ? coinSilver : coinBronze;
            return s != null ? s : PlaceholderArt.Circle;
        }
    }
}
