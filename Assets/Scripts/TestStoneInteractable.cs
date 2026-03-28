using UnityEngine;

public class TestStoneInteractable : InteractableBase
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] [TextArea(2, 4)] private string fallbackDialogueText = "石碑表面刻着一行浅白色的字：\n\n‘愿仍记誓者，先于黑夜到达。’";
    [SerializeField] private Color idleColor = new Color(0.55f, 0.55f, 0.62f);
    [SerializeField] private Color focusColor = new Color(0.9f, 0.82f, 0.45f);

    public DialogueData DialogueData => dialogueData;

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        ApplyDefaults();
        _renderer = GetComponentInChildren<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        ApplyColor(idleColor);
    }

    public override void Interact(PlayerMover player)
    {
        if (SimpleDialogueUI.Instance == null || SimpleDialogueUI.IsOpen)
        {
            return;
        }

        if (TryGetComponent(out QuestObjectiveTarget objectiveTarget))
        {
            objectiveTarget.NotifyInteracted();
        }

        Debug.Log("[TestStoneInteractable] Interact triggered");

        if (dialogueData != null)
        {
            SimpleDialogueUI.Instance.Show(dialogueData.SpeakerName, dialogueData.Lines);
            return;
        }

        SimpleDialogueUI.Instance.Show(DisplayName, fallbackDialogueText);
    }

    public override void OnFocusGained(PlayerMover player)
    {
        ApplyColor(focusColor);
    }

    public override void OnFocusLost(PlayerMover player)
    {
        ApplyColor(idleColor);
    }

    private void ApplyDefaults()
    {
        if (dialogueData != null)
        {
            if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
            {
                displayName = dialogueData.SpeakerName;
            }

            promptText = dialogueData.PromptText;
            return;
        }

        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = "石碑";
        }

        if (string.IsNullOrWhiteSpace(promptText) || promptText == "Interact")
        {
            promptText = "查看";
        }
    }

    private void ApplyColor(Color color)
    {
        if (_renderer == null)
        {
            return;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        _propertyBlock.Clear();
        _propertyBlock.SetColor("_BaseColor", color);
        _propertyBlock.SetColor("_Color", color);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
}
