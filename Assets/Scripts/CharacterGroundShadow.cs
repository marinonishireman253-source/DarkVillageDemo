using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterGroundShadow : MonoBehaviour
{
    private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");

    private static Material s_SharedMaterial;

    [SerializeField] private float verticalOffset = -0.018f;
    [SerializeField] private float widthScale = 0.54f;
    [SerializeField] private float depthScale = 0.28f;
    [SerializeField] private float moveStretch = 0.12f;
    [SerializeField] private float attackStretch = 0.16f;
    [SerializeField] [Range(0f, 1f)] private float softness = 0.42f;
    [SerializeField] [Range(0f, 1f)] private float baseOpacity = 0.42f;

    private MaterialPropertyBlock _propertyBlock;
    private Transform _shadowRoot;
    private MeshRenderer _shadowRenderer;

    public void ApplyFrame(Vector3 origin, float desiredHeight, float moveBlend, float attackBlend, Color shadowColor, float shadowOpacity)
    {
        EnsureShadow();
        if (_shadowRoot == null || _shadowRenderer == null)
        {
            return;
        }

        float width = desiredHeight * widthScale * (1f + moveBlend * moveStretch + attackBlend * attackStretch);
        float depth = desiredHeight * depthScale * (1f + moveBlend * moveStretch * 0.35f - attackBlend * 0.08f);
        _shadowRoot.position = new Vector3(origin.x, origin.y + verticalOffset, origin.z);
        _shadowRoot.rotation = Quaternion.Euler(90f, 0f, 0f);
        _shadowRoot.localScale = new Vector3(width, depth, 1f);

        Color resolvedShadowColor = shadowColor;
        resolvedShadowColor.a *= baseOpacity * shadowOpacity * (1f - attackBlend * 0.12f);

        MaterialPropertyBlock propertyBlock = GetPropertyBlock();
        _shadowRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ShadowColorId, resolvedShadowColor);
        propertyBlock.SetFloat(SoftnessId, softness);
        _shadowRenderer.SetPropertyBlock(propertyBlock);
    }

    private void EnsureShadow()
    {
        if (_shadowRoot != null && _shadowRenderer != null)
        {
            return;
        }

        Transform existing = transform.Find("ContactShadow");
        _shadowRoot = existing;

        if (_shadowRoot == null)
        {
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "ContactShadow";
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = Vector3.zero;
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale = Vector3.one;

            Collider collider = shadow.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            _shadowRoot = shadow.transform;
        }

        _shadowRenderer = _shadowRoot.GetComponent<MeshRenderer>();
        if (_shadowRenderer == null)
        {
            return;
        }

        Material sharedMaterial = GetSharedMaterial();
        if (sharedMaterial != null)
        {
            _shadowRenderer.sharedMaterial = sharedMaterial;
        }

        _shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _shadowRenderer.receiveShadows = false;
        _shadowRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _shadowRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        _shadowRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    private static Material GetSharedMaterial()
    {
        if (s_SharedMaterial != null && s_SharedMaterial.shader != null)
        {
            return s_SharedMaterial;
        }

        Shader shader = Shader.Find("Custom/ContactShadowBlob");
        if (shader == null)
        {
            return null;
        }

        s_SharedMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return s_SharedMaterial;
    }

    private MaterialPropertyBlock GetPropertyBlock()
    {
        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        return _propertyBlock;
    }
}
