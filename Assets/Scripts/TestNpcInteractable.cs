using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TestNpcInteractable : InteractableBase
{
    [System.Serializable]
    private struct DialogueLine
    {
        [TextArea(2, 4)]
        public string text;
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueLine[] dialogueLines =
    {
        new DialogueLine
        {
            text = "夜色压下来以后，村口就没有那么好认了。"
        },
        new DialogueLine
        {
            text = "要是你准备继续往前走，记得沿着灯火最弱的那条路。"
        }
    };
    public DialogueData DialogueData => dialogueData;
    public override string DisplayName => dialogueData != null && !string.IsNullOrWhiteSpace(dialogueData.SpeakerName)
        ? dialogueData.SpeakerName
        : base.DisplayName;

    private void Reset()
    {
        ApplyPromptDefaults();
        EnsureCollider();
    }

    private void Awake()
    {
        ApplyPromptDefaults();
        EnsureCollider();
    }

    public void ConfigureFallbackDialogue(params string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        dialogueData = null;
        dialogueLines = new DialogueLine[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            dialogueLines[i] = new DialogueLine
            {
                text = lines[i]
            };
        }

        ApplyPromptDefaults();
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

        if (dialogueData != null)
        {
            SimpleDialogueUI.Instance.Show(dialogueData.SpeakerName, dialogueData.Lines);
            return;
        }

        SimpleDialogueUI.Instance.Show(DisplayName, EnumerateFallbackDialogueLines());
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
        if (dialogueData != null)
        {
            promptText = dialogueData.PromptText;

            if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
            {
                displayName = dialogueData.SpeakerName;
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(promptText) || promptText == "Interact")
        {
            promptText = "交谈";
        }

        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = "守夜人";
        }
    }

    private IEnumerable<string> EnumerateFallbackDialogueLines()
    {
        bool hasLine = false;

        foreach (DialogueLine line in dialogueLines)
        {
            if (string.IsNullOrWhiteSpace(line.text))
            {
                continue;
            }

            hasLine = true;
            yield return line.text.Trim();
        }

        if (!hasLine)
        {
            yield return "...";
        }
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider>() != null)
        {
            return;
        }

        CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        capsuleCollider.center = new Vector3(0f, 1f, 0f);
        capsuleCollider.height = 2f;
        capsuleCollider.radius = 0.35f;
    }

}
