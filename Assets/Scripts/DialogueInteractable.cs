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
    private void Reset()
    {
        EnsureCollider();
    }

    private void Awake()
    {
        ApplyPromptDefaults();
        EnsureCollider();
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
        base.OnFocusGained(player);
    }

    public override void OnFocusLost(PlayerMover player)
    {
        base.OnFocusLost(player);
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

}
