using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomLightingZone : MonoBehaviour
{
    public readonly struct LocalLightConfig
    {
        public LocalLightConfig(Vector3 localPosition, Color color, float intensity, float range)
        {
            LocalPosition = localPosition;
            Color = color;
            Intensity = Mathf.Max(0f, intensity);
            Range = Mathf.Max(0.01f, range);
        }

        public Vector3 LocalPosition { get; }
        public Color Color { get; }
        public float Intensity { get; }
        public float Range { get; }
    }

    private static readonly List<RoomLightingZone> ActiveZones = new List<RoomLightingZone>();

    [SerializeField] private Vector2 horizontalBounds;
    [SerializeField] private Color ambientColor = new Color(0.82f, 0.82f, 0.86f, 1f);
    [SerializeField] [Range(0f, 2f)] private float ambientIntensity = 0.7f;
    [SerializeField] private Color accentColor = new Color(1f, 0.88f, 0.74f, 1f);
    [SerializeField] [Range(0f, 2f)] private float accentIntensity = 0.35f;
    [SerializeField] private Vector3 accentDirection = new Vector3(-0.2f, -0.85f, -0.42f);
    [SerializeField] private Color rimColor = new Color(1f, 0.95f, 0.86f, 1f);
    [SerializeField] [Range(0f, 2f)] private float rimIntensity = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float shadowStrength = 0.58f;
    [SerializeField] [Range(0f, 1f)] private float minimumLight = 0.46f;
    [SerializeField] private Color shadowColor = new Color(0.05f, 0.05f, 0.06f, 1f);
    [SerializeField] [Range(0f, 1f)] private float shadowOpacity = 0.75f;

    private LocalLightConfig[] _localLights = System.Array.Empty<LocalLightConfig>();

    public Vector2 HorizontalBounds => horizontalBounds;
    public Color AmbientColor => ambientColor;
    public float AmbientIntensity => ambientIntensity;
    public Color AccentColor => accentColor;
    public float AccentIntensity => accentIntensity;
    public Vector3 AccentDirection => accentDirection;
    public Color RimColor => rimColor;
    public float RimIntensity => rimIntensity;
    public float ShadowStrength => shadowStrength;
    public float MinimumLight => minimumLight;
    public Color ShadowColor => shadowColor;
    public float ShadowOpacity => shadowOpacity;

    public static RoomLightingZone FindBest(Vector3 worldPosition)
    {
        RoomLightingZone bestZone = null;
        float bestDistance = float.PositiveInfinity;

        for (int index = 0; index < ActiveZones.Count; index++)
        {
            RoomLightingZone zone = ActiveZones[index];
            if (zone == null || !zone.isActiveAndEnabled)
            {
                continue;
            }

            Vector2 bounds = zone.horizontalBounds;
            if (worldPosition.x >= bounds.x && worldPosition.x <= bounds.y)
            {
                return zone;
            }

            float distance = worldPosition.x < bounds.x ? bounds.x - worldPosition.x : worldPosition.x - bounds.y;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestZone = zone;
            }
        }

        return bestZone;
    }

    public void Configure(
        Vector2 newHorizontalBounds,
        Color newAmbientColor,
        float newAmbientIntensity,
        Color newAccentColor,
        float newAccentIntensity,
        Vector3 newAccentDirection,
        Color newRimColor,
        float newRimIntensity,
        float newShadowStrength,
        float newMinimumLight,
        Color newShadowColor,
        float newShadowOpacity,
        params LocalLightConfig[] localLights)
    {
        horizontalBounds = newHorizontalBounds;
        ambientColor = newAmbientColor;
        ambientIntensity = Mathf.Max(0f, newAmbientIntensity);
        accentColor = newAccentColor;
        accentIntensity = Mathf.Max(0f, newAccentIntensity);
        accentDirection = newAccentDirection.sqrMagnitude > 0.0001f ? newAccentDirection.normalized : Vector3.down;
        rimColor = newRimColor;
        rimIntensity = Mathf.Max(0f, newRimIntensity);
        shadowStrength = Mathf.Clamp01(newShadowStrength);
        minimumLight = Mathf.Clamp01(newMinimumLight);
        shadowColor = newShadowColor;
        shadowOpacity = Mathf.Clamp01(newShadowOpacity);
        _localLights = localLights ?? System.Array.Empty<LocalLightConfig>();
    }

    public bool TryGetNearestLocalLight(Vector3 worldPosition, out Vector3 lightPosition, out Color lightColor, out float lightIntensity, out float lightRange)
    {
        lightPosition = Vector3.zero;
        lightColor = Color.black;
        lightIntensity = 0f;
        lightRange = 1f;

        if (_localLights == null || _localLights.Length == 0)
        {
            return false;
        }

        float totalWeight = 0f;
        Vector3 blendedPosition = Vector3.zero;
        Vector4 blendedColor = Vector4.zero;
        float blendedIntensity = 0f;
        float blendedRange = 0f;

        float fallbackDistance = float.PositiveInfinity;
        bool found = false;

        for (int index = 0; index < _localLights.Length; index++)
        {
            LocalLightConfig localLight = _localLights[index];
            Vector3 worldLightPosition = transform.TransformPoint(localLight.LocalPosition);

            float distance = Vector3.Distance(worldLightPosition, worldPosition);
            float influence = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, localLight.Range));
            float weight = influence * influence * Mathf.Max(0.15f, localLight.Intensity);

            if (weight > 0.0001f)
            {
                totalWeight += weight;
                blendedPosition += worldLightPosition * weight;
                blendedColor += (Vector4)(localLight.Color * localLight.Intensity) * weight;
                blendedIntensity += localLight.Intensity * weight;
                blendedRange += localLight.Range * weight;
                found = true;
            }

            if (distance < fallbackDistance)
            {
                fallbackDistance = distance;
                lightPosition = worldLightPosition;
                lightColor = localLight.Color;
                lightIntensity = localLight.Intensity;
                lightRange = localLight.Range;
            }
        }

        if (found && totalWeight > 0.0001f)
        {
            float inverseWeight = 1f / totalWeight;
            lightPosition = blendedPosition * inverseWeight;
            lightColor = (Color)(blendedColor * inverseWeight);
            lightIntensity = blendedIntensity * inverseWeight;
            lightRange = blendedRange * inverseWeight;
        }

        return found;
    }

    private void OnEnable()
    {
        if (!ActiveZones.Contains(this))
        {
            ActiveZones.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
    }
}
