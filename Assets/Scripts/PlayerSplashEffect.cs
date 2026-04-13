using UnityEngine;

[RequireComponent(typeof(PlayerMover))]
public sealed class PlayerSplashEffect : MonoBehaviour
{
    private const float FootY = 0.26f;
    private const float MinMoveSpeed = 0.08f;
    private const float WalkStepDistance = 0.8f;
    private const float SprintStepDistance = 0.55f;

    private PlayerMover _playerMover;
    private ParticleSystem _splashPS;
    private ParticleSystem _ripplePS;
    private float _lastXPosition;
    private float _distanceSinceStep;
    private bool _hasLastXSample;

    private void Awake()
    {
        _playerMover = GetComponent<PlayerMover>();
        Build();
        _lastXPosition = transform.position.x;
        _hasLastXSample = true;
    }

    private void LateUpdate()
    {
        if (_playerMover == null || _splashPS == null || _ripplePS == null)
        {
            return;
        }

        Vector3 emitterPosition = transform.position;
        emitterPosition.y = FootY;
        emitterPosition.z = transform.position.z;
        _splashPS.transform.position = emitterPosition;
        _ripplePS.transform.position = emitterPosition;

        Vector3 planarVelocity = _playerMover.Velocity + _playerMover.ExternalVelocity;
        planarVelocity.y = 0f;
        float speed = planarVelocity.magnitude;
        float currentX = transform.position.x;

        if (!_hasLastXSample)
        {
            _lastXPosition = currentX;
            _hasLastXSample = true;
            return;
        }

        float deltaX = Mathf.Abs(currentX - _lastXPosition);
        _lastXPosition = currentX;

        if (speed <= MinMoveSpeed)
        {
            return;
        }

        _distanceSinceStep += deltaX;
        float stepDistance = _playerMover.IsSprintActive ? SprintStepDistance : WalkStepDistance;

        while (_distanceSinceStep >= stepDistance)
        {
            EmitStepBurst();
            _distanceSinceStep -= stepDistance;
        }
    }

    private void Build()
    {
        if (_splashPS != null && _ripplePS != null)
        {
            return;
        }

        GameObject splashGO = new GameObject("PlayerSplashParticles");
        splashGO.transform.SetParent(transform, false);
        splashGO.transform.localPosition = new Vector3(0f, FootY, 0f);
        splashGO.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        _splashPS = splashGO.AddComponent<ParticleSystem>();
        _splashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _splashPS.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.45f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
        main.startColor = new Color(0.58f, 0.66f, 0.74f, 0.38f);
        main.maxParticles = 128;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.5f;

        var emission = _splashPS.emission;
        emission.rateOverTime = 0f;

        var shape = _splashPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.08f;
        shape.radiusThickness = 0.8f;

        var colorOverLifetime = _splashPS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.62f, 0.7f, 0.77f), 0f),
                new GradientColorKey(new Color(0.48f, 0.56f, 0.62f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.34f, 0f),
                new GradientAlphaKey(0.22f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = _splashPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.8f),
                new Keyframe(0.4f, 1f),
                new Keyframe(1f, 0.28f)));

        ParticleSystemRenderer renderer = splashGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader shader = FindParticleShader();
        renderer.material = new Material(shader);
        renderer.material.color = new Color(0.58f, 0.66f, 0.74f, 0.38f);
        renderer.sortingOrder = 5;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        GameObject rippleGO = new GameObject("PlayerStepRipple");
        rippleGO.transform.SetParent(transform, false);
        rippleGO.transform.localPosition = new Vector3(0f, FootY, 0f);
        rippleGO.transform.localRotation = Quaternion.identity;

        _ripplePS = rippleGO.AddComponent<ParticleSystem>();
        _ripplePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var rippleMain = _ripplePS.main;
        rippleMain.loop = false;
        rippleMain.playOnAwake = false;
        rippleMain.duration = 0.4f;
        rippleMain.startLifetime = 0.4f;
        rippleMain.startSpeed = 0f;
        rippleMain.startSize = 0.12f;
        rippleMain.startColor = new Color(0.66f, 0.74f, 0.82f, 0.3f);
        rippleMain.maxParticles = 32;
        rippleMain.simulationSpace = ParticleSystemSimulationSpace.World;
        rippleMain.gravityModifier = 0f;

        var rippleEmission = _ripplePS.emission;
        rippleEmission.rateOverTime = 0f;

        var rippleShape = _ripplePS.shape;
        rippleShape.enabled = true;
        rippleShape.shapeType = ParticleSystemShapeType.Circle;
        rippleShape.radius = 0.01f;

        var rippleSizeOverLifetime = _ripplePS.sizeOverLifetime;
        rippleSizeOverLifetime.enabled = true;
        rippleSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.2f, 0.45f),
                new Keyframe(1f, 1f)));

        var rippleColorOverLifetime = _ripplePS.colorOverLifetime;
        rippleColorOverLifetime.enabled = true;
        Gradient rippleGradient = new Gradient();
        rippleGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.68f, 0.76f, 0.84f), 0f),
                new GradientColorKey(new Color(0.58f, 0.66f, 0.74f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.28f, 0f),
                new GradientAlphaKey(0.18f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        rippleColorOverLifetime.color = new ParticleSystem.MinMaxGradient(rippleGradient);

        ParticleSystemRenderer rippleRenderer = rippleGO.GetComponent<ParticleSystemRenderer>();
        rippleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        rippleRenderer.material = new Material(shader);
        rippleRenderer.material.color = new Color(0.66f, 0.74f, 0.82f, 0.3f);
        rippleRenderer.sortingOrder = 5;
        rippleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rippleRenderer.receiveShadows = false;
    }

    private void EmitStepBurst()
    {
        if (_splashPS == null || _ripplePS == null)
        {
            return;
        }

        _splashPS.Emit(Random.Range(3, 6));
        _ripplePS.Emit(1);
    }

    private static Shader FindParticleShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        return shader;
    }
}
