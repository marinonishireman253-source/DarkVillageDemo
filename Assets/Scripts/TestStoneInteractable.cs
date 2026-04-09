using UnityEngine;

public class TestStoneInteractable : InteractableBase
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] [TextArea(2, 4)] private string fallbackDialogueText = "石碑表面刻着一行浅白色的字：\n\n‘愿仍记誓者，先于黑夜到达。’";

    public DialogueData DialogueData => dialogueData;

    private void Awake()
    {
        ApplyDefaults();
    }

    public void ConfigureFallbackDialogue(string speakerName, string prompt, string text)
    {
        dialogueData = null;

        if (!string.IsNullOrWhiteSpace(speakerName))
        {
            displayName = speakerName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(prompt))
        {
            promptText = prompt.Trim();
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            fallbackDialogueText = text.Trim();
        }
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
        base.OnFocusGained(player);
    }

    public override void OnFocusLost(PlayerMover player)
    {
        base.OnFocusLost(player);
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

}
