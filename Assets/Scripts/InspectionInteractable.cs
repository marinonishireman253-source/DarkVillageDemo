using UnityEngine;

[DisallowMultipleComponent]
public sealed class InspectionInteractable : InteractableBase
{
    [SerializeField] private string speakerName = "伊尔萨恩";
    [SerializeField] [TextArea(2, 4)] private string[] firstInspectLines =
    {
        "这里留下了不属于普通火灾的焦痕。"
    };
    [SerializeField] [TextArea(2, 4)] private string[] repeatInspectLines;
    [SerializeField] private BoxCollider interactionTrigger;

    private bool _hasInspected;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = "旧物";
        }

        if (string.IsNullOrWhiteSpace(promptText) || promptText == "Interact")
        {
            promptText = "查看";
        }

        EnsureTrigger();
    }

    public void Configure(
        string newDisplayName,
        string newPromptText,
        string newSpeakerName,
        string[] inspectLines,
        string[] revisitLines,
        Vector3 triggerCenter,
        Vector3 triggerSize)
    {
        ConfigurePresentation(newDisplayName, newPromptText);

        if (!string.IsNullOrWhiteSpace(newSpeakerName))
        {
            speakerName = newSpeakerName.Trim();
        }

        if (inspectLines != null && inspectLines.Length > 0)
        {
            firstInspectLines = inspectLines;
        }

        repeatInspectLines = revisitLines;

        EnsureTrigger();
        interactionTrigger.center = triggerCenter;
        interactionTrigger.size = triggerSize;
    }

    public override void Interact(PlayerMover player)
    {
        if (SimpleDialogueUI.IsOpen)
        {
            return;
        }

        string[] lines = _hasInspected && repeatInspectLines != null && repeatInspectLines.Length > 0
            ? repeatInspectLines
            : firstInspectLines;

        if (lines == null || lines.Length == 0)
        {
            return;
        }

        _hasInspected = true;
        SimpleDialogueUI.Instance?.Show(speakerName, lines);
    }

    private void EnsureTrigger()
    {
        if (interactionTrigger == null)
        {
            interactionTrigger = GetComponent<BoxCollider>();
        }

        if (interactionTrigger == null)
        {
            interactionTrigger = gameObject.AddComponent<BoxCollider>();
        }

        interactionTrigger.isTrigger = true;

        if (interactionTrigger.size.sqrMagnitude <= 0.01f)
        {
            interactionTrigger.center = new Vector3(0f, 1f, 0f);
            interactionTrigger.size = new Vector3(1.4f, 2f, 1.2f);
        }
    }
}
