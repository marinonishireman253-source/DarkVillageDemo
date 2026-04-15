using UnityEngine;

public sealed class AshParlorChoiceInteractable : InteractableBase
{
    public enum ChoiceKind
    {
        Risky,
        Safe
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private ChoiceKind choiceKind;
    [SerializeField] private Light accentLight;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color riskyColor = new Color(0.82f, 0.38f, 0.2f, 1f);
    [SerializeField] private Color safeColor = new Color(0.56f, 0.66f, 0.74f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.22f, 0.22f, 0.22f, 1f);

    public ChoiceKind Kind => choiceKind;
    public bool IsResolved { get; private set; }

    private FloorRunController _controller;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = choiceKind == ChoiceKind.Risky ? "躁动余烬" : "封灰祷台";
        }

        promptText = choiceKind == ChoiceKind.Risky ? "夺取余烬" : "压住灰封";
        ApplyState(false);
    }

    public void Configure(
        FloorRunController controller,
        ChoiceKind kind,
        Light lightSource,
        Renderer[] renderers,
        string newDisplayName,
        string newPromptText)
    {
        _controller = controller;
        choiceKind = kind;
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

        ApplyState(false);
    }

    public override void Interact(PlayerMover player)
    {
        if (IsResolved)
        {
            SimpleDialogueUI.Instance?.Show("灰烬客厅", "这个决定已经落下了。");
            return;
        }

        _controller?.TryResolveChoice(choiceKind, player);
    }

    public void SetResolved(bool resolved)
    {
        IsResolved = resolved;
        ApplyState(true);
    }

    public void SetAvailable(bool available)
    {
        IsResolved = !available;
        ApplyState(false);
    }

    private void ApplyState(bool keepPrompt)
    {
        Color targetColor;
        Color emission;

        if (IsResolved)
        {
            targetColor = disabledColor;
            emission = Color.black;
        }
        else
        {
            targetColor = choiceKind == ChoiceKind.Risky ? riskyColor : safeColor;
            emission = targetColor * 0.82f;
        }

        if (IsResolved)
        {
            promptText = "已选择";
        }

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
            accentLight.intensity = IsResolved ? 0.14f : 1.35f;
            accentLight.range = IsResolved ? 2.1f : 5.2f;
            accentLight.enabled = true;
        }
    }
}
