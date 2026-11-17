using System.Collections.Generic;
using UnityEngine;

namespace Ridgeline.World
{
    /// <summary>
    /// Endless, seeded terrain. Keeps a few chunks ahead of and behind the target,
    /// recycling chunk and pickup objects instead of allocating new ones.
    /// Pickups are placed deterministically, and collected ones stay collected for the run.
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        public StageConfig stage;
        public Transform target;
        public Material groundMaterial;
        public int chunksAhead = 3;
        public int chunksBehind = 1;

        [Header("Visuals")]
        [Tooltip("CC0 art (terrain textures, pickups, decor). Null = placeholder visuals.")]
        public Art.ArtSet art;
        public Material spriteMaterial;
        [Tooltip("Scale of the coin sprite (the Kenney coin fills about half its 1.28 m tile).")]
        public float coinSize = 1.3f;
        public float fuelSize = 0.9f;

        readonly Dictionary<int, TerrainChunk> _active = new Dictionary<int, TerrainChunk>();
        readonly Stack<TerrainChunk> _chunkPool = new Stack<TerrainChunk>();
        readonly Stack<Pickup> _coinPool = new Stack<Pickup>();
        readonly Stack<Pickup> _fuelPool = new Stack<Pickup>();
        readonly Stack<SpriteRenderer> _decorPool = new Stack<SpriteRenderer>();
        readonly HashSet<int> _collected = new HashSet<int>();
        readonly List<float> _fuelPositions = new List<float>();
        readonly List<int> _toRemove = new List<int>();
        PhysicsMaterial2D _groundPhysics;
        int _groundLayer, _pickupLayer;

        public float StartX => 2f;

        void Awake()
        {
            _groundLayer = LayerMask.NameToLayer("Ground");
            _pickupLayer = LayerMask.NameToLayer("Pickup");
            if (_groundLayer < 0) _groundLayer = 0;
            if (_pickupLayer < 0) _pickupLayer = 0;
            Pickup.Collected += OnPickupCollected;
        }

        void OnDestroy() => Pickup.Collected -= OnPickupCollected;

        /// <summary>Resets for a new run: same terrain, all pickups back.</summary>
        public void ResetRun()
        {
            _collected.Clear();
            _fuelPositions.Clear();
            Physics2D.gravity = new Vector2(0f, stage.gravity);
            if (_groundPhysics == null) _groundPhysics = new PhysicsMaterial2D("Ground") { bounciness = 0f };
            _groundPhysics.friction = stage.surfaceFriction;

            foreach (var kv in _active) Recycle(kv.Value);
            _active.Clear();
            UpdateChunks(force: true);
        }

        void LateUpdate() => UpdateChunks(force: false);

        void UpdateChunks(bool force)
        {
            if (stage == null || target == null) return;
            int center = Mathf.FloorToInt(target.position.x / stage.chunkWidth);
            int from = center - chunksBehind, to = center + chunksAhead;

            _toRemove.Clear();
            foreach (var kv in _active)
                if (kv.Key < from || kv.Key > to) _toRemove.Add(kv.Key);
            foreach (var i in _toRemove)
            {
                Recycle(_active[i]);
                _active.Remove(i);
            }

            for (int i = from; i <= to; i++)
            {
                if (_active.ContainsKey(i)) continue;
                var chunk = GetChunk();
                chunk.Build(stage, i, _groundPhysics, Textured, art != null ? art.terrainTileSize : 1f);
                SpawnDecor(chunk);
                SpawnPickups(chunk);
                _active[i] = chunk;
            }
        }

        TerrainChunk GetChunk()
        {
            if (_chunkPool.Count > 0) return _chunkPool.Pop();
            var go = new GameObject("Chunk", typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D), typeof(TerrainChunk));
            go.layer = _groundLayer;
            go.transform.SetParent(transform, false);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterials = Textured
                ? new[] { art.grassMaterial, art.dirtMaterial }
                : new[] { groundMaterial, groundMaterial };
            mr.sortingOrder = 0;
            return go.GetComponent<TerrainChunk>();
        }

        void Recycle(TerrainChunk chunk)
        {
            foreach (var p in chunk.pickups)
            {
                p.gameObject.SetActive(false);
                (p.kind == PickupKind.Coin ? _coinPool : _fuelPool).Push(p);
            }
            chunk.pickups.Clear();
            foreach (var d in chunk.decor)
            {
                d.gameObject.SetActive(false);
                _decorPool.Push(d);
            }
            chunk.decor.Clear();
            chunk.gameObject.SetActive(false);
            _chunkPool.Push(chunk);
        }

        // ---------- Pickups ----------

        void SpawnPickups(TerrainChunk chunk)
        {
            if (chunk.Index < 0) return;
            int slot = 0;

            // Fuel cans: a deterministic sequence x(n+1) = x(n) + spacing(x(n)).
            EnsureFuelPositions(chunk.EndX);
            foreach (float fx in _fuelPositions)
            {
                if (fx < chunk.StartX || fx >= chunk.EndX) continue;
                Place(chunk, PickupKind.Fuel, 0, fx, 1.0f, slot++);
            }

            // Coins: one group per coinGroupSpacing, seeded per group index.
            float spacing = stage.coinGroupSpacing;
            int firstGroup = Mathf.CeilToInt(chunk.StartX / spacing);
            for (int g = Mathf.Max(1, firstGroup); g * spacing < chunk.EndX; g++)
            {
                var rng = new System.Random(stage.seed * 7919 + g * 104729);
                float gx = g * spacing + (float)rng.NextDouble() * spacing * 0.4f;
                if (gx < stage.flatStart) continue;
                if (NearFuel(gx, 6f)) gx += 8f;
                int count = rng.Next(stage.coinsPerGroupMin, stage.coinsPerGroupMax + 1);
                int value = CoinValueAt(gx, rng);
                for (int c = 0; c < count; c++)
                {
                    float x = gx + c * stage.coinSpacing;
                    if (x < chunk.StartX || x >= chunk.EndX) { slot++; continue; }
                    Place(chunk, PickupKind.Coin, value, x, stage.coinHeight, slot++);
                }
            }
        }

        int CoinValueAt(float x, System.Random rng)
        {
            float d = stage.Difficulty(x);
            double r = rng.NextDouble();
            if (r < 0.01 + 0.05 * d) return 500;  // ~2 coins/m early, more later
            if (r < 0.05 + 0.15 * d) return 100;
            if (r < 0.25 + 0.25 * d) return 25;
            return 5;
        }

        bool NearFuel(float x, float range)
        {
            foreach (float fx in _fuelPositions) if (Mathf.Abs(fx - x) < range) return true;
            return false;
        }

        void EnsureFuelPositions(float upTo)
        {
            float x = _fuelPositions.Count == 0 ? stage.flatStart + stage.fuelSpacingStart * 0.6f
                                                : _fuelPositions[_fuelPositions.Count - 1];
            if (_fuelPositions.Count == 0) _fuelPositions.Add(x);
            while (x < upTo)
            {
                x += stage.FuelSpacingAt(x);
                _fuelPositions.Add(x);
            }
        }

        void Place(TerrainChunk chunk, PickupKind kind, int value, float x, float heightAboveGround, int slot)
        {
            int id = chunk.Index * 1000 + slot;
            if (_collected.Contains(id)) return;

            var p = GetPickup(kind);
            p.kind = kind;
            p.value = value;
            p.id = id;
            p.size = kind == PickupKind.Coin ? coinSize : fuelSize;
            p.transform.position = new Vector3(x, stage.HeightAt(x) + heightAboveGround, 0f);
            var sr = p.GetComponent<SpriteRenderer>();
            if (kind == PickupKind.Coin)
            {
                if (art != null) { sr.sprite = art.CoinFor(value); sr.color = Color.white; }
                else sr.color = CoinTint(value);
            }
            p.gameObject.SetActive(true);
            chunk.pickups.Add(p);
        }

        static Color CoinTint(int value)
        {
            switch (value)
            {
                case 500: return new Color(0.75f, 0.55f, 1f);
                case 100: return new Color(1f, 0.45f, 0.35f);
                case 25: return new Color(0.55f, 0.85f, 1f);
                default: return new Color(1f, 0.82f, 0.2f);
            }
        }

        Pickup GetPickup(PickupKind kind)
        {
            var pool = kind == PickupKind.Coin ? _coinPool : _fuelPool;
            if (pool.Count > 0) return pool.Pop();

            var go = new GameObject(kind.ToString(), typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Pickup));
            go.layer = _pickupLayer;
            go.transform.SetParent(transform, false);
            var sr = go.GetComponent<SpriteRenderer>();
            Sprite fuel = art != null ? art.fuelCan : null;
            sr.sprite = kind == PickupKind.Coin ? Art.PlaceholderArt.Circle : (fuel != null ? fuel : Art.PlaceholderArt.Square);
            if (spriteMaterial) sr.sharedMaterial = spriteMaterial;
            sr.sortingOrder = 5;
            if (kind == PickupKind.Fuel && fuel == null) sr.color = new Color(0.9f, 0.2f, 0.2f);
            var col = go.GetComponent<CircleCollider2D>();
            col.radius = kind == PickupKind.Coin ? 0.27f : 0.45f; // local units, scaled by size
            col.isTrigger = true;
            go.SetActive(false);
            return go.GetComponent<Pickup>();
        }

        void OnPickupCollected(Pickup p) => _collected.Add(p.id);

        // ---------- Decor (visual only, no colliders) ----------

        bool Textured => art != null && art.grassMaterial != null && art.dirtMaterial != null;

        void SpawnDecor(TerrainChunk chunk)
        {
            if (art == null || chunk.Index < 0) return;
            var rng = new System.Random(stage.seed * 31337 + chunk.Index * 7907);

            // Trees and bushes behind the road (sorting -3/-2: the terrain covers their sunk bases).
            for (float x = chunk.StartX + (float)rng.NextDouble() * 6f; x < chunk.EndX; x += 5f + (float)rng.NextDouble() * 9f)
            {
                if (x < 8f) continue; // keep the start line clear
                double r = rng.NextDouble();
                if (r < 0.45) AddDecor(chunk, Pick(art.trees, rng), x, 2.8f + (float)rng.NextDouble() * 1.6f, -3, rng);
                else if (r < 0.8) AddDecor(chunk, Pick(art.bushes, rng), x, 0.8f + (float)rng.NextDouble() * 0.5f, -2, rng);
                else AddDecor(chunk, Pick(art.smallProps, rng), x, 0.7f + (float)rng.NextDouble() * 0.3f, -2, rng);
            }

            // Milestone flags every stage.milestoneEvery metres from the start line.
            if (art.milestoneFlag != null && stage.milestoneEvery > 0f)
            {
                const float startLine = 4f; // matches GameManager's spawn x
                int k0 = Mathf.Max(1, Mathf.CeilToInt((chunk.StartX - startLine) / stage.milestoneEvery));
                for (int k = k0; startLine + k * stage.milestoneEvery < chunk.EndX; k++)
                    AddDecor(chunk, art.milestoneFlag, startLine + k * stage.milestoneEvery, 1.6f, -1, null);
            }
        }

        static Sprite Pick(Sprite[] list, System.Random rng) =>
            list == null || list.Length == 0 ? null : list[rng.Next(list.Length)];

        void AddDecor(TerrainChunk chunk, Sprite sprite, float x, float height, int order, System.Random rng)
        {
            if (sprite == null) return;
            var sr = _decorPool.Count > 0 ? _decorPool.Pop() : NewDecor();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.flipX = rng != null && rng.NextDouble() < 0.5;
            float scale = height / sprite.bounds.size.y;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            // Sink the base so it never floats on a slope (the terrain mesh hides the sunk part).
            float halfW = sprite.bounds.size.x * scale * 0.5f;
            float ground = Mathf.Min(stage.HeightAt(x - halfW), stage.HeightAt(x), stage.HeightAt(x + halfW));
            float bottom = (sprite.bounds.center.y - sprite.bounds.extents.y) * scale;
            sr.transform.position = new Vector3(x, ground - 0.15f - bottom, 0f);
            sr.gameObject.SetActive(true);
            chunk.decor.Add(sr);
        }

        SpriteRenderer NewDecor()
        {
            var go = new GameObject("Decor");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (spriteMaterial) sr.sharedMaterial = spriteMaterial;
            return sr;
        }
    }
}
