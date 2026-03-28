using System.Collections.Generic;
using UnityEngine;

public class PortraitController : MonoBehaviour
{
    private static Texture2D s_WhiteTexture;

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
    private GUIStyle _nameStyle;

    private readonly Dictionary<string, PortraitEntry> _portraitMap = new Dictionary<string, PortraitEntry>();

    private void Awake()
    {
        EnsureWhiteTexture();
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
    }

    private void HandleNodeStarted(DialogueNode node)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.SpeakerName))
        {
            _isVisible = false;
            return;
        }

        // 尝试用 speakerName 匹配 characterId 或 characterName
        if (_portraitMap.TryGetValue(node.SpeakerName, out PortraitEntry entry))
        {
            _currentPortrait = entry;
            _isVisible = true;
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
                    return;
                }
            }
            _isVisible = false;
        }
    }

    private void HandleDialogueEnded()
    {
        _isVisible = false;
        _currentPortrait = null;
    }

    private void OnGUI()
    {
        if (!_isVisible || _currentPortrait == null)
        {
            return;
        }

        EnsureStyles();

        float portraitSize = Mathf.Clamp(Screen.width * 0.12f, 100f, 160f);
        float x = 24f;
        float y = Screen.height - portraitSize - 140f;

        // 立绘框
        Rect frameRect = new Rect(x, y, portraitSize, portraitSize);
        DrawRect(frameRect, _currentPortrait.bgColor);
        DrawRect(new Rect(x + 3f, y + 3f, portraitSize - 6f, portraitSize - 6f), new Color(0.08f, 0.09f, 0.12f, 0.9f));

        // 立绘纹理
        if (_currentPortrait.portraitTexture != null)
        {
            Rect texRect = new Rect(x + 6f, y + 6f, portraitSize - 12f, portraitSize - 12f);
            GUI.DrawTexture(texRect, _currentPortrait.portraitTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            // 空纹理时显示占位符
            GUI.Label(frameRect, "📷", new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 32
            });
        }

        // 名字条
        if (!string.IsNullOrWhiteSpace(_currentPortrait.characterName))
        {
            float nameHeight = 26f;
            Rect nameBg = new Rect(x, y - nameHeight - 4f, portraitSize, nameHeight);
            DrawRect(nameBg, new Color(0.79f, 0.67f, 0.45f, 0.95f));
            GUI.Label(nameBg, _currentPortrait.characterName, _nameStyle);
        }
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
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        _nameStyle.normal.textColor = new Color(0.12f, 0.09f, 0.05f);
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
