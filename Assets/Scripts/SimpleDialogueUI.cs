using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleDialogueUI : MonoBehaviour
{
    public static SimpleDialogueUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance._isOpen;
    public static bool AllLinesShown => Instance != null && Instance._currentLineIndex >= Instance._lines.Count - 1;

    public event System.Action OnAllLinesCompleted;

    private static Texture2D s_WhiteTexture;

    private bool _isOpen;
    private readonly List<string> _lines = new List<string>();
    private string _speakerName = string.Empty;
    private int _currentLineIndex;
    private int _openedFrame = -1;
    private GUIStyle _nameStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _hintStyle;
    private Vector2 _bodyScroll;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureWhiteTexture();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
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
        _bodyScroll = Vector2.zero;
    }

    public void Hide()
    {
        _isOpen = false;
        _speakerName = string.Empty;
        _lines.Clear();
        _currentLineIndex = 0;
        _openedFrame = -1;
        _bodyScroll = Vector2.zero;
    }

    private void OnGUI()
    {
        if (!_isOpen)
        {
            return;
        }

        EnsureStyles();

        float width = Mathf.Min(Screen.width * 0.86f, 960f);
        float contentWidth = width - 56f;
        float textHeight = _bodyStyle.CalcHeight(new GUIContent(CurrentLine), contentWidth - 18f);
        float bodyViewportHeight = Mathf.Clamp(textHeight, 96f, Screen.height * 0.26f);
        float height = Mathf.Clamp(bodyViewportHeight + 96f, 190f, Screen.height * 0.42f);
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - height - 24f;

        Rect panelRect = new Rect(x, y, width, height);
        DrawRect(panelRect, new Color(0.05f, 0.06f, 0.08f, 0.82f));
        DrawRect(new Rect(x + 4f, y + 4f, width - 8f, height - 8f), new Color(0.12f, 0.14f, 0.17f, 0.68f));

        if (!string.IsNullOrWhiteSpace(_speakerName))
        {
            Vector2 nameSize = _nameStyle.CalcSize(new GUIContent(_speakerName));
            float nameWidth = Mathf.Clamp(nameSize.x + 34f, 160f, width * 0.4f);
            Rect nameRect = new Rect(x + 22f, y - 18f, nameWidth, 36f);
            DrawRect(nameRect, new Color(0.79f, 0.67f, 0.45f, 0.95f));
            GUI.Label(nameRect, _speakerName, _nameStyle);
        }

        float contentX = x + 28f;
        Rect bodyViewport = new Rect(contentX, y + 28f, contentWidth, height - 82f);
        Rect bodyContent = new Rect(0f, 0f, contentWidth - 18f, textHeight + 10f);
        _bodyScroll = GUI.BeginScrollView(bodyViewport, _bodyScroll, bodyContent, false, textHeight > bodyViewport.height);
        GUI.Label(new Rect(0f, 0f, bodyContent.width, bodyContent.height), CurrentLine, _bodyStyle);
        GUI.EndScrollView();

        string hintText = HasMoreLines
            ? $"E / Enter / Space 继续    Esc 关闭    {_currentLineIndex + 1}/{_lines.Count}"
            : "E / Enter / Space 关闭";
        GUI.Label(new Rect(contentX, y + height - 34f, contentWidth, 24f), hintText, _hintStyle);
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

    private void EnsureWhiteTexture()
    {
        if (s_WhiteTexture != null)
        {
            return;
        }

        s_WhiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        s_WhiteTexture.SetPixel(0, 0, Color.white);
        s_WhiteTexture.Apply(false, true);
    }

    private void EnsureStyles()
    {
        if (_nameStyle != null)
        {
            return;
        }

        _nameStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(16, 16, 8, 8)
        };
        _nameStyle.normal.textColor = new Color(0.12f, 0.09f, 0.05f);

        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            fontSize = 22,
            richText = true,
            alignment = TextAnchor.UpperLeft
        };
        _bodyStyle.normal.textColor = new Color(0.93f, 0.91f, 0.86f);

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = 15
        };
        _hintStyle.normal.textColor = new Color(0.8f, 0.79f, 0.74f, 0.9f);
    }

    private void DrawRect(Rect rect, Color color)
    {
        if (s_WhiteTexture == null)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, s_WhiteTexture, ScaleMode.StretchToFill);
        GUI.color = previousColor;
    }
}
