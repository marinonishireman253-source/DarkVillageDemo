using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpriteCharacterLighting : MonoBehaviour
{
    private static readonly int AmbientColorId = Shader.PropertyToID("_AmbientColor");
    private static readonly int AmbientIntensityId = Shader.PropertyToID("_AmbientIntensity");
    private static readonly int MainLightColorId = Shader.PropertyToID("_CharacterMainLightColor");
    private static readonly int MainLightDirectionId = Shader.PropertyToID("_CharacterMainLightDirection");
    private static readonly int MainLightStrengthId = Shader.PropertyToID("_CharacterMainLightStrength");
    private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
    private static readonly int AccentDirectionId = Shader.PropertyToID("_AccentDirection");
    private static readonly int AccentStrengthId = Shader.PropertyToID("_AccentStrength");
    private static readonly int LocalLightColorId = Shader.PropertyToID("_LocalLightColor");
    private static readonly int LocalLightPositionId = Shader.PropertyToID("_LocalLightPosition");
    private static readonly int LocalLightRangeId = Shader.PropertyToID("_LocalLightRange");
    private static readonly int LocalLightStrengthId = Shader.PropertyToID("_LocalLightStrength");
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
    private static readonly int ShadowStrengthId = Shader.PropertyToID("_ShadowStrength");
    private static readonly int MinimumLightId = Shader.PropertyToID("_MinimumLight");
    private static readonly int NormalBendXId = Shader.PropertyToID("_NormalBendX");
    private static readonly int NormalBendYId = Shader.PropertyToID("_NormalBendY");

    private static Material s_SharedMaterial;
    private static Light s_CachedDirectionalLight;

    [SerializeField] [Range(0f, 2f)] private float mainLightStrength = 0.38f;
    [SerializeField] [Range(0f, 2f)] private float accentStrengthScale = 1f;
    [SerializeField] [Range(0f, 2f)] private float localLightStrengthScale = 1f;
    [SerializeField] [Range(0f, 2f)] private float rimStrengthScale = 1f;
    [SerializeField] [Range(0f, 4f)] private float normalBendX = 1.2f;
    [SerializeField] [Range(0f, 4f)] private float normalBendY = 1.85f;
    [SerializeField] private bool enableGroundShadow = true;

    private MaterialPropertyBlock _propertyBlock;
    private CharacterGroundShadow _groundShadow;

    public void ApplyFrame(SpriteRenderer spriteRenderer, Transform visualRoot, float desiredHeight, float moveBlend, float attackBlend)
    {
        if (spriteRenderer == null || visualRoot == null)
        {
            return;
        }

        Material sharedMaterial = GetSharedMaterial();
        if (sharedMaterial != null && spriteRenderer.sharedMaterial != sharedMaterial)
        {
            spriteRenderer.sharedMaterial = sharedMaterial;
        }

        RoomLightingZone zone = RoomLightingZone.FindBest(transform.position);
        Vector3 mainLightDirection = Vector3.down;
        Color mainLightColor = Color.black;
        float resolvedMainLightStrength = 0f;

        Light mainDirectionalLight = GetMainDirectionalLight();
        if (mainDirectionalLight != null && mainDirectionalLight.isActiveAndEnabled)
        {
            mainLightDirection = mainDirectionalLight.transform.forward;
            mainLightColor = mainDirectionalLight.color * Mathf.Max(0.01f, mainDirectionalLight.intensity);
            resolvedMainLightStrength = mainLightStrength;
        }

        Color ambientColor = zone != null ? zone.AmbientColor : new Color(0.82f, 0.82f, 0.86f, 1f);
        float ambientIntensity = zone != null ? zone.AmbientIntensity : 0.72f;
        Color accentColor = zone != null ? zone.AccentColor : new Color(1f, 0.9f, 0.78f, 1f);
        Vector3 accentDirection = zone != null ? zone.AccentDirection : new Vector3(-0.25f, -0.84f, -0.42f);
        float accentStrength = (zone != null ? zone.AccentIntensity : 0.3f) * accentStrengthScale;
        Color rimColor = zone != null ? zone.RimColor : new Color(0.98f, 0.96f, 0.92f, 1f);
        float rimStrength = (zone != null ? zone.RimIntensity : 0.22f) * rimStrengthScale;
        float shadowStrength = zone != null ? zone.ShadowStrength : 0.56f;
        float minimumLight = zone != null ? zone.MinimumLight : 0.46f;

        Vector3 localLightPosition = transform.position + Vector3.up * 2.5f;
        Color localLightColor = Color.black;
        float localLightIntensity = 0f;
        float localLightRange = 1f;

        if (zone != null && zone.TryGetNearestLocalLight(transform.position, out Vector3 nearestLightPosition, out Color nearestLightColor, out float nearestLightIntensity, out float nearestLightRange))
        {
            localLightPosition = nearestLightPosition;
            localLightColor = nearestLightColor;
            localLightIntensity = nearestLightIntensity * localLightStrengthScale;
            localLightRange = nearestLightRange;
        }

        MaterialPropertyBlock propertyBlock = GetPropertyBlock();
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(AmbientColorId, ambientColor);
        propertyBlock.SetFloat(AmbientIntensityId, ambientIntensity);
        propertyBlock.SetColor(MainLightColorId, mainLightColor);
        propertyBlock.SetVector(MainLightDirectionId, mainLightDirection);
        propertyBlock.SetFloat(MainLightStrengthId, resolvedMainLightStrength);
        propertyBlock.SetColor(AccentColorId, accentColor);
        propertyBlock.SetVector(AccentDirectionId, accentDirection);
        propertyBlock.SetFloat(AccentStrengthId, accentStrength);
        propertyBlock.SetColor(LocalLightColorId, localLightColor);
        propertyBlock.SetVector(LocalLightPositionId, localLightPosition);
        propertyBlock.SetFloat(LocalLightRangeId, Mathf.Max(0.01f, localLightRange));
        propertyBlock.SetFloat(LocalLightStrengthId, localLightIntensity);
        propertyBlock.SetColor(RimColorId, rimColor);
        propertyBlock.SetFloat(RimStrengthId, rimStrength);
        propertyBlock.SetFloat(ShadowStrengthId, shadowStrength);
        propertyBlock.SetFloat(MinimumLightId, minimumLight);
        propertyBlock.SetFloat(NormalBendXId, normalBendX);
        propertyBlock.SetFloat(NormalBendYId, normalBendY);
        spriteRenderer.SetPropertyBlock(propertyBlock);

        if (!enableGroundShadow)
        {
            return;
        }

        EnsureGroundShadow();
        if (_groundShadow != null)
        {
            Color shadowColor = zone != null ? zone.ShadowColor : new Color(0.05f, 0.05f, 0.06f, 1f);
            float shadowOpacity = zone != null ? zone.ShadowOpacity : 0.72f;
            _groundShadow.ApplyFrame(transform.position, desiredHeight, moveBlend, attackBlend, shadowColor, shadowOpacity);
        }
    }

    private static Material GetSharedMaterial()
    {
        if (s_SharedMaterial != null && s_SharedMaterial.shader != null)
        {
            return s_SharedMaterial;
        }

        Shader shader = Shader.Find("Custom/BillboardCharacterLit");
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

    private static Light GetMainDirectionalLight()
    {
        if (RenderSettings.sun != null)
        {
            s_CachedDirectionalLight = RenderSettings.sun;
            return s_CachedDirectionalLight;
        }

        if (s_CachedDirectionalLight != null)
        {
            return s_CachedDirectionalLight;
        }

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int index = 0; index < lights.Length; index++)
        {
            Light light = lights[index];
            if (light != null && light.type == LightType.Directional)
            {
                s_CachedDirectionalLight = light;
                return s_CachedDirectionalLight;
            }
        }

        return null;
    }

    private void EnsureGroundShadow()
    {
        if (_groundShadow != null)
        {
            return;
        }

        _groundShadow = GetComponent<CharacterGroundShadow>();
        if (_groundShadow == null)
        {
            _groundShadow = gameObject.AddComponent<CharacterGroundShadow>();
        }
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
