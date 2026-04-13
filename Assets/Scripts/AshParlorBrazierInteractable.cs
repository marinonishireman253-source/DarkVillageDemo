using UnityEngine;

public sealed class AshParlorBrazierInteractable : InteractableBase
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private int brazierIndex = 1;
    [SerializeField] private Light flameLight;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color dormantColor = new Color(0.26f, 0.22f, 0.2f, 1f);
    [SerializeField] private Color litColor = new Color(0.93f, 0.63f, 0.26f, 1f);
    [SerializeField] private Color litEmission = new Color(1f, 0.54f, 0.18f, 1f);
    [SerializeField] private float dormantLightIntensity = 0.1f;
    [SerializeField] private float litLightIntensity = 2.1f;

    public int BrazierIndex => brazierIndex;
    public bool IsLit { get; private set; }

    private AshParlorRunController _controller;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = "封印烛台";
        }

        promptText = "点燃";
        ApplyState();
    }

    public void Configure(AshParlorRunController controller, int index, Light brazierLight, Renderer[] renderers)
    {
        _controller = controller;
        brazierIndex = Mathf.Max(1, index);
        flameLight = brazierLight;
        targetRenderers = renderers;
        ApplyState();
    }

    public override void Interact(PlayerMover player)
    {
        if (IsLit)
        {
            SimpleDialogueUI.Instance?.Show("灰烬客厅", "余烬已经稳住了。");
            return;
        }

        _controller?.TryLightBrazier(this, player);
    }

    public void SetLit(bool lit)
    {
        IsLit = lit;
        promptText = lit ? "余烬已亮" : "点燃";
        ApplyState();
    }

    private void ApplyState()
    {
        Color targetColor = IsLit ? litColor : dormantColor;
        Color emission = IsLit ? litEmission : Color.black;

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

        if (flameLight != null)
        {
            flameLight.color = IsLit ? litEmission : dormantColor;
            flameLight.intensity = IsLit ? litLightIntensity : dormantLightIntensity;
            flameLight.range = IsLit ? 6.2f : 2.2f;
            flameLight.enabled = true;
        }
    }
}
