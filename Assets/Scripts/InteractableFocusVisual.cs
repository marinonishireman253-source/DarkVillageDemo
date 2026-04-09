using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class InteractableFocusVisual : MonoBehaviour
{
    private sealed class RendererState
    {
        public Renderer Renderer;
        public MaterialPropertyBlock PropertyBlock;
        public bool SupportsBaseColor;
        public bool SupportsColor;
        public bool SupportsEmission;
        public Color BaseColor;
        public Color MainColor;
        public Color EmissionColor;
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private static Material s_RingMaterial;

    [Header("Tint")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private bool tintRenderers = true;
    [SerializeField] private Color highlightColor = new Color(0.97f, 0.83f, 0.58f, 1f);
    [SerializeField] [Range(0f, 1f)] private float tintStrength = 0.34f;
    [SerializeField] private bool boostEmission = true;
    [SerializeField] [Range(0f, 4f)] private float emissionIntensity = 0.75f;

    [Header("Ground Ring")]
    [SerializeField] private bool showGroundRing = true;
    [SerializeField] private Color ringColor = new Color(0.97f, 0.83f, 0.58f, 0.95f);
    [SerializeField] private float ringPadding = 0.22f;
    [SerializeField] private float ringHeight = 0.06f;
    [SerializeField] private float ringWidth = 0.085f;
    [SerializeField] [Range(12, 48)] private int ringSegments = 28;
    [SerializeField] private float pulseSpeed = 3.8f;
    [SerializeField] [Range(0f, 0.2f)] private float pulseAmplitude = 0.06f;

    private readonly List<RendererState> _rendererStates = new List<RendererState>();
    private LineRenderer _ringRenderer;
    private float _highlightBlend;
    private float _targetBlend;
    private bool _initialized;

    private void Awake()
    {
        enabled = false;
    }

    private void Reset()
    {
        CacheTargetRenderers();
    }

    private void OnDisable()
    {
        _targetBlend = 0f;
        _highlightBlend = 0f;

        if (_initialized)
        {
            ApplyRendererBlend(0f);
            UpdateRingVisual(0f, true);
        }
    }

    public void SetFocused(bool isFocused)
    {
        EnsureInitialized();

        _targetBlend = isFocused ? 1f : 0f;
        if (isFocused)
        {
            RebuildRingGeometry();
        }

        enabled = true;
    }

    private void LateUpdate()
    {
        float nextBlend = Mathf.MoveTowards(_highlightBlend, _targetBlend, Time.deltaTime * 7.5f);
        bool changed = !Mathf.Approximately(nextBlend, _highlightBlend);
        _highlightBlend = nextBlend;

        if (changed)
        {
            ApplyRendererBlend(_highlightBlend);
        }

        UpdateRingVisual(_highlightBlend, false);

        if (!changed && Mathf.Abs(_highlightBlend - _targetBlend) <= 0.0001f && _targetBlend <= 0.0001f)
        {
            enabled = false;
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        CacheTargetRenderers();
        CacheRendererStates();

        if (showGroundRing)
        {
            EnsureRingRenderer();
            RebuildRingGeometry();
            UpdateRingVisual(0f, true);
        }

        ApplyRendererBlend(0f);
        _initialized = true;
    }

    private void CacheTargetRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            return;
        }

        List<Renderer> renderers = new List<Renderer>();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || renderer is LineRenderer || renderer is TrailRenderer || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            renderers.Add(renderer);
        }

        targetRenderers = renderers.ToArray();
    }

    private void CacheRendererStates()
    {
        _rendererStates.Clear();

        if (targetRenderers == null)
        {
            return;
        }

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                continue;
            }

            RendererState state = new RendererState
            {
                Renderer = renderer,
                PropertyBlock = new MaterialPropertyBlock(),
                SupportsBaseColor = material.HasProperty(BaseColorId),
                SupportsColor = material.HasProperty(ColorId),
                SupportsEmission = material.HasProperty(EmissionColorId),
                BaseColor = material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white,
                MainColor = material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white,
                EmissionColor = material.HasProperty(EmissionColorId) ? material.GetColor(EmissionColorId) : Color.black
            };

            _rendererStates.Add(state);
        }
    }

    private void ApplyRendererBlend(float blend)
    {
        if (!tintRenderers && !boostEmission)
        {
            return;
        }

        for (int i = 0; i < _rendererStates.Count; i++)
        {
            RendererState state = _rendererStates[i];
            if (state.Renderer == null)
            {
                continue;
            }

            state.Renderer.GetPropertyBlock(state.PropertyBlock);

            if (tintRenderers)
            {
                if (state.SupportsBaseColor)
                {
                    Color targetBaseColor = Color.Lerp(state.BaseColor, highlightColor, tintStrength);
                    state.PropertyBlock.SetColor(BaseColorId, Color.Lerp(state.BaseColor, targetBaseColor, blend));
                }

                if (state.SupportsColor)
                {
                    Color targetMainColor = Color.Lerp(state.MainColor, highlightColor, tintStrength);
                    state.PropertyBlock.SetColor(ColorId, Color.Lerp(state.MainColor, targetMainColor, blend));
                }
            }

            if (boostEmission && state.SupportsEmission)
            {
                Color targetEmission = state.EmissionColor + highlightColor * emissionIntensity;
                state.PropertyBlock.SetColor(EmissionColorId, Color.Lerp(state.EmissionColor, targetEmission, blend));
            }

            state.Renderer.SetPropertyBlock(state.PropertyBlock);
        }
    }

    private void EnsureRingRenderer()
    {
        if (_ringRenderer != null)
        {
            return;
        }

        GameObject ringRoot = new GameObject("FocusRing");
        ringRoot.transform.SetParent(transform, false);

        _ringRenderer = ringRoot.AddComponent<LineRenderer>();
        _ringRenderer.enabled = false;
        _ringRenderer.useWorldSpace = false;
        _ringRenderer.loop = true;
        _ringRenderer.positionCount = Mathf.Max(12, ringSegments);
        _ringRenderer.widthMultiplier = ringWidth;
        _ringRenderer.alignment = LineAlignment.View;
        _ringRenderer.textureMode = LineTextureMode.Stretch;
        _ringRenderer.numCapVertices = 4;
        _ringRenderer.numCornerVertices = 4;
        _ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _ringRenderer.receiveShadows = false;
        _ringRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        _ringRenderer.material = GetRingMaterial();
    }

    private void RebuildRingGeometry()
    {
        if (_ringRenderer == null)
        {
            return;
        }

        Bounds bounds;
        if (!TryGetBounds(out bounds))
        {
            Vector3 fallbackPosition = transform.position + Vector3.up * ringHeight;
            bounds = new Bounds(fallbackPosition, new Vector3(1f, 0.2f, 1f));
        }

        Vector3 center = new Vector3(bounds.center.x, bounds.min.y + ringHeight, bounds.center.z);
        Vector3 localCenter = transform.InverseTransformPoint(center);
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + ringPadding;
        int segmentCount = Mathf.Max(12, ringSegments);

        _ringRenderer.positionCount = segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segmentCount;
            Vector3 point = localCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            _ringRenderer.SetPosition(i, point);
        }
    }

    private bool TryGetBounds(out Bounds bounds)
    {
        bool foundBounds = false;
        bounds = default;

        foreach (Collider colliderComponent in GetComponentsInChildren<Collider>(true))
        {
            if (colliderComponent == null || colliderComponent.isTrigger)
            {
                continue;
            }

            if (!foundBounds)
            {
                bounds = colliderComponent.bounds;
                foundBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliderComponent.bounds);
            }
        }

        if (foundBounds)
        {
            return true;
        }

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!foundBounds)
            {
                bounds = renderer.bounds;
                foundBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return foundBounds;
    }

    private void UpdateRingVisual(float blend, bool forceHidden)
    {
        if (_ringRenderer == null)
        {
            return;
        }

        if (forceHidden || !showGroundRing || blend <= 0.0001f)
        {
            _ringRenderer.enabled = false;
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        float alpha = ringColor.a * blend;
        Color currentRingColor = new Color(ringColor.r, ringColor.g, ringColor.b, alpha);

        _ringRenderer.enabled = true;
        _ringRenderer.widthMultiplier = ringWidth * pulse;
        _ringRenderer.startColor = currentRingColor;
        _ringRenderer.endColor = currentRingColor;
    }

    private static Material GetRingMaterial()
    {
        if (s_RingMaterial != null)
        {
            return s_RingMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        s_RingMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return s_RingMaterial;
    }
}
