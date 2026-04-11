using System.Collections.Generic;
using UnityEngine;

public class PortraitController : MonoBehaviour
{
    [System.Serializable]
    public class PortraitEntry
    {
        public string characterId;
        public string characterName;
        public Texture2D portraitTexture;
        public Color bgColor = new Color(0.12f, 0.14f, 0.17f, 0.9f);
    }

    [SerializeField] private List<PortraitEntry> portraits = new List<PortraitEntry>();

    private bool _isVisible;
    private PortraitEntry _currentPortrait;

    private readonly Dictionary<string, PortraitEntry> _portraitMap = new Dictionary<string, PortraitEntry>();

    private void Awake()
    {
        RebuildMap();
    }

    private void RebuildMap()
    {
        _portraitMap.Clear();
        foreach (PortraitEntry entry in portraits)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.characterId))
            {
                _portraitMap[entry.characterId] = entry;
            }
        }
    }

    private void OnEnable()
    {
        if (DialogueRunner.Instance != null)
        {
            DialogueRunner.Instance.OnNodeStarted += HandleNodeStarted;
            DialogueRunner.Instance.OnDialogueEnded += HandleDialogueEnded;
        }
    }

    private void OnDisable()
    {
        if (DialogueRunner.Instance != null)
        {
            DialogueRunner.Instance.OnNodeStarted -= HandleNodeStarted;
            DialogueRunner.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }

        _isVisible = false;
        _currentPortrait = null;
        SyncCanvasView();
    }

    private void HandleNodeStarted(DialogueNode node)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.SpeakerName))
        {
            _isVisible = false;
            _currentPortrait = null;
            SyncCanvasView();
            return;
        }

        // 尝试用 speakerName 匹配 characterId 或 characterName
        if (_portraitMap.TryGetValue(node.SpeakerName, out PortraitEntry entry))
        {
            _currentPortrait = entry;
            _isVisible = true;
            SyncCanvasView();
        }
        else
        {
            // 尝试按 characterName 模糊匹配
            foreach (PortraitEntry p in portraits)
            {
                if (p != null && p.characterName == node.SpeakerName)
                {
                    _currentPortrait = p;
                    _isVisible = true;
                    SyncCanvasView();
                    return;
                }
            }
            _isVisible = false;
            _currentPortrait = null;
            SyncCanvasView();
        }
    }

    private void HandleDialogueEnded()
    {
        _isVisible = false;
        _currentPortrait = null;
        SyncCanvasView();
    }

    private void SyncCanvasView()
    {
        if (!UiBootstrap.TryGetDialogueView(out DialogueCanvasView dialogueView))
        {
            return;
        }

        if (!_isVisible || _currentPortrait == null)
        {
            dialogueView.HidePortrait();
            return;
        }

        string displayName = !string.IsNullOrWhiteSpace(_currentPortrait.characterName)
            ? _currentPortrait.characterName
            : _currentPortrait.characterId;

        dialogueView.ShowPortrait(_currentPortrait.portraitTexture, displayName, _currentPortrait.bgColor);
    }
}
