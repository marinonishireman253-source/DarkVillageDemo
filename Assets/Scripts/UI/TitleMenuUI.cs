using UnityEngine;
using UnityEngine.InputSystem;

public class TitleMenuUI : MonoBehaviour
{
    private static Texture2D s_WhiteTexture;

    private GUIStyle _titleStyle;
    private GUIStyle _subtitleStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _disabledButtonStyle;
    private GUIStyle _buttonLabelStyle;
    private GUIStyle _disabledButtonLabelStyle;
    private GUIStyle _hintStyle;
    private GUIStyle _saveHintStyle;
    private GUIStyle _saveBadgeStyle;

    private void Awake()
    {
        EnsureWhiteTexture();
    }

    private void Update()
    {
        if (Keyboard.current != null
            && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            StartGame();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame && SaveSystem.HasSaveData())
        {
            ContinueGame();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            OpenVfxTestBench();
        }
    }

    private void OnGUI()
    {
        EnsureWhiteTexture();
        EnsureStyles();

        DrawBackdrop();

        float width = Mathf.Min(Screen.width * 0.42f, 500f);
        float x = (Screen.width - width) * 0.5f;
        float y = Mathf.Max(72f, Screen.height * 0.14f);

        GUI.Label(new Rect(x, y, width, 72f), "Ersarn", _titleStyle);
        GUI.Label(new Rect(x, y + 62f, width, 40f), "2.5D Dark Epic Narrative RPG Prototype", _subtitleStyle);

        float buttonHeight = 58f;
        float buttonGap = 14f;
        float buttonY = y + 138f;
        if (DrawMenuButton(new Rect(x, buttonY, width, buttonHeight), "Start Prologue", true))
        {
            StartGame();
        }

        bool hasSave = SaveSystem.HasSaveData();
        string continueLabel = hasSave ? "Continue" : "Continue (No Save)";
        Rect continueRect = new Rect(x, buttonY + buttonHeight + buttonGap, width, buttonHeight);
        if (DrawMenuButton(continueRect, continueLabel, hasSave))
        {
            ContinueGame();
        }

        Rect vfxRect = new Rect(x, continueRect.yMax + buttonGap, width, buttonHeight);
        if (DrawMenuButton(vfxRect, "VFX Test Bench", true))
        {
            OpenVfxTestBench();
        }

        if (DrawMenuButton(new Rect(x, vfxRect.yMax + buttonGap, width, buttonHeight), "Quit", true))
        {
            QuitGame();
        }

        GUI.Label(
            new Rect(x, vfxRect.yMax + buttonHeight + 26f, width, 52f),
            "Current slice: Chapter 01 vertical slice from the prologue street through Red Creek village to the cellar truth. A separate VFX test bench is available for iteration.",
            _hintStyle);

        GUI.Label(
            new Rect(x, vfxRect.yMax + buttonHeight + 82f, width, 24f),
            hasSave ? "Save found: Continue resumes the latest story scene and quest." : "No save found yet.",
            _saveHintStyle);

        Color badgeColor = hasSave ? new Color(0.24f, 0.41f, 0.24f, 0.96f) : new Color(0.34f, 0.24f, 0.18f, 0.96f);
        Rect badgeRect = new Rect(x, vfxRect.yMax + buttonHeight + 110f, width, 26f);
        DrawRect(badgeRect, badgeColor);
        GUI.Label(badgeRect, hasSave ? "存档已找到" : "当前没有存档", _saveBadgeStyle);

        GUI.Label(
            new Rect(x, badgeRect.yMax + 8f, width, 22f),
            hasSave ? "快捷键: Enter 开始新游戏, C 继续游戏, V 进入特效测试台" : "快捷键: Enter 开始新游戏, V 进入特效测试台",
            _saveHintStyle);
    }

    private void StartGame()
    {
        SaveSystem.DeleteSave();
        ChapterState.ResetRuntime();
        DialogueEventSystem.ClearFlags();
        SceneLoader.LoadPrologueStreet();
    }

    private void ContinueGame()
    {
        ChapterState.ResetRuntime();
        DialogueEventSystem.ClearFlags();
        if (!SaveSystem.TryLoadLatest())
        {
            SceneLoader.LoadPrologueStreet();
        }
    }

    private void OpenVfxTestBench()
    {
        ChapterState.ResetRuntime();
        DialogueEventSystem.ClearFlags();
        SceneLoader.LoadVfxTestBench();
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

        _buttonLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        _buttonLabelStyle.normal.textColor = new Color(0.94f, 0.92f, 0.87f);

        _disabledButtonLabelStyle = new GUIStyle(_buttonLabelStyle);
        _disabledButtonLabelStyle.normal.textColor = new Color(0.52f, 0.52f, 0.5f);

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 14,
            wordWrap = true
        };
        _hintStyle.normal.textColor = new Color(0.65f, 0.64f, 0.6f);

        _saveHintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 12,
            wordWrap = true
        };
        _saveHintStyle.normal.textColor = new Color(0.58f, 0.6f, 0.62f);

        _saveBadgeStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        _saveBadgeStyle.normal.textColor = new Color(0.93f, 0.91f, 0.86f);
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, s_WhiteTexture, ScaleMode.StretchToFill);
        GUI.color = previousColor;
    }

    private bool DrawMenuButton(Rect rect, string label, bool enabled)
    {
        Color outerColor = enabled ? new Color(0.17f, 0.14f, 0.11f, 0.96f) : new Color(0.11f, 0.11f, 0.12f, 0.9f);
        Color innerColor = enabled ? new Color(0.31f, 0.2f, 0.12f, 0.96f) : new Color(0.17f, 0.17f, 0.18f, 0.92f);

        DrawRect(rect, outerColor);
        DrawRect(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), innerColor);
        GUI.Label(rect, label, enabled ? _buttonLabelStyle : _disabledButtonLabelStyle);

        if (!enabled)
        {
            return false;
        }

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }
}
