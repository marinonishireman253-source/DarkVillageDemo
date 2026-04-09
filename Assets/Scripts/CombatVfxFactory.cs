using UnityEngine;
using UnityEngine.Rendering;

public static class CombatVfxFactory
{
    private const string PunchImpactResourcePrefix = "Vfx/PunchImpact/impact_";
    private const float PunchImpactPixelsPerUnit = 32f;
    private static Material s_LineMaterial;
    private static Sprite[] s_PunchImpactSprites;

    public static void SpawnSlash(Transform source, Color color)
    {
        if (source == null)
        {
            return;
        }

        GameObject root = new GameObject("AttackSlashVfx");
        SlashArcVfx effect = root.AddComponent<SlashArcVfx>();
        effect.Initialize(source, color, GetLineMaterial());
    }

    public static void SpawnHitBurst(Vector3 position, Vector3 attackForward, Color color)
    {
        Sprite[] punchImpactSprites = GetPunchImpactSprites();
        if (HasAnySprite(punchImpactSprites))
        {
            GameObject spriteRoot = new GameObject("HitBurstVfx");
            SpriteHitBurstVfx spriteEffect = spriteRoot.AddComponent<SpriteHitBurstVfx>();
            spriteEffect.Initialize(position, attackForward, color, punchImpactSprites);
            return;
        }

        GameObject fallbackRoot = new GameObject("HitBurstVfx");
        HitBurstVfx fallbackEffect = fallbackRoot.AddComponent<HitBurstVfx>();
        fallbackEffect.Initialize(position, attackForward, color, GetLineMaterial());
    }

    private static Sprite[] GetPunchImpactSprites()
    {
        if (s_PunchImpactSprites != null)
        {
            return s_PunchImpactSprites;
        }

        s_PunchImpactSprites = new Sprite[5];
        for (int index = 0; index < s_PunchImpactSprites.Length; index++)
        {
            string resourcePath = $"{PunchImpactResourcePrefix}{index + 1:00}";
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                s_PunchImpactSprites[index] = sprite;
                continue;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                continue;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            s_PunchImpactSprites[index] = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PunchImpactPixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        return s_PunchImpactSprites;
    }

    private static bool HasAnySprite(Sprite[] sprites)
    {
        if (sprites == null)
        {
            return false;
        }

        foreach (Sprite sprite in sprites)
        {
            if (sprite != null)
            {
                return true;
            }
        }

        return false;
    }

    private static Material GetLineMaterial()
    {
        if (s_LineMaterial != null)
        {
            return s_LineMaterial;
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

        s_LineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = (int)RenderQueue.Transparent + 50
        };
        return s_LineMaterial;
    }
}

internal sealed class SpriteHitBurstVfx : MonoBehaviour
{
    private Sprite[] _sprites;
    private SpriteRenderer _spriteRenderer;
    private Vector3 _position;
    private Vector3 _attackForward = Vector3.forward;
    private Color _baseColor;
    private float _elapsed;

    public void Initialize(Vector3 position, Vector3 attackForward, Color color, Sprite[] sprites)
    {
        _sprites = sprites;
        _position = position;
        _attackForward = Vector3.ProjectOnPlane(attackForward, Vector3.up);
        if (_attackForward.sqrMagnitude <= 0.001f)
        {
            _attackForward = Vector3.forward;
        }
        else
        {
            _attackForward.Normalize();
        }

        _baseColor = color;
        _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        _spriteRenderer.sortingOrder = 140;
        _spriteRenderer.sprite = GetFirstValidSprite();
        _spriteRenderer.color = color;
        UpdateVisual(0f);
    }

    private void LateUpdate()
    {
        _elapsed += Time.deltaTime;
        float duration = 0.18f;
        float t = Mathf.Clamp01(_elapsed / duration);
        UpdateVisual(t);

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateVisual(float normalizedTime)
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        Sprite sprite = GetSpriteForTime(normalizedTime);
        if (sprite != null)
        {
            _spriteRenderer.sprite = sprite;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up);
        }

        float eased = 1f - Mathf.Pow(1f - normalizedTime, 3f);
        float fade = 1f - normalizedTime;
        transform.position = _position
            + Vector3.up * Mathf.Lerp(0.02f, 0.12f, eased)
            + _attackForward * Mathf.Lerp(0.04f, 0.16f, eased);
        transform.localScale = Vector3.one * Mathf.Lerp(2.8f, 3.9f, eased);
        _spriteRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, fade);
    }

    private Sprite GetFirstValidSprite()
    {
        if (_sprites == null)
        {
            return null;
        }

        foreach (Sprite sprite in _sprites)
        {
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private Sprite GetSpriteForTime(float normalizedTime)
    {
        if (_sprites == null || _sprites.Length == 0)
        {
            return null;
        }

        int frameIndex = Mathf.Clamp(Mathf.FloorToInt(normalizedTime * _sprites.Length), 0, _sprites.Length - 1);
        for (int index = frameIndex; index < _sprites.Length; index++)
        {
            if (_sprites[index] != null)
            {
                return _sprites[index];
            }
        }

        for (int index = frameIndex - 1; index >= 0; index--)
        {
            if (_sprites[index] != null)
            {
                return _sprites[index];
            }
        }

        return null;
    }
}

internal sealed class SlashArcVfx : MonoBehaviour
{
    private const int SegmentCount = 14;

    private Transform _source;
    private LineRenderer _coreLine;
    private LineRenderer _glowLine;
    private Color _baseColor;
    private float _elapsed;
    private Quaternion _attackRotation = Quaternion.identity;

    public void Initialize(Transform source, Color color, Material material)
    {
        _source = source;
        _baseColor = color;
        _attackRotation = CombatVfxMath.ResolveAttackRotation(source != null ? source.forward : Vector3.forward);

        _glowLine = CreateLineRenderer("Glow", material, 40);
        _coreLine = CreateLineRenderer("Core", material, 41);
    }

    private void LateUpdate()
    {
        if (_source == null)
        {
            Destroy(gameObject);
            return;
        }

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / 0.18f);
        float eased = EaseOutCubic(t);
        float fade = Mathf.Sin(t * Mathf.PI);
        _attackRotation = CombatVfxMath.ResolveAttackRotation(_source.forward);

        Vector3 center = _source.position
            + Vector3.up * Mathf.Lerp(0.8f, 1.18f, eased)
            + _source.forward * Mathf.Lerp(0.28f, 0.92f, eased);
        float radius = Mathf.Lerp(0.18f, 0.82f, eased);
        float centerAngle = Mathf.Lerp(-50f, 18f, eased);
        float halfSweep = Mathf.Lerp(18f, 56f, eased);

        UpdateArcLine(_glowLine, center, radius * 1.08f, centerAngle, halfSweep, fade * 0.42f, _baseColor, Mathf.Lerp(0.28f, 0.06f, t));
        UpdateArcLine(_coreLine, center, radius, centerAngle, halfSweep, fade * 0.95f, _baseColor, Mathf.Lerp(0.18f, 0.025f, t));

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private LineRenderer CreateLineRenderer(string name, Material material, int sortingOrder)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = SegmentCount;
        lineRenderer.loop = false;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 6;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        lineRenderer.material = material;
        lineRenderer.sortingOrder = sortingOrder;
        return lineRenderer;
    }

    private void UpdateArcLine(
        LineRenderer lineRenderer,
        Vector3 center,
        float radius,
        float centerAngle,
        float halfSweep,
        float alpha,
        Color color,
        float width)
    {
        if (lineRenderer == null)
        {
            return;
        }

        for (int i = 0; i < SegmentCount; i++)
        {
            float pointT = SegmentCount == 1 ? 0f : i / (SegmentCount - 1f);
            float angle = Mathf.Lerp(centerAngle - halfSweep, centerAngle + halfSweep, pointT) * Mathf.Deg2Rad;
            Vector3 localPoint = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                Mathf.Lerp(-0.08f, 0.16f, pointT));
            Vector3 point = center + _attackRotation * localPoint;
            lineRenderer.SetPosition(i, point);
        }

        Color currentColor = new Color(color.r, color.g, color.b, alpha);
        lineRenderer.widthMultiplier = width;
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;
        lineRenderer.enabled = alpha > 0.001f;
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - Mathf.Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }
}

internal sealed class HitBurstVfx : MonoBehaviour
{
    private const int RingSegments = 16;

    private readonly LineRenderer[] _sparkLines = new LineRenderer[5];
    private readonly float[] _sparkAngles = { -62f, -28f, 0f, 26f, 58f };
    private readonly float[] _sparkDistances = { 0.26f, 0.18f, 0.3f, 0.2f, 0.24f };

    private LineRenderer _ringLine;
    private Vector3 _position;
    private Vector3 _attackForward;
    private Color _baseColor;
    private float _elapsed;
    private Quaternion _attackRotation = Quaternion.identity;

    public void Initialize(Vector3 position, Vector3 attackForward, Color color, Material material)
    {
        _position = position;
        _attackForward = attackForward.sqrMagnitude > 0.001f ? attackForward.normalized : Vector3.forward;
        _baseColor = color;
        _attackRotation = CombatVfxMath.ResolveAttackRotation(_attackForward);

        _ringLine = CreateLineRenderer("Ring", material, RingSegments, 42);
        for (int i = 0; i < _sparkLines.Length; i++)
        {
            _sparkLines[i] = CreateLineRenderer($"Spark_{i + 1}", material, 2, 43);
        }
    }

    private void LateUpdate()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / 0.2f);
        float eased = EaseOutCubic(t);
        float fade = 1f - t;
        _attackRotation = CombatVfxMath.ResolveAttackRotation(_attackForward);

        UpdateRing(eased, fade);
        UpdateSparks(eased, fade);

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private LineRenderer CreateLineRenderer(string name, Material material, int positions, int sortingOrder)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = positions;
        lineRenderer.loop = positions > 2;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 3;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        lineRenderer.material = material;
        lineRenderer.sortingOrder = sortingOrder;
        return lineRenderer;
    }

    private void UpdateRing(float eased, float fade)
    {
        if (_ringLine == null)
        {
            return;
        }

        float radius = Mathf.Lerp(0.04f, 0.28f, eased);
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / RingSegments;
            Vector3 localPoint = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                Mathf.Lerp(0.02f, 0.08f, eased));
            Vector3 point = _position + _attackRotation * localPoint;
            _ringLine.SetPosition(i, point);
        }

        Color ringColor = new Color(_baseColor.r, _baseColor.g * 0.92f, _baseColor.b * 0.9f, fade * 0.7f);
        _ringLine.widthMultiplier = Mathf.Lerp(0.16f, 0.02f, eased);
        _ringLine.startColor = ringColor;
        _ringLine.endColor = ringColor;
        _ringLine.enabled = fade > 0.001f;
    }

    private void UpdateSparks(float eased, float fade)
    {
        for (int i = 0; i < _sparkLines.Length; i++)
        {
            LineRenderer spark = _sparkLines[i];
            if (spark == null)
            {
                continue;
            }

            float angle = _sparkAngles[i] * Mathf.Deg2Rad;
            Vector3 localDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
            Vector3 localStart = new Vector3(
                localDirection.x * Mathf.Lerp(0.01f, 0.04f, eased),
                localDirection.y * Mathf.Lerp(0.01f, 0.04f, eased),
                Mathf.Lerp(0.02f, 0.12f, eased));
            Vector3 localEnd = new Vector3(
                localDirection.x * Mathf.Lerp(0.03f, _sparkDistances[i], eased),
                localDirection.y * Mathf.Lerp(0.03f, _sparkDistances[i] * 0.85f, eased),
                Mathf.Lerp(0.06f, 0.18f, eased));
            Vector3 start = _position + _attackRotation * localStart;
            Vector3 end = _position + _attackRotation * localEnd;

            spark.SetPosition(0, start);
            spark.SetPosition(1, end);

            Color sparkColor = new Color(_baseColor.r, _baseColor.g, _baseColor.b, fade * 0.95f);
            spark.widthMultiplier = Mathf.Lerp(0.14f, 0.018f, eased);
            spark.startColor = sparkColor;
            spark.endColor = new Color(_baseColor.r, _baseColor.g * 0.7f, _baseColor.b * 0.65f, 0f);
            spark.enabled = fade > 0.001f;
        }
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - Mathf.Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }
}

internal static class CombatVfxMath
{
    public static Quaternion ResolveAttackRotation(Vector3 forward)
    {
        Vector3 planarForward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (planarForward.sqrMagnitude <= 0.001f)
        {
            planarForward = Vector3.forward;
        }

        return Quaternion.LookRotation(planarForward.normalized, Vector3.up);
    }
}
