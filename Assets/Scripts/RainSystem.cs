using UnityEngine;

/// <summary>
/// Procedural indoor rain particle system that follows the camera horizontally.
/// Creates a leaking-ceiling rain band with collision-driven impact splashes.
/// </summary>
public sealed class RainSystem : MonoBehaviour
{
    private const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";
    private const float FollowSpeed = 5f;
    private const float RainHeight = 6.0f;
    private const float RoomMidDepth = 4.2f;
    private const float RainWidth = 30.0f;
    private const float RainDepth = 6.0f;

    private ParticleSystem _rainPS;
    private Transform _followTarget;

    private static RainSystem s_Instance;

    public static void Ensure()
    {
        if (s_Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("__RainSystem");
        s_Instance = go.AddComponent<RainSystem>();
        s_Instance.Build();
    }

    public static void Disable()
    {
        if (s_Instance == null)
        {
            RainSystem existing = FindFirstObjectByType<RainSystem>();
            if (existing == null)
            {
                return;
            }

            s_Instance = existing;
        }

        if (s_Instance != null)
        {
            Destroy(s_Instance.gameObject);
            s_Instance = null;
        }
    }

    private void Build()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            _followTarget = mainCam.transform;
        }

        CreateRainParticles();
    }

    private void Update()
    {
        if (_followTarget == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                _followTarget = mainCam.transform;
            }
            return;
        }

        // Follow only the camera X so the rain stays inside the room volume.
        Vector3 targetPos = new Vector3(_followTarget.position.x, 0f, 0f);
        transform.position = Vector3.Lerp(transform.position, targetPos, FollowSpeed * Time.deltaTime);
    }

    private void CreateRainParticles()
    {
        GameObject rainGO = new GameObject("RainDrops");
        rainGO.transform.SetParent(transform, false);
        rainGO.transform.localPosition = new Vector3(0f, RainHeight, RoomMidDepth);
        rainGO.transform.localRotation = Quaternion.Euler(78f, 8f, 0f);

        _rainPS = rainGO.AddComponent<ParticleSystem>();
        var main = _rainPS.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6.0f, 8.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.03f);
        main.startColor = new Color(0.6f, 0.7f, 0.8f, 0.3f);
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;

        var emission = _rainPS.emission;
        emission.rateOverTime = 150f;

        var shape = _rainPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(RainWidth, 0.1f, RainDepth);
        shape.rotation = Vector3.zero;

        var velocityOverLifetime = _rainPS.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-1.2f, -0.4f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var collision = _rainPS.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.bounceMultiplier = 0f;
        collision.lifetimeLossMultiplier = 1f;
        collision.collidesWith = 1; // Default layer only (layer 0)
        collision.sendCollisionMessages = false;
        collision.quality = ParticleSystemCollisionQuality.Low;
        collision.colliderForce = 0f;
        collision.multiplyColliderForceByCollisionAngle = false;
        collision.multiplyColliderForceByParticleSize = false;
        collision.multiplyColliderForceByParticleSpeed = false;

        // Renderer setup (line particles)
        ParticleSystemRenderer renderer = rainGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 4f;
        renderer.velocityScale = 0.15f;

        // Use default particle material
        Shader particleShader = FindParticleShader();
        renderer.material = new Material(particleShader);
        renderer.material.color = new Color(0.6f, 0.7f, 0.8f, 0.3f);
        renderer.sortingOrder = 5;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        CreateRippleSubEmitter(rainGO.transform);
        CreateSplashSubEmitter(rainGO.transform);
    }

    private void CreateRippleSubEmitter(Transform rainRoot)
    {
        if (_rainPS == null || rainRoot == null)
        {
            return;
        }

        GameObject rippleGO = new GameObject("RainDropRipple");
        rippleGO.transform.SetParent(rainRoot, false);
        rippleGO.transform.localPosition = Vector3.zero;
        rippleGO.transform.localRotation = Quaternion.identity;

        ParticleSystem ripplePS = rippleGO.AddComponent<ParticleSystem>();
        ripplePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ripplePS.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.65f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new Color(0.65f, 0.72f, 0.8f, 0.4f);
        main.maxParticles = 128;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = ripplePS.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ripplePS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0f;

        var sizeOverLifetime = ripplePS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, 0.4f),
                new Keyframe(1f, 1f)));

        var colorOverLifetime = ripplePS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient rippleGradient = new Gradient();
        rippleGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.68f, 0.74f, 0.82f), 0f),
                new GradientColorKey(new Color(0.72f, 0.78f, 0.85f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.4f, 0f),
                new GradientAlphaKey(0.18f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(rippleGradient);

        ParticleSystemRenderer renderer = rippleGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(FindParticleShader());
        renderer.material.color = new Color(0.68f, 0.74f, 0.82f, 0.4f);
        renderer.sortingOrder = 5;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var subEmitters = _rainPS.subEmitters;
        subEmitters.enabled = true;
        subEmitters.AddSubEmitter(ripplePS, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing);
    }

    private void CreateSplashSubEmitter(Transform rainRoot)
    {
        if (_rainPS == null || rainRoot == null)
        {
            return;
        }

        GameObject splashGO = new GameObject("RainDropSplash");
        splashGO.transform.SetParent(rainRoot, false);
        splashGO.transform.localPosition = Vector3.zero;
        splashGO.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem splashPS = splashGO.AddComponent<ParticleSystem>();
        splashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = splashPS.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
        main.startColor = new Color(0.6f, 0.7f, 0.8f, 0.35f);
        main.maxParticles = 128;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.8f;

        var emission = splashPS.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1, (short)2) });

        var shape = splashPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.02f;
        shape.radiusThickness = 1f;

        var colorOverLifetime = splashPS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient splashGradient = new Gradient();
        splashGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.64f, 0.74f, 0.82f), 0f),
                new GradientColorKey(new Color(0.54f, 0.64f, 0.72f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.35f, 0f),
                new GradientAlphaKey(0.16f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(splashGradient);

        ParticleSystemRenderer renderer = splashGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(FindParticleShader());
        renderer.material.color = new Color(0.6f, 0.7f, 0.8f, 0.35f);
        renderer.sortingOrder = 5;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var subEmitters = _rainPS.subEmitters;
        subEmitters.enabled = true;
        subEmitters.AddSubEmitter(splashPS, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing);
    }

    private static Shader FindParticleShader()
    {
        Shader particleShader = Shader.Find(ParticleShaderName);
        if (particleShader == null)
        {
            particleShader = Shader.Find("Particles/Standard Unlit");
        }

        return particleShader;
    }
}
