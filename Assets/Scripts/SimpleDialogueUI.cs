using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleDialogueUI : MonoBehaviour
{
    public static SimpleDialogueUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance._isOpen;
    public static bool AllLinesShown => Instance != null && Instance._currentLineIndex >= Instance._lines.Count - 1;

    public event System.Action OnAllLinesCompleted;

    private bool _isOpen;
    private readonly List<string> _lines = new List<string>();
    private string _speakerName = string.Empty;
    private int _currentLineIndex;
    private int _openedFrame = -1;

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
            if (UiBootstrap.TryGetDialogueView(out DialogueCanvasView dialogueView))
            {
                dialogueView.HideDialogue();
            }

            Instance = null;
        }
    }

    private void Update()
    {
        if (!_isOpen)
        {
            return;
        }

        if (Time.frameCount <= _openedFrame)
        {
            return;
        }

        bool advancePressed = false;
        bool cancelPressed = false;

        if (Keyboard.current != null)
        {
            cancelPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            advancePressed = Keyboard.current.eKey.wasPressedThisFrame
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        if (cancelPressed)
        {
            Hide();
            return;
        }

        if (advancePressed)
        {
            AdvanceOrHide();
        }
    }

    public void Show(string text)
    {
        Show(string.Empty, new[] { text });
    }

    public void Show(string speakerName, params string[] lines)
    {
        Show(speakerName, (IEnumerable<string>)lines);
    }

    public void Show(string speakerName, IEnumerable<string> lines)
    {
        _speakerName = speakerName == null ? string.Empty : speakerName.Trim();
        _lines.Clear();

        if (lines != null)
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                _lines.Add(line.Trim());
            }
        }

        if (_lines.Count == 0)
        {
            _lines.Add("...");
        }

        _currentLineIndex = 0;
        _isOpen = true;
        _openedFrame = Time.frameCount;

        DialogueVoicePlayer voicePlayer = DialogueVoicePlayer.Instance;
        if (voicePlayer != null)
        {
            voicePlayer.OnDialogueOpened();
        }

        SyncCanvasView();
    }

    public void Hide()
    {
        _isOpen = false;
        _speakerName = string.Empty;
        _lines.Clear();
        _currentLineIndex = 0;
        _openedFrame = -1;

        if (UiBootstrap.TryGetDialogueView(out DialogueCanvasView dialogueView))
        {
            dialogueView.HideDialogue();
        }

        DialogueVoicePlayer voicePlayer = DialogueVoicePlayer.Instance;
        if (voicePlayer != null)
        {
            voicePlayer.OnDialogueClosed();
        }
    }

    private string CurrentLine => _lines.Count == 0 ? string.Empty : _lines[_currentLineIndex];

    private bool HasMoreLines => _currentLineIndex < _lines.Count - 1;

    private void AdvanceOrHide()
    {
        if (!_isOpen)
        {
            return;
        }

        if (HasMoreLines)
        {
            _currentLineIndex++;
            SyncCanvasView();
            return;
        }

        // 所有行都显示完了
        OnAllLinesCompleted?.Invoke();

        // 如果有 DialogueRunner 在运行，通知它推进
        if (DialogueRunner.IsActive)
        {
            DialogueRunner.Instance?.Advance();
            return;
        }

        Hide();
    }

    private void SyncCanvasView()
    {
        if (!_isOpen || !UiBootstrap.TryGetDialogueView(out DialogueCanvasView dialogueView))
        {
            return;
        }

        string hintText = HasMoreLines
            ? $"E / Enter / Space 继续    Esc 关闭    {_currentLineIndex + 1}/{_lines.Count}"
            : "E / Enter / Space 关闭";

        dialogueView.ShowDialogue(_speakerName, CurrentLine, hintText);

        // Play voice for current line
        DialogueVoicePlayer voicePlayer = DialogueVoicePlayer.Instance;
        if (voicePlayer != null)
        {
            voicePlayer.PlayLine(_speakerName, _currentLineIndex, CurrentLine);
        }
    }
}
