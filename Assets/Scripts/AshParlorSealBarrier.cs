using UnityEngine;

public sealed class AshParlorSealBarrier : MonoBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Collider[] targetColliders;
    [SerializeField] private Light accentLight;

    public bool IsLocked { get; private set; } = true;

    public void Configure(Renderer[] renderers, Collider[] colliders, Light lightSource)
    {
        targetRenderers = renderers;
        targetColliders = colliders;
        accentLight = lightSource;
        ApplyState();
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        ApplyState();
    }

    private void ApplyState()
    {
        if (targetRenderers != null)
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = IsLocked;
                }
            }
        }

        if (targetColliders != null)
        {
            foreach (Collider colliderComponent in targetColliders)
            {
                if (colliderComponent != null)
                {
                    colliderComponent.enabled = IsLocked;
                }
            }
        }

        if (accentLight != null)
        {
            accentLight.enabled = true;
            accentLight.intensity = IsLocked ? 1.15f : 0.16f;
            accentLight.range = IsLocked ? 3.4f : 1.8f;
            accentLight.color = IsLocked
                ? new Color(0.82f, 0.44f, 0.22f, 1f)
                : new Color(0.22f, 0.18f, 0.16f, 1f);
        }
    }
}
