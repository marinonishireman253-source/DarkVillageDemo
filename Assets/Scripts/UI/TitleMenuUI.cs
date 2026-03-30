using UnityEngine;

public class TitleMenuUI : MonoBehaviour
{
    private static Texture2D s_WhiteTexture;

    private GUIStyle _titleStyle;
    private GUIStyle _subtitleStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _disabledButtonStyle;
    private GUIStyle _hintStyle;

    private void Awake()
    {
        EnsureWhiteTexture();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
    }

    private void OnGUI()
    {
        EnsureWhiteTexture();
        EnsureStyles();

        DrawBackdrop();

        float width = Mathf.Min(Screen.width * 0.4f, 480f);
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height * 0.2f;

        GUI.Label(new Rect(x, y, width, 72f), "Ersarn", _titleStyle);
        GUI.Label(new Rect(x, y + 62f, width, 40f), "2.5D Dark Epic Narrative RPG Prototype", _subtitleStyle);

        float buttonY = y + 150f;
        if (GUI.Button(new Rect(x, buttonY, width, 56f), "Start Game", _buttonStyle))
        {
            StartGame();
        }

        GUI.enabled = false;
        GUI.Button(new Rect(x, buttonY + 68f, width, 56f), "Continue (Coming Soon)", _disabledButtonStyle);
        GUI.enabled = true;

        if (GUI.Button(new Rect(x, buttonY + 136f, width, 56f), "Quit", _buttonStyle))
        {
            QuitGame();
        }

        GUI.Label(
            new Rect(x, buttonY + 212f, width, 48f),
            "Current slice: exploration, dialogue, quest marker, fixed 2.5D camera.",
            _hintStyle);
    }

    private void StartGame()
    {
        SceneLoader.LoadMain();
    }

    private void QuitGame()
    {
        Application.Quit();
    }

    private void DrawBackdrop()
    {
        DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.03f, 0.03f, 0.05f, 1f));
        DrawRect(new Rect(Screen.width * 0.08f, Screen.height * 0.1f, Screen.width * 0.84f, Screen.height * 0.8f), new Color(0.12f, 0.1f, 0.08f, 0.15f));
        DrawRect(new Rect(Screen.width * 0.16f, Screen.height * 0.18f, Screen.width * 0.68f, Screen.height * 0.64f), new Color(0.24f, 0.16f, 0.1f, 0.08f));
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
            alignment = TextAnchor.MiddleCenter,
            fontSize = 42,
            fontStyle = FontStyle.Bold
        };
        _titleStyle.normal.textColor = new Color(0.91f, 0.82f, 0.68f);

        _subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            wordWrap = true
        };
        _subtitleStyle.normal.textColor = new Color(0.73f, 0.72f, 0.67f);

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        _disabledButtonStyle = new GUIStyle(_buttonStyle);
        _disabledButtonStyle.normal.textColor = new Color(0.45f, 0.45f, 0.45f);

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 14,
            wordWrap = true
        };
        _hintStyle.normal.textColor = new Color(0.65f, 0.64f, 0.6f);
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, s_WhiteTexture, ScaleMode.StretchToFill);
        GUI.color = previousColor;
    }
}
