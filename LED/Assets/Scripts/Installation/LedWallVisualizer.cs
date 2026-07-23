// Affichage du mur Glassworks via une texture (résolution = wall-bands / WallMapping).

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class LedWallVisualizer : MonoBehaviour
{
    private float _cellWorldSize = 0.05f;
    [SerializeField] private float glowIntensity = 1.8f;
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f);

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private EntityManager _entityManager;
    private Texture2D _texture;
    private Material _material;
    private readonly Dictionary<int, Vector2Int> _cellByEntity = new Dictionary<int, Vector2Int>();
    private bool _suppressSingleUpdates;

    public EntityManager EntityManager => _entityManager;
    public bool IsBuilt => _texture != null && _entityManager != null;

    public void ApplyDisplayPixels(Color[] pixels)
    {
        if (_texture == null || pixels == null || pixels.Length != _texture.width * _texture.height) return;
        _texture.SetPixels(pixels);
        _texture.Apply();
    }

    public void SetSuppressSingleUpdates(bool suppress) => _suppressSingleUpdates = suppress;

    public void Build(EntityManager entityManager, WallBandsConfig config, float cellWorldSize = 0.05f)
    {
        _cellWorldSize = cellWorldSize;
        _entityManager = entityManager;
        WallMapping.Initialize(config);
        WallMapping.RegisterAllEntities(entityManager);

        BuildCellMap(config.columns);
        CreateWallMesh(config.columns);
        _entityManager.OnColorChanged += OnEntityColorChanged;

        Debug.Log(
            $"[LedWallVisualizer] {WallMapping.TotalEntityCount()} entités enregistrées, " +
            $"{_cellByEntity.Count} cellules visibles, {WallMapping.GetAllWallLedChunks().Count} chunks LEDS");
    }

    private void BuildCellMap(int columns)
    {
        _cellByEntity.Clear();
        for (int row = 0; row < WallMapping.VisibleRows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int? entityId = WallMapping.EntityIdForCell(row, col);
                if (entityId.HasValue)
                    _cellByEntity[entityId.Value] = new Vector2Int(col, row);
            }
        }
    }

    private void CreateWallMesh(int columns)
    {
        int width = columns;
        int height = WallMapping.VisibleRows;

        _texture = new Texture2D(width, height, TextureFormat.RGB24, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = backgroundColor;
        _texture.SetPixels(pixels);
        _texture.Apply();

        float worldWidth = width * _cellWorldSize;
        float worldHeight = height * _cellWorldSize;

        var meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateQuadMesh(worldWidth, worldHeight);

        _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _material.SetTexture(BaseMapId, _texture);
        _material.SetColor(BaseColorId, Color.white);

        var renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;

        transform.localPosition = Vector3.zero;
    }

    /// <summary>Retourne la taille monde du mur (largeur, hauteur).</summary>
    public Vector2 GetWorldSize(int columns)
    {
        return new Vector2(columns * _cellWorldSize, WallMapping.VisibleRows * _cellWorldSize);
    }

    private static Mesh CreateQuadMesh(float width, float height)
    {
        var mesh = new Mesh { name = "LedWallQuad" };
        float hw = width * 0.5f;
        float hh = height * 0.5f;

        mesh.vertices = new[]
        {
            new Vector3(-hw, -hh, 0f),
            new Vector3(hw, -hh, 0f),
            new Vector3(-hw, hh, 0f),
            new Vector3(hw, hh, 0f),
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        return mesh;
    }

    private void OnEntityColorChanged(int id)
    {
        if (_suppressSingleUpdates) return;
        if (!_cellByEntity.TryGetValue(id, out var cell)) return;

        var state = _entityManager.GetColor(id);
        if (state == null) return;

        var color = new Color(
            (state.R / 255f) * glowIntensity,
            (state.G / 255f) * glowIntensity,
            (state.B / 255f) * glowIntensity);

        // row 0 = haut du mur → coordonnée texture inversée en Y
        int texY = WallMapping.VisibleRows - 1 - cell.y;
        _texture.SetPixel(cell.x, texY, color);
        _texture.Apply();
    }

    void OnDestroy()
    {
        if (_entityManager != null)
            _entityManager.OnColorChanged -= OnEntityColorChanged;

        if (_material != null)
            Destroy(_material);
        if (_texture != null)
            Destroy(_texture);
    }
}
