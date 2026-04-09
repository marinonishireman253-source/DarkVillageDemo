using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCharacterVisual : MonoBehaviour
{
    private const string VisualRootName = "CharacterVisual";

    private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

    [Header("Palette")]
    [SerializeField] private Color coatColor = new Color(0.16f, 0.2f, 0.28f, 1f);
    [SerializeField] private Color trimColor = new Color(0.3f, 0.18f, 0.12f, 1f);
    [SerializeField] private Color scarfColor = new Color(0.75f, 0.31f, 0.26f, 1f);
    [SerializeField] private Color skinColor = new Color(0.88f, 0.74f, 0.62f, 1f);
    [SerializeField] private Color hairColor = new Color(0.12f, 0.07f, 0.05f, 1f);
    [SerializeField] private Color skirtColor = new Color(0.22f, 0.24f, 0.33f, 1f);
    [SerializeField] private Color lanternColor = new Color(0.98f, 0.8f, 0.4f, 1f);

    private PlayerMover _playerMover;
    private Transform _visualRoot;
    private Transform _leftArmPivot;
    private Transform _rightArmPivot;
    private Transform _leftLegPivot;
    private Transform _rightLegPivot;
    private Transform _lanternRoot;
    private Transform _lanternGlow;
    private Renderer _lanternRenderer;
    private Vector3 _visualRootBasePosition;

    private void Awake()
    {
        EnsureBuilt();
    }

    private void OnEnable()
    {
        EnsureBuilt();
    }

    private void LateUpdate()
    {
        EnsureBuilt();
        AnimateVisual(Time.time);
    }

    [ContextMenu("Rebuild Character Visual")]
    public void Rebuild()
    {
        DestroyGeneratedVisual();
        EnsureBuilt();
    }

    private void EnsureBuilt()
    {
        if (_visualRoot != null)
        {
            return;
        }

        if (_playerMover == null)
        {
            _playerMover = GetComponent<PlayerMover>();
        }

        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        Transform existingRoot = transform.Find(VisualRootName);
        if (existingRoot != null)
        {
            CacheRigReferences(existingRoot);
            return;
        }

        BuildVisual();
    }

    private void BuildVisual()
    {
        _visualRoot = CreateNode(VisualRootName, transform, new Vector3(0f, -1f, 0f));
        _visualRootBasePosition = _visualRoot.localPosition;

        CreatePrimitivePart("Torso", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.02f, 0f), new Vector3(0.52f, 0.56f, 0.34f), coatColor);
        CreatePrimitivePart("Waist", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 0.76f, 0f), new Vector3(0.44f, 0.12f, 0.3f), trimColor);
        CreatePrimitivePart("SkirtFront", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 0.55f, 0.08f), new Vector3(0.66f, 0.3f, 0.18f), skirtColor);
        CreatePrimitivePart("SkirtBack", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 0.55f, -0.08f), new Vector3(0.62f, 0.26f, 0.16f), skirtColor);
        CreatePrimitivePart("Scarf", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.28f, 0.08f), new Vector3(0.46f, 0.1f, 0.18f), scarfColor);
        CreatePrimitivePart("ScarfTail", PrimitiveType.Cube, _visualRoot, new Vector3(-0.16f, 1.02f, 0.14f), new Vector3(0.1f, 0.32f, 0.06f), scarfColor, new Vector3(0f, 0f, -8f));

        CreateLeg(out _leftLegPivot, "LeftLegPivot", new Vector3(-0.14f, 0.7f, 0f));
        CreateLeg(out _rightLegPivot, "RightLegPivot", new Vector3(0.14f, 0.7f, 0f));

        CreateArm(out _leftArmPivot, "LeftArmPivot", new Vector3(-0.34f, 1.08f, 0f), coatColor, false);
        CreateArm(out _rightArmPivot, "RightArmPivot", new Vector3(0.34f, 1.08f, 0f), coatColor, true);

        CreatePrimitivePart("Head", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.55f, 0f), new Vector3(0.42f, 0.42f, 0.34f), skinColor);
        CreatePrimitivePart("HairCap", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.67f, -0.02f), new Vector3(0.46f, 0.18f, 0.38f), hairColor);
        CreatePrimitivePart("FrontBangLeft", PrimitiveType.Cube, _visualRoot, new Vector3(-0.12f, 1.53f, 0.12f), new Vector3(0.1f, 0.18f, 0.08f), hairColor);
        CreatePrimitivePart("FrontBangRight", PrimitiveType.Cube, _visualRoot, new Vector3(0.12f, 1.53f, 0.12f), new Vector3(0.1f, 0.18f, 0.08f), hairColor);
        CreatePrimitivePart("HairBack", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.38f, -0.12f), new Vector3(0.3f, 0.42f, 0.12f), hairColor);
        CreatePrimitivePart("Ribbon", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.44f, -0.14f), new Vector3(0.22f, 0.06f, 0.04f), scarfColor);

        CacheRigReferences(_visualRoot);
        AnimateVisual(0f);
    }

    private void CreateLeg(out Transform pivot, string pivotName, Vector3 localPosition)
    {
        pivot = CreateNode(pivotName, _visualRoot, localPosition);
        CreatePrimitivePart("Leg", PrimitiveType.Cube, pivot, new Vector3(0f, -0.26f, 0f), new Vector3(0.14f, 0.42f, 0.14f), new Color(0.16f, 0.17f, 0.2f, 1f));
        CreatePrimitivePart("Sock", PrimitiveType.Cube, pivot, new Vector3(0f, -0.48f, 0f), new Vector3(0.13f, 0.08f, 0.13f), new Color(0.78f, 0.76f, 0.72f, 1f));
        CreatePrimitivePart("Boot", PrimitiveType.Cube, pivot, new Vector3(0f, -0.6f, 0.05f), new Vector3(0.18f, 0.12f, 0.24f), trimColor);
    }

    private void CreateArm(out Transform pivot, string pivotName, Vector3 localPosition, Color sleeveColor, bool attachLantern)
    {
        pivot = CreateNode(pivotName, _visualRoot, localPosition);
        CreatePrimitivePart("UpperArm", PrimitiveType.Cube, pivot, new Vector3(0f, -0.16f, 0f), new Vector3(0.12f, 0.28f, 0.14f), sleeveColor);
        CreatePrimitivePart("Forearm", PrimitiveType.Cube, pivot, new Vector3(0f, -0.38f, 0.02f), new Vector3(0.1f, 0.2f, 0.12f), sleeveColor);
        CreatePrimitivePart("Hand", PrimitiveType.Cube, pivot, new Vector3(0f, -0.52f, 0.04f), new Vector3(0.08f, 0.08f, 0.08f), skinColor);

        if (!attachLantern)
        {
            return;
        }

        _lanternRoot = CreateNode("LanternRoot", pivot, new Vector3(0f, -0.6f, 0.14f));
        CreatePrimitivePart("LanternHandle", PrimitiveType.Cube, _lanternRoot, new Vector3(0f, 0.1f, 0f), new Vector3(0.04f, 0.14f, 0.04f), trimColor);
        CreatePrimitivePart("LanternFrame", PrimitiveType.Cube, _lanternRoot, new Vector3(0f, -0.02f, 0f), new Vector3(0.16f, 0.18f, 0.16f), trimColor);
        _lanternRenderer = CreatePrimitivePart("LanternGlow", PrimitiveType.Cube, _lanternRoot, new Vector3(0f, -0.02f, 0f), new Vector3(0.1f, 0.12f, 0.1f), lanternColor, null, 0.75f).GetComponent<Renderer>();
    }

    private void CacheRigReferences(Transform root)
    {
        if (root == null)
        {
            return;
        }

        _visualRoot = root;
        _visualRootBasePosition = _visualRoot.localPosition;
        _leftArmPivot = root.Find("LeftArmPivot");
        _rightArmPivot = root.Find("RightArmPivot");
        _leftLegPivot = root.Find("LeftLegPivot");
        _rightLegPivot = root.Find("RightLegPivot");
        _lanternRoot = root.Find("RightArmPivot/LanternRoot");
        _lanternGlow = root.Find("RightArmPivot/LanternRoot/LanternGlow");

        _lanternRenderer = _lanternGlow != null ? _lanternGlow.GetComponent<Renderer>() : null;
    }

    private void AnimateVisual(float timeValue)
    {
        if (_visualRoot == null)
        {
            return;
        }

        float moveSpeed = 0f;
        if (_playerMover != null)
        {
            Vector3 planarVelocity = _playerMover.Velocity;
            planarVelocity.y = 0f;
            moveSpeed = planarVelocity.magnitude;
        }

        float moveBlend = Mathf.Clamp01(moveSpeed / 4.2f);
        float cycle = timeValue * Mathf.Lerp(1.5f, 7.5f, moveBlend);
        float sway = Quantize(Mathf.Sin(cycle) * Mathf.Lerp(2f, 18f, moveBlend), 2f);
        float counterSway = Quantize(Mathf.Sin(cycle + Mathf.PI) * Mathf.Lerp(1.5f, 16f, moveBlend), 2f);
        float bob = Quantize((Mathf.Sin(cycle * 2f) * 0.5f + 0.5f) * 0.04f * moveBlend, 0.01f);

        _visualRoot.localPosition = _visualRootBasePosition + new Vector3(0f, bob, 0f);

        if (_leftArmPivot != null)
        {
            _leftArmPivot.localRotation = Quaternion.Euler(6f + sway, 0f, -8f);
        }

        if (_rightArmPivot != null)
        {
            _rightArmPivot.localRotation = Quaternion.Euler(12f + counterSway * 0.4f, 0f, 8f);
        }

        if (_leftLegPivot != null)
        {
            _leftLegPivot.localRotation = Quaternion.Euler(counterSway, 0f, 0f);
        }

        if (_rightLegPivot != null)
        {
            _rightLegPivot.localRotation = Quaternion.Euler(sway, 0f, 0f);
        }

        if (_lanternRoot != null)
        {
            float lanternSwing = Quantize(Mathf.Sin(cycle + 0.6f) * Mathf.Lerp(3f, 14f, moveBlend), 2f);
            _lanternRoot.localRotation = Quaternion.Euler(12f + lanternSwing, 0f, 0f);
        }

        if (_lanternRenderer != null)
        {
            float glowPulse = 0.85f + Mathf.Sin(timeValue * 5.8f) * 0.15f;
            if (_lanternGlow != null)
            {
                _lanternGlow.localScale = new Vector3(0.1f, 0.12f, 0.1f) * Quantize(glowPulse, 0.05f);
            }
        }
    }

    private float Quantize(float value, float step)
    {
        if (step <= 0f)
        {
            return value;
        }

        return Mathf.Round(value / step) * step;
    }

    private Transform CreateNode(string name, Transform parent, Vector3 localPosition)
    {
        return CreateNode(name, parent, localPosition, Vector3.zero);
    }

    private Transform CreateNode(string name, Transform parent, Vector3 localPosition, Vector3 localEulerAngles)
    {
        GameObject node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;
        node.transform.localRotation = Quaternion.Euler(localEulerAngles);
        node.transform.localScale = Vector3.one;
        return node.transform;
    }

    private Transform CreatePrimitivePart(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        return CreatePrimitivePart(name, primitiveType, parent, localPosition, localScale, color, null, 0f);
    }

    private Transform CreatePrimitivePart(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition, Vector3 localScale, Color color, Vector3? localEulerAngles, float emissionStrength = 0f)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEulerAngles ?? Vector3.zero);
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            SafeDestroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.sharedMaterial = GetMaterial(color, emissionStrength);
        }

        return part.transform;
    }

    private Material GetMaterial(Color color, float emissionStrength)
    {
        string cacheKey = ColorUtility.ToHtmlStringRGBA(color) + "_" + emissionStrength.ToString("0.00");
        if (MaterialCache.TryGetValue(cacheKey, out Material cachedMaterial) && cachedMaterial != null)
        {
            return cachedMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (emissionStrength > 0f && material.HasProperty("_EmissionColor"))
        {
            Color emissionColor = color * Mathf.LinearToGammaSpace(emissionStrength);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
        }

        MaterialCache[cacheKey] = material;
        return material;
    }

    private void DestroyGeneratedVisual()
    {
        Transform generatedRoot = transform.Find(VisualRootName);
        if (generatedRoot != null)
        {
            SafeDestroy(generatedRoot.gameObject);
        }

        _visualRoot = null;
        _leftArmPivot = null;
        _rightArmPivot = null;
        _leftLegPivot = null;
        _rightLegPivot = null;
        _lanternRoot = null;
        _lanternGlow = null;
        _lanternRenderer = null;
    }

    private void SafeDestroy(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
