using UnityEngine;

public sealed class AshParlorExitInteractable : InteractableBase
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private Light accentLight;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color lockedColor = new Color(0.23f, 0.22f, 0.24f, 1f);
    [SerializeField] private Color unlockedColor = new Color(0.82f, 0.65f, 0.3f, 1f);

    private FloorRunController _controller;
    private bool _isUnlocked;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = "塔梯";
        }

        SynchronizeUnlockedStateFromHub();
        RefreshPrompt();
        ApplyState();
    }

    public void Configure(FloorRunController controller, Light exitLight, Renderer[] renderers)
    {
        _controller = controller;
        accentLight = exitLight;
        targetRenderers = renderers;
        ApplyState();
    }

    public override void Interact(PlayerMover player)
    {
        SynchronizeUnlockedStateFromHub();
        _controller?.TryUseExit(player);
    }

    public void SetUnlocked(bool unlocked)
    {
        _isUnlocked = unlocked;
        RefreshPrompt();
        ApplyState();
    }

    private void SynchronizeUnlockedStateFromHub()
    {
        if (GameStateHub.Instance == null)
        {
            return;
        }

        string exitUnlockedFlagId = _controller != null ? _controller.ExitUnlockedFlagId : string.Empty;
        if (string.IsNullOrWhiteSpace(exitUnlockedFlagId))
        {
            return;
        }

        string flagValue = GameStateHub.Instance.GetChapterFlag(exitUnlockedFlagId);
        if (string.IsNullOrEmpty(flagValue))
        {
            return;
        }

        _isUnlocked = string.Equals(flagValue, "true", System.StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshPrompt()
    {
        promptText = _isUnlocked ? "攀上塔梯" : "查看封印";
    }

    private void ApplyState()
    {
        Color targetColor = _isUnlocked ? unlockedColor : lockedColor;
        Color emission = _isUnlocked ? unlockedColor * 0.8f : Color.black;

        if (targetRenderers != null)
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, targetColor);
                block.SetColor(ColorId, targetColor);
                block.SetColor(EmissionColorId, emission);
                renderer.SetPropertyBlock(block);
            }
        }

        if (accentLight != null)
        {
            accentLight.color = _isUnlocked ? unlockedColor : new Color(0.2f, 0.16f, 0.14f, 1f);
            accentLight.intensity = _isUnlocked ? 1.6f : 0.18f;
            accentLight.range = _isUnlocked ? 6f : 2.4f;
            accentLight.enabled = true;
        }
    }
}
