using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PuddleZone : MonoBehaviour
{
    private const string WetFloorShaderName = "Custom/WetFloor";
    private const float SurfaceY = 0.265f;
    private const float SlowMultiplier = 0.6f;

    private static readonly Dictionary<PlayerMover, HashSet<PuddleZone>> ActivePlayers = new Dictionary<PlayerMover, HashSet<PuddleZone>>();
    private static Material s_PuddleMaterial;

    private float _width;
    private float _depth;
    private BoxCollider _trigger;

    public void Configure(Vector3 center, float width, float depth)
    {
        _width = Mathf.Max(0.5f, width);
        _depth = Mathf.Max(0.5f, depth);

        transform.position = new Vector3(center.x, SurfaceY, center.z);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        EnsureVisual();
        EnsureTrigger();
        EnsureRipples();
        EnsureRainImpacts();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMover mover = other.GetComponentInParent<PlayerMover>();
        if (mover == null)
        {
            return;
        }

        if (!ActivePlayers.TryGetValue(mover, out HashSet<PuddleZone> zones))
        {
            zones = new HashSet<PuddleZone>();
            ActivePlayers.Add(mover, zones);
        }

        if (zones.Add(this))
        {
            mover.SetSpeedMultiplier(SlowMultiplier);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMover mover = other.GetComponentInParent<PlayerMover>();
        if (mover == null)
        {
            return;
        }

        RemovePlayer(mover);
    }

    private void OnDisable()
    {
        if (ActivePlayers.Count == 0)
        {
            return;
        }

        List<PlayerMover> movers = new List<PlayerMover>(ActivePlayers.Keys);
        foreach (PlayerMover mover in movers)
        {
            if (mover == null)
            {
                ActivePlayers.Remove(mover);
                continue;
            }

            RemovePlayer(mover);
        }
    }

    private void EnsureVisual()
    {
        Transform existingVisual = transform.Find("Visual");
        GameObject visual = existingVisual != null ? existingVisual.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        visual.transform.localScale = new Vector3(_width, _depth, 1f);

        Collider quadCollider = visual.GetComponent<Collider>();
        if (quadCollider != null)
        {
            DestroyImmediate(quadCollider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.material = GetOrCreatePuddleMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.material.renderQueue = 3002;
    }

    private void EnsureTrigger()
    {
        _trigger = GetComponent<BoxCollider>();
        if (_trigger == null)
        {
            _trigger = gameObject.AddComponent<BoxCollider>();
        }

        _trigger.isTrigger = true;
        _trigger.center = new Vector3(0f, 0.45f, 0f);
        _trigger.size = new Vector3(_width, 1.1f, _depth);
    }

    private void EnsureRipples()
    {
        Transform rippleTransform = transform.Find("DenseRipples");
        ParticleSystem ripplePS = rippleTransform != null ? rippleTransform.GetComponent<ParticleSystem>() : null;
        if (ripplePS == null)
        {
            GameObject rippleGO = new GameObject("DenseRipples");
            rippleGO.transform.SetParent(transform, false);
            rippleGO.transform.localPosition = Vector3.zero;
            rippleGO.transform.localRotation = Quaternion.identity;
            ripplePS = rippleGO.AddComponent<ParticleSystem>();
        }

        var main = ripplePS.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.68f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.18f);
        main.startColor = new Color(0.56f, 0.64f, 0.72f, 0.28f);
        main.maxParticles = 256;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = ripplePS.emission;
        emission.rateOverTime = Mathf.Clamp(_width * _depth * 1.1f, 10f, 30f);

        var shape = ripplePS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(_width * 0.92f, 0.01f, _depth * 0.92f);

        var sizeOverLifetime = ripplePS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.2f, 0.45f),
                new Keyframe(1f, 1.15f)));

        var colorOverLifetime = ripplePS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.58f, 0.67f, 0.75f), 0f),
                new GradientColorKey(new Color(0.48f, 0.56f, 0.62f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.26f, 0f),
                new GradientAlphaKey(0.12f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = ripplePS.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        renderer.material = new Material(shader);
        renderer.material.color = new Color(0.56f, 0.64f, 0.72f, 0.28f);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void EnsureRainImpacts()
    {
        Transform impactTransform = transform.Find("RainImpacts");
        ParticleSystem impactPS = impactTransform != null ? impactTransform.GetComponent<ParticleSystem>() : null;
        if (impactPS == null)
        {
            GameObject impactGO = new GameObject("RainImpacts");
            impactGO.transform.SetParent(transform, false);
            impactGO.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            impactGO.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            impactPS = impactGO.AddComponent<ParticleSystem>();
        }

        var main = impactPS.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
        main.startColor = new Color(0.72f, 0.8f, 0.88f, 0.34f);
        main.maxParticles = 128;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.15f;

        var emission = impactPS.emission;
        emission.rateOverTime = Mathf.Clamp(_width * _depth * 0.42f, 3f, 10f);

        var shape = impactPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(_width * 0.9f, 0.01f, _depth * 0.9f);

        var colorOverLifetime = impactPS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.74f, 0.82f, 0.9f), 0f),
                new GradientColorKey(new Color(0.56f, 0.64f, 0.72f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.34f, 0f),
                new GradientAlphaKey(0.18f, 0.42f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = impactPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.6f, 1f),
                new Keyframe(1f, 0.25f)));

        ParticleSystemRenderer renderer = impactPS.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        renderer.material = new Material(shader);
        renderer.material.color = new Color(0.72f, 0.8f, 0.88f, 0.34f);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Material GetOrCreatePuddleMaterial()
    {
        if (s_PuddleMaterial != null)
        {
            return s_PuddleMaterial;
        }

        Shader wetShader = Shader.Find(WetFloorShaderName);
        if (wetShader != null)
        {
            s_PuddleMaterial = new Material(wetShader);
            s_PuddleMaterial.SetFloat("_ReflectionStrength", 0.32f);
            s_PuddleMaterial.SetFloat("_RippleStrength", 0.03f);
            s_PuddleMaterial.SetFloat("_RippleScale", 10f);
            s_PuddleMaterial.SetFloat("_RippleSpeed", 0.55f);
            s_PuddleMaterial.SetFloat("_FresnelPower", 2.4f);
            s_PuddleMaterial.SetFloat("_SpecularStrength", 0.75f);
            s_PuddleMaterial.SetColor("_WaterColor", new Color(0.08f, 0.12f, 0.16f, 0.52f));
            return s_PuddleMaterial;
        }

        Shader fallbackShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (fallbackShader == null)
        {
            fallbackShader = Shader.Find("Particles/Standard Unlit");
        }

        s_PuddleMaterial = new Material(fallbackShader);
        s_PuddleMaterial.color = new Color(0.09f, 0.12f, 0.16f, 0.58f);
        return s_PuddleMaterial;
    }

    private void RemovePlayer(PlayerMover mover)
    {
        if (mover == null || !ActivePlayers.TryGetValue(mover, out HashSet<PuddleZone> zones))
        {
            return;
        }

        zones.Remove(this);
        if (zones.Count > 0)
        {
            mover.SetSpeedMultiplier(SlowMultiplier);
            return;
        }

        mover.ResetSpeedMultiplier();
        ActivePlayers.Remove(mover);
    }
}
