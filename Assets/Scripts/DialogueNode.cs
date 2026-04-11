using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueNode", menuName = "Ersarn/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [Header("对话内容")]
    [SerializeField] private string speakerName;
    [SerializeField] [TextArea(2, 6)] private List<string> lines = new List<string>();

    [Header("选项分支")]
    [SerializeField] private List<DialogueChoice> choices = new List<DialogueChoice>();

    [Header("剧情事件")]
    [SerializeField] private List<DialogueEvent> onEnterEvents = new List<DialogueEvent>();
    [SerializeField] private string completeObjectiveId;

    [Header("下一段")]
    [SerializeField] private DialogueNode nextNode;

    public string SpeakerName => speakerName;
    public IReadOnlyList<string> Lines => lines;
    public IReadOnlyList<DialogueChoice> Choices => choices;
    public IReadOnlyList<DialogueEvent> OnEnterEvents => onEnterEvents;
    public string CompleteObjectiveId => completeObjectiveId;
    public DialogueNode NextNode => nextNode;

    public bool HasChoices => choices != null && choices.Count > 0;
}

[Serializable]
public class DialogueChoice
{
    [SerializeField] private string choiceText;
    [SerializeField] private DialogueNode nextNode;
    [SerializeField] private string tag;

    public string ChoiceText => choiceText;
    public DialogueNode NextNode => nextNode;
    public string Tag => tag;

    public static DialogueChoice CreatePreview(string text, string previewTag = "")
    {
        return new DialogueChoice
        {
            choiceText = text,
            tag = previewTag
        };
    }
}

[Serializable]
public class DialogueEvent
{
    public enum EventType
    {
        TriggerQuest,
        SetFlag,
        PlaySound,
        SpawnObject,
        Custom
    }

    [SerializeField] private EventType eventType;
    [SerializeField] private string eventId;
    [SerializeField] private string eventParameter;

    public EventType Type => eventType;
    public string Id => eventId;
    public string Parameter => eventParameter;
}
