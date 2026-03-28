using UnityEngine;

/// <summary>
/// 挂在 NPC 上，通过 DialogueNode 数据驱动完整对话流程（含选项分支）。
/// 替代 TestNpcInteractable 的硬编码对话方式。
/// </summary>
[DisallowMultipleComponent]
public class DialogueInteractable : InteractableBase
{
    [Header("对话数据")]
    [SerializeField] private DialogueNode startNode;

    [Header("立绘匹配（可选）")]
    [SerializeField] private string characterId;

    [Header("焦点反馈")]
    [SerializeField] private Renderer highlightRenderer;
    [SerializeField] private Color idleColor = new Color(0.78f, 0.8f, 0.82f);
    [SerializeField] private Color focusColor = new Color(0.97f, 0.83f, 0.58f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public override string DisplayName
    {
        get
        {
            if (startNode != null && !string.IsNullOrWhiteSpace(startNode.SpeakerName))
            {
                return startNode.SpeakerName;
            }
            return base.DisplayName;
        }
    }

    private MaterialPropertyBlock _propertyBlock;
    private bool _supportsBaseColor;
    private bool _supportsColor;

    private void Reset()
    {
        if (highlightRenderer == null)
        {
            highlightRenderer = GetComponentInChildren<Renderer>();
        }
        EnsureCollider();
    }

    private void Awake()
    {
        ApplyPromptDefaults();
        if (highlightRenderer == null)
        {
            highlightRenderer = GetComponentInChildren<Renderer>();
        }
        EnsureCollider();
        CacheColorSupport();
        ApplyColor(idleColor);
    }

    public override void Interact(PlayerMover player)
    {
        if (SimpleDialogueUI.IsOpen || DialogueRunner.IsActive)
        {
            return;
        }

        if (startNode == null)
        {
            Debug.LogWarning($"[DialogueInteractable] {gameObject.name}: startNode 未设置");
            return;
        }

        // 通知任务系统
        if (TryGetComponent(out QuestObjectiveTarget objectiveTarget))
        {
            objectiveTarget.NotifyInteracted();
        }

        DialogueRunner.Instance?.StartDialogue(startNode);
    }

    public override void OnFocusGained(PlayerMover player)
    {
        ApplyColor(focusColor);
    }

    public override void OnFocusLost(PlayerMover player)
    {
        ApplyColor(idleColor);
    }

    private void ApplyPromptDefaults()
    {
        if (startNode != null && !string.IsNullOrWhiteSpace(startNode.SpeakerName))
        {
            displayName = startNode.SpeakerName;
        }
        if (string.IsNullOrWhiteSpace(promptText) || promptText == "Interact")
        {
            promptText = "交谈";
        }
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider>() != null)
        {
            return;
        }
        CapsuleCollider cc = gameObject.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.height = 2f;
        cc.radius = 0.35f;
    }

    private void CacheColorSupport()
    {
        if (highlightRenderer == null || highlightRenderer.sharedMaterial == null)
        {
            return;
        }
        _propertyBlock = new MaterialPropertyBlock();
        _supportsBaseColor = highlightRenderer.sharedMaterial.HasProperty(BaseColorId);
        _supportsColor = highlightRenderer.sharedMaterial.HasProperty(ColorId);
    }

    private void ApplyColor(Color color)
    {
        if (highlightRenderer == null || (!_supportsBaseColor && !_supportsColor))
        {
            return;
        }
        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }
        _propertyBlock.Clear();
        if (_supportsBaseColor) _propertyBlock.SetColor(BaseColorId, color);
        if (_supportsColor) _propertyBlock.SetColor(ColorId, color);
        highlightRenderer.SetPropertyBlock(_propertyBlock);
    }
}
