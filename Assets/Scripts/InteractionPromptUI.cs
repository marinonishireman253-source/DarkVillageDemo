using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    private static Texture2D s_WhiteTexture;

    private PlayerMover _player;
    private GUIStyle _titleStyle;
    private GUIStyle _keyStyle;
    private GUIStyle _hintStyle;

    private void Awake()
    {
        EnsureWhiteTexture();
    }

    private void Update()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerMover>();
        }
    }

    private void OnGUI()
    {
        EnsureWhiteTexture();

        if (SimpleDialogueUI.IsOpen)
        {
            return;
        }

        if (_player == null || !_player.HasInteractableTarget)
        {
            return;
        }

        EnsureStyles();

        string prompt = _player.CurrentInteractable.PromptText;
        string displayName = _player.CurrentInteractable.DisplayName;

        float width = Mathf.Clamp(Screen.width * 0.38f, 300f, 460f);
        float height = 72f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - 126f;

        Rect panelRect = new Rect(x, y, width, height);
        DrawRect(panelRect, new Color(0.05f, 0.06f, 0.08f, 0.76f));
        DrawRect(new Rect(x + 3f, y + 3f, width - 6f, height - 6f), new Color(0.12f, 0.14f, 0.17f, 0.58f));
        DrawRect(new Rect(x + 12f, y + 10f, 40f, height - 20f), new Color(0.83f, 0.71f, 0.49f, 0.96f));

        GUI.Label(new Rect(x + 12f, y + 10f, 40f, height - 20f), "E", _keyStyle);
        GUI.Label(new Rect(x + 64f, y + 8f, width - 76f, 26f), displayName, _titleStyle);
        GUI.Label(new Rect(x + 64f, y + 34f, width - 76f, 22f), $"[{prompt}]  靠近后按键进行交互", _hintStyle);
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
        if (_titleStyle != null)
        {
            return;
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        _titleStyle.normal.textColor = new Color(0.95f, 0.93f, 0.88f);

        _keyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        _keyStyle.normal.textColor = new Color(0.12f, 0.09f, 0.05f);

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 13
        };
        _hintStyle.normal.textColor = new Color(0.74f, 0.74f, 0.71f, 0.92f);
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
