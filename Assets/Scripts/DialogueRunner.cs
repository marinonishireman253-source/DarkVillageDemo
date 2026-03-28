using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DialogueRunner : MonoBehaviour
{
    public static DialogueRunner Instance { get; private set; }
    public static bool IsActive => Instance != null && Instance._isActive;
    public bool IsRunning => _isActive;

    private bool _isActive;
    private DialogueNode _currentNode;
    private readonly List<DialogueNode> _nodeHistory = new List<DialogueNode>();

    public event System.Action<DialogueNode> OnNodeStarted;
    public event System.Action OnDialogueEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartDialogue(DialogueNode startNode)
    {
        if (startNode == null)
        {
            Debug.LogWarning("[DialogueRunner] startNode is null");
            return;
        }

        _isActive = true;
        _nodeHistory.Clear();
        ShowNode(startNode);
    }

    public void SelectChoice(int choiceIndex)
    {
        if (_currentNode == null || !_currentNode.HasChoices)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= _currentNode.Choices.Count)
        {
            Debug.LogWarning($"[DialogueRunner] Invalid choice index: {choiceIndex}");
            return;
        }

        DialogueChoice choice = _currentNode.Choices[choiceIndex];
        if (choice.NextNode != null)
        {
            ShowNode(choice.NextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    public void Advance()
    {
        if (_currentNode == null)
        {
            return;
        }

        // 有选项时不允许自动推进，必须选择
        if (_currentNode.HasChoices)
        {
            return;
        }

        if (_currentNode.NextNode != null)
        {
            ShowNode(_currentNode.NextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    private void ShowNode(DialogueNode node)
    {
        _currentNode = node;
        _nodeHistory.Add(node);

        // 触发进入事件
        foreach (DialogueEvent evt in node.OnEnterEvents)
        {
            DialogueEventSystem.Raise(evt);
        }

        // 完成任务目标
        if (!string.IsNullOrWhiteSpace(node.CompleteObjectiveId))
        {
            QuestTracker tracker = QuestTracker.Instance;
            if (tracker != null)
            {
                tracker.CompleteObjective(node.CompleteObjectiveId);
            }
        }

        OnNodeStarted?.Invoke(node);
    }

    private void EndDialogue()
    {
        _isActive = false;
        _currentNode = null;
        _nodeHistory.Clear();
        OnDialogueEnded?.Invoke();
    }
}
