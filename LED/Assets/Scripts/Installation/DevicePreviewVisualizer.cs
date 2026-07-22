// Preview 3D projecteur + 4 lyres (état DeviceManager, hors réseau).
// Visible en Play Mode et Preview Timeline (Edit Mode).

using UnityEngine;
using UnityEngine.Playables;

[ExecuteAlways]
public class DevicePreviewVisualizer : MonoBehaviour
{
    private const int SegmentCount = 6;

    [SerializeField] private DeviceManager deviceManager;
    [SerializeField] private float wallHalfExtent = 3.2f;
    [SerializeField] private float beamMaxLength = 2.8f;

    private Transform _previewRoot;
    private Transform[] _roots;
    private Transform[] _heads;
    private Renderer[] _lenses;
    private Transform[] _flux;
    private MaterialPropertyBlock _block;
    private double _lastEditTime = double.NaN;

    /// <summary>Centre monde recommandé pour la caméra (mur + devices).</summary>
    public Vector3 RecommendedViewCenter { get; private set; }

    /// <summary>Demi-hauteur monde à couvrir pour voir mur + devices.</summary>
    public float RecommendedHalfHeight { get; private set; } = 4f;

    public void Initialize(DeviceManager manager, float wallWorldWidth)
    {
        deviceManager = manager;
        wallHalfExtent = Mathf.Max(0.5f, wallWorldWidth * 0.5f);
        beamMaxLength = Mathf.Max(1.8f, wallHalfExtent * 0.55f);
        Rebuild();
        RefreshFromManager();
    }

    /// <summary>Appelé par le mixer Timeline (Edit Preview + Play).</summary>
    public void RefreshFromManager()
    {
        if (deviceManager == null)
            deviceManager = FindFirstObjectByType<DeviceManager>();
        if (deviceManager == null) return;
        BuildIfNeeded();
        for (byte id = 0; id < DeviceManager.DeviceCount; id++)
            ApplyVisual(id, deviceManager.GetDevice(id));
    }

    void OnEnable()
    {
        if (deviceManager == null)
            deviceManager = FindFirstObjectByType<DeviceManager>();
        BuildIfNeeded();
    }

    void Update()
    {
        // Edit Mode : sync uniquement si le temps Timeline change (évite freeze éditeur)
        if (Application.isPlaying) return;

        var director = FindFirstObjectByType<PlayableDirector>();
        if (director == null || director.playableAsset == null) return;

        if (deviceManager == null)
            deviceManager = FindFirstObjectByType<DeviceManager>();
        if (deviceManager == null) return;

        if (!double.IsNaN(_lastEditTime) && System.Math.Abs(director.time - _lastEditTime) < 0.0001)
            return;
        _lastEditTime = director.time;

        EntityTextTrackBinder.DriveDevicesFromTimeline(director, deviceManager);
        RefreshFromManager();
    }

    void LateUpdate()
    {
        // Play Mode : sync continue depuis DeviceManager (alimenté par le mixer)
        if (!Application.isPlaying) return;
        RefreshFromManager();
    }

    private void BuildIfNeeded()
    {
        if (_roots != null && _roots.Length == DeviceManager.DeviceCount && _roots[0] != null)
            return;
        Rebuild();
    }

    private void Rebuild()
    {
        if (_previewRoot != null)
            DestroyImmediateSafe(_previewRoot.gameObject);

        var existing = transform.Find("DevicesPreview");
        if (existing != null)
            DestroyImmediateSafe(existing.gameObject);

        _block = new MaterialPropertyBlock();
        _roots = new Transform[DeviceManager.DeviceCount];
        _heads = new Transform[DeviceManager.DeviceCount];
        _lenses = new Renderer[DeviceManager.DeviceCount];
        _flux = new Transform[DeviceManager.DeviceCount];

        var parent = new GameObject("DevicesPreview");
        parent.transform.SetParent(transform, false);
        // Parent persisté (DeviceManager) : enfants visibles en Hierarchy Edit Mode
        parent.hideFlags = HideFlags.DontSave;
        _previewRoot = parent.transform;

        // Rangée SOUS le mur, assez proche pour rester dans le cadre caméra
        float gap = 0.55f;
        float projectorY = -wallHalfExtent - gap;
        float lyreY = projectorY - 0.85f;
        float span = wallHalfExtent * 0.92f;
        float z = -0.35f;

        RecommendedViewCenter = new Vector3(0f, (lyreY + wallHalfExtent) * 0.5f, 0f);
        RecommendedHalfHeight = wallHalfExtent + Mathf.Abs(lyreY) + 0.9f;

        _roots[0] = CreateProjector(parent.transform, new Vector3(0f, projectorY, z));

        float[] xs = { -span, -span / 3f, span / 3f, span };
        for (byte id = 1; id <= 4; id++)
            _roots[id] = CreateLyre(parent.transform, id, new Vector3(xs[id - 1], lyreY, z));
    }

    private Transform CreateProjector(Transform parent, Vector3 pos)
    {
        var root = new GameObject("Projector_RGBW");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.hideFlags = HideFlags.DontSave;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.hideFlags = HideFlags.DontSave;
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.7f, 0.28f, 0.4f);
        DestroyCollider(body);
        ApplyUnlit(body.GetComponent<Renderer>(), new Color(0.45f, 0.45f, 0.5f));

        var lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lens.name = "Lens";
        lens.hideFlags = HideFlags.DontSave;
        lens.transform.SetParent(root.transform, false);
        lens.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        lens.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);
        DestroyCollider(lens);
        _lenses[0] = lens.GetComponent<Renderer>();
        ApplyUnlit(_lenses[0], new Color(0.2f, 0.2f, 0.22f));

        var head = new GameObject("BeamPivot");
        head.hideFlags = HideFlags.DontSave;
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.24f, 0f);
        _heads[0] = head.transform;
        CreateFlux(head.transform, 0);
        return root.transform;
    }

    private Transform CreateLyre(Transform parent, byte id, Vector3 pos)
    {
        var root = new GameObject($"Lyre_{id}");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.hideFlags = HideFlags.DontSave;

        var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseGo.name = "Base";
        baseGo.hideFlags = HideFlags.DontSave;
        baseGo.transform.SetParent(root.transform, false);
        baseGo.transform.localScale = new Vector3(0.28f, 0.09f, 0.28f);
        DestroyCollider(baseGo);
        ApplyUnlit(baseGo.GetComponent<Renderer>(), new Color(0.35f, 0.35f, 0.4f));

        var yoke = new GameObject("Yoke");
        yoke.hideFlags = HideFlags.DontSave;
        yoke.transform.SetParent(root.transform, false);
        yoke.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        _heads[id] = yoke.transform;

        var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Arm";
        arm.hideFlags = HideFlags.DontSave;
        arm.transform.SetParent(yoke.transform, false);
        arm.transform.localScale = new Vector3(0.1f, 0.32f, 0.1f);
        arm.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        DestroyCollider(arm);
        ApplyUnlit(arm.GetComponent<Renderer>(), new Color(0.55f, 0.55f, 0.6f));

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.hideFlags = HideFlags.DontSave;
        head.transform.SetParent(yoke.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        head.transform.localScale = new Vector3(0.28f, 0.28f, 0.34f);
        DestroyCollider(head);
        _lenses[id] = head.GetComponent<Renderer>();
        ApplyUnlit(_lenses[id], new Color(0.2f, 0.2f, 0.22f));

        CreateFlux(yoke.transform, id);
        return root.transform;
    }

    private void CreateFlux(Transform head, byte id)
    {
        var flux = new GameObject("Flux");
        flux.hideFlags = HideFlags.DontSave;
        flux.transform.SetParent(head, false);
        flux.transform.localPosition = new Vector3(0f, id == 0 ? 0.06f : 0.48f, 0f);

        float baseR = id == 0 ? 0.22f : 0.13f;
        for (int s = 0; s < SegmentCount; s++)
        {
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seg.name = $"Seg_{s}";
            seg.hideFlags = HideFlags.DontSave;
            seg.transform.SetParent(flux.transform, false);
            DestroyCollider(seg);
            float t = (s + 0.5f) / SegmentCount;
            float radius = baseR * Mathf.Lerp(1f, 2.4f, t);
            seg.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
            ApplyUnlit(seg.GetComponent<Renderer>(), new Color(1f, 1f, 1f, 0.35f));
            SetTransparentSetup(seg.GetComponent<Renderer>());
        }

        _flux[id] = flux.transform;
        flux.transform.localScale = Vector3.zero;
    }

    private void ApplyVisual(byte id, DeviceState state)
    {
        if (_roots == null || id >= _roots.Length || _roots[id] == null) return;

        float intensity = (state.dimmer / 255f) * (state.shutter > 0 ? Mathf.Clamp01(state.shutter / 40f) : 0f);
        intensity = Mathf.Clamp01(intensity);

        // Caméra ortho face au mur (plan XY) : une rotation Y est invisible.
        // pan → Z (balayage gauche/droite à l'écran), tilt → X (pencher le faisceau).
        float panDeg = (state.pan - 128) / 128f * 75f;
        float tiltDeg = (state.tilt - 128) / 128f * 50f;
        if (_heads[id] != null)
            _heads[id].localRotation = Quaternion.Euler(-tiltDeg, 0f, -panDeg);

        Color color;
        if (id == 0)
        {
            color = new Color(state.r / 255f, state.g / 255f, state.b / 255f, 1f);
            color = Color.Lerp(color, Color.white, state.w / 255f * 0.4f);
        }
        else if (intensity < 0.02f)
        {
            SetColor(_lenses[id], new Color(0.25f, 0.25f, 0.28f));
            if (_flux[id] != null) _flux[id].localScale = Vector3.zero;
            return;
        }
        else
        {
            // Teinte depuis colorWheel réel (DeviceShow envoie 4 roues distinctes)
            float hue = (state.colorWheel / 255f) % 1f;
            color = Color.HSVToRGB(Mathf.Clamp01(hue * 0.95f + 0.02f), 0.95f, 1f);
            if (state.r > 0)
                color = Color.Lerp(color, new Color(1f, 0.15f, 0.05f), state.r / 255f * 0.35f);
        }

        // Projecteur : blink très lisible (lentille + faisceau disparaissent)
        if (id == 0)
        {
            bool on = intensity >= 0.02f;
            if (_lenses[0] != null)
            {
                _lenses[0].enabled = on;
                float pulse = on ? 1.15f : 0.01f;
                _lenses[0].transform.localScale = new Vector3(0.45f * pulse, 0.06f, 0.45f * pulse);
            }
            if (_roots[0] != null)
                _roots[0].localScale = on ? Vector3.one : new Vector3(1f, 0.35f, 1f);
            if (!on)
            {
                if (_flux[0] != null) _flux[0].localScale = Vector3.zero;
                return;
            }
        }

        SetColor(_lenses[id], Color.Lerp(new Color(0.15f, 0.15f, 0.16f), color, 0.25f + 0.75f * intensity));

        if (_flux[id] == null) return;

        if (intensity < 0.02f)
        {
            _flux[id].localScale = Vector3.zero;
            return;
        }

        _flux[id].localScale = Vector3.one;
        float length = beamMaxLength * Mathf.Lerp(0.45f, 1f, intensity);
        float segH = length / SegmentCount;
        float baseR = (id == 0 ? 0.22f : 0.13f) * Mathf.Lerp(0.75f, 1.15f, intensity);

        for (int s = 0; s < SegmentCount; s++)
        {
            var seg = _flux[id].Find($"Seg_{s}");
            if (seg == null) continue;

            float t = (s + 0.5f) / SegmentCount;
            float alpha = Mathf.Lerp(0.85f, 0.1f, t * t) * intensity;
            float radius = baseR * Mathf.Lerp(1f, 2.5f, t);

            seg.localScale = new Vector3(radius * 2f, segH * 0.5f, radius * 2f);
            seg.localPosition = new Vector3(0f, (s + 0.5f) * segH, 0f);
            SetTransparentColor(seg.GetComponent<Renderer>(), color, alpha);
        }
    }

    private static void SetTransparentSetup(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null) return;
        var mat = renderer.sharedMaterial;
        mat.SetOverrideTag("RenderType", "Transparent");
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void ApplyUnlit(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        renderer.sharedMaterial = mat;
    }

    private void SetColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        renderer.GetPropertyBlock(_block);
        _block.SetColor("_BaseColor", color);
        _block.SetColor("_Color", color);
        renderer.SetPropertyBlock(_block);
    }

    private void SetTransparentColor(Renderer renderer, Color color, float alpha)
    {
        if (renderer == null) return;
        color.a = Mathf.Clamp01(alpha);
        renderer.GetPropertyBlock(_block);
        _block.SetColor("_BaseColor", color);
        _block.SetColor("_Color", color);
        renderer.SetPropertyBlock(_block);
    }

    private static void DestroyCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediateSafe(col);
    }

    private static void DestroyImmediateSafe(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Object.Destroy(obj);
        else Object.DestroyImmediate(obj);
    }
}
