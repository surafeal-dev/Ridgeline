using System.Collections.Generic;
using UnityEngine;

namespace Ridgeline.World
{
    /// <summary>
    /// One recyclable slice of ground: an EdgeCollider2D along the surface and a mesh with two parts.
    ///  - Textured (art present): submesh 0 = grass-top tile following the surface,
    ///    submesh 1 = dirt tile in world space below it, darkening with depth.
    ///  - Untextured fallback: vertex colours only (grass strip + dirt).
    /// Rebuilt in place when the pool moves it.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D))]
    public class TerrainChunk : MonoBehaviour
    {
        public int Index { get; private set; } = int.MinValue;
        public float StartX { get; private set; }
        public float EndX { get; private set; }

        /// <summary>Pickups and decor spawned for this chunk, returned to the pools when the chunk is recycled.</summary>
        public readonly List<Pickup> pickups = new List<Pickup>();
        public readonly List<SpriteRenderer> decor = new List<SpriteRenderer>();

        Mesh _mesh;
        EdgeCollider2D _edge;
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<Vector2> _uvs = new List<Vector2>();
        readonly List<int> _topTris = new List<int>();
        readonly List<int> _bodyTris = new List<int>();
        readonly List<Vector2> _edgePoints = new List<Vector2>();

        void Awake()
        {
            _mesh = new Mesh { name = "TerrainChunk" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            _edge = GetComponent<EdgeCollider2D>();
        }

        /// <param name="tile">World size of one texture tile; also the grass strip thickness when textured.</param>
        public void Build(StageConfig stage, int index, PhysicsMaterial2D groundMaterial, bool textured, float tile)
        {
            Index = index;
            StartX = index * stage.chunkWidth;
            EndX = StartX + stage.chunkWidth;
            transform.position = Vector3.zero;
            gameObject.name = $"Chunk {index}";

            int segments = Mathf.Max(1, Mathf.RoundToInt(stage.chunkWidth / stage.pointSpacing));
            float step = stage.chunkWidth / segments;
            float strip = textured ? tile : stage.grassThickness;

            _verts.Clear(); _colors.Clear(); _uvs.Clear(); _topTris.Clear(); _bodyTris.Clear(); _edgePoints.Clear();

            // Mesh vertex colours are not colour-space converted (SpriteRenderer colours are),
            // so convert them ourselves in a Linear project.
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Color Lin(Color c) => linear ? c.linear : c;
            Color top = textured ? Color.white : Lin(stage.grassColor);
            Color mid = textured ? Color.white : Lin(stage.dirtColor);
            Color deep = textured ? Lin(new Color(0.55f, 0.45f, 0.42f)) : Lin(stage.dirtDeepColor);

            // Per column: 0 = surface, 1 = strip bottom (top submesh), 2 = strip bottom (body submesh), 3 = deep bottom.
            for (int i = 0; i <= segments; i++)
            {
                float x = StartX + i * step;
                float h = stage.HeightAt(x);
                _edgePoints.Add(new Vector2(x, h));

                float u = x / tile;
                float yStrip = h - strip;
                float yDeep = h - stage.groundDepth;
                _verts.Add(new Vector3(x, h, 0f));      _uvs.Add(new Vector2(u, 1f));          _colors.Add(top);
                _verts.Add(new Vector3(x, yStrip, 0f)); _uvs.Add(new Vector2(u, 0f));          _colors.Add(mid);
                // Dirt V is measured down from the strip, so the dirt tile continues the grass tile seamlessly
                // (and its stripes follow the hills).
                _verts.Add(new Vector3(x, yStrip, 0f)); _uvs.Add(new Vector2(u, 0f));                       _colors.Add(mid);
                _verts.Add(new Vector3(x, yDeep, 0f));  _uvs.Add(new Vector2(u, (yDeep - yStrip) / tile)); _colors.Add(deep);

                if (i == 0) continue;
                int a = (i - 1) * 4, b = i * 4;
                // Clockwise winding faces the camera.
                _topTris.Add(a); _topTris.Add(b); _topTris.Add(a + 1);
                _topTris.Add(b); _topTris.Add(b + 1); _topTris.Add(a + 1);
                _bodyTris.Add(a + 2); _bodyTris.Add(b + 2); _bodyTris.Add(a + 3);
                _bodyTris.Add(b + 2); _bodyTris.Add(b + 3); _bodyTris.Add(a + 3);
            }

            _mesh.Clear();
            _mesh.subMeshCount = 2;
            _mesh.SetVertices(_verts);
            _mesh.SetColors(_colors);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(_topTris, 0);
            _mesh.SetTriangles(_bodyTris, 1);
            _mesh.RecalculateBounds();

            _edge.SetPoints(_edgePoints);
            _edge.sharedMaterial = groundMaterial;
            gameObject.SetActive(true);
        }
    }
}
