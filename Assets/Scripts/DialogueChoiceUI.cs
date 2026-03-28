using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueChoiceUI : MonoBehaviour
{
    private static Texture2D s_WhiteTexture;

    private GUIStyle _choiceStyle;
    private GUIStyle _choiceSelectedStyle;
    private GUIStyle _promptStyle;

    private List<DialogueChoice> _currentChoices = new List<DialogueChoice>();
    private int _selectedIndex = -1;
    private bool _isVisible;

    private void Awake()
    {
        EnsureWhiteTexture();
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
    }

    private void HandleNodeStarted(DialogueNode node)
    {
        if (node != null && node.HasChoices)
        {
            _currentChoices = new List<DialogueChoice>(node.Choices);
            _selectedIndex = 0;
            _isVisible = true;
        }
        else
        {
            _isVisible = false;
            _currentChoices.Clear();
        }
    }

    private void HandleDialogueEnded()
    {
        _isVisible = false;
        _currentChoices.Clear();
        _selectedIndex = -1;
    }

    private void Update()
    {
        if (!_isVisible || _currentChoices.Count == 0)
        {
            return;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            {
                _selectedIndex = (_selectedIndex - 1 + _currentChoices.Count) % _currentChoices.Count;
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                _selectedIndex = (_selectedIndex + 1) % _currentChoices.Count;
            }
            else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ConfirmChoice();
            }
        }
    }

    private void OnGUI()
    {
        if (!_isVisible || _currentChoices.Count == 0)
        {
            return;
        }

        EnsureStyles();

        float width = Mathf.Clamp(Screen.width * 0.7f, 500f, 800f);
        float choiceHeight = 44f;
        float padding = 8f;
        float totalHeight = _currentChoices.Count * (choiceHeight + padding) + 16f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - totalHeight - 30f;

        // 背景
        Rect panelRect = new Rect(x, y, width, totalHeight);
        DrawRect(panelRect, new Color(0.05f, 0.06f, 0.08f, 0.85f));
        DrawRect(new Rect(x + 4f, y + 4f, width - 8f, totalHeight - 8f), new Color(0.1f, 0.12f, 0.15f, 0.65f));

        // 提示文字
        GUI.Label(new Rect(x + 18f, y + 6f, width - 36f, 20f), "选择你的回应：", _promptStyle);

        float contentY = y + 28f;
        for (int i = 0; i < _currentChoices.Count; i++)
        {
            float cy = contentY + i * (choiceHeight + padding);
            Rect choiceRect = new Rect(x + 12f, cy, width - 24f, choiceHeight);

            bool isSelected = i == _selectedIndex;

            // 选项背景
            Color bgColor = isSelected
                ? new Color(0.79f, 0.67f, 0.45f, 0.35f)
                : new Color(0.15f, 0.17f, 0.2f, 0.4f);
            DrawRect(choiceRect, bgColor);

            // 选项编号和文字
            string label = isSelected
                ? $"▸ {i + 1}. {_currentChoices[i].ChoiceText}"
                : $"  {i + 1}. {_currentChoices[i].ChoiceText}";

            GUIStyle style = isSelected ? _choiceSelectedStyle : _choiceStyle;
            GUI.Label(new Rect(choiceRect.x + 14f, choiceRect.y + 6f, choiceRect.width - 28f, choiceRect.height - 12f), label, style);
        }
    }

    private void ConfirmChoice()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _currentChoices.Count)
        {
            return;
        }

        DialogueRunner.Instance?.SelectChoice(_selectedIndex);
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
        if (_choiceStyle != null)
        {
            return;
        }

        _choiceStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        _choiceStyle.normal.textColor = new Color(0.78f, 0.78f, 0.75f);

        _choiceSelectedStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        _choiceSelectedStyle.normal.textColor = new Color(0.95f, 0.93f, 0.88f);

        _promptStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleLeft
        };
        _promptStyle.normal.textColor = new Color(0.7f, 0.7f, 0.65f, 0.9f);
    }

    private static void DrawRect(Rect rect, Color color)
    {
        if (s_WhiteTexture == null)
        {
            return;
        }

        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, s_WhiteTexture, ScaleMode.StretchToFill);
        GUI.color = prev;
    }
}
