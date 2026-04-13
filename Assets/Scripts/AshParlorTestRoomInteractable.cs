using UnityEngine;

public sealed class AshParlorTestRoomInteractable : InteractableBase
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private AshParlorRunController.FloorSummaryTestPreset preset;
    [SerializeField] private Light accentLight;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color riskColor = new Color(0.82f, 0.38f, 0.2f, 1f);
    [SerializeField] private Color safeColor = new Color(0.56f, 0.66f, 0.74f, 1f);

    private AshParlorRunController _controller;

    private void Awake()
    {
        ApplyDefaults();
        ApplyState();
    }

    public void Configure(
        AshParlorRunController controller,
        AshParlorRunController.FloorSummaryTestPreset testPreset,
        Light lightSource,
        Renderer[] renderers,
        string newDisplayName,
        string newPromptText)
    {
        _controller = controller;
        preset = testPreset;
        accentLight = lightSource;
        targetRenderers = renderers;

        if (!string.IsNullOrWhiteSpace(newDisplayName))
        {
            displayName = newDisplayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(newPromptText))
        {
            promptText = newPromptText.Trim();
        }

        ApplyDefaults();
        ApplyState();
    }

    public override void Interact(PlayerMover player)
    {
        _controller?.PrepareFloorSummaryTest(preset, player);
    }

    private void ApplyDefaults()
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = preset == AshParlorRunController.FloorSummaryTestPreset.RiskSummary
                ? "风险结算测试"
                : "安全结算测试";
        }

        if (string.IsNullOrWhiteSpace(promptText) || promptText == "Interact")
        {
            promptText = "快速准备";
        }
    }

    private void ApplyState()
    {
        Color targetColor = preset == AshParlorRunController.FloorSummaryTestPreset.RiskSummary
            ? riskColor
            : safeColor;
        Color emission = targetColor * 0.78f;

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
            accentLight.color = targetColor;
            accentLight.intensity = 1.2f;
            accentLight.range = 4.4f;
            accentLight.enabled = true;
        }
    }
}
