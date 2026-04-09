using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ChapterCompleteOverlay : MonoBehaviour
{
    public static bool IsVisible { get; private set; }

    private static Texture2D s_WhiteTexture;

    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _hintStyle;

    private void Awake()
    {
        EnsureWhiteTexture();
    }

    private void Update()
    {
        IsVisible = ShouldShow();
        if (!IsVisible)
        {
            return;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ReturnToTitle();
                return;
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                ReplayChapter();
            }
        }
    }

    private void OnGUI()
    {
        EnsureWhiteTexture();

        if (!ShouldShow())
        {
            IsVisible = false;
            return;
        }

        IsVisible = true;
        EnsureStyles();

        DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.02f, 0.03f, 0.54f));

        float width = Mathf.Clamp(Screen.width * 0.48f, 480f, 720f);
        float height = Mathf.Clamp(Screen.height * 0.38f, 260f, 360f);
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;

        Rect panelRect = new Rect(x, y, width, height);
        DrawRect(panelRect, new Color(0.05f, 0.06f, 0.08f, 0.94f));
        DrawRect(new Rect(x + 4f, y + 4f, width - 8f, height - 8f), new Color(0.12f, 0.14f, 0.17f, 0.92f));

        GUI.Label(new Rect(x + 28f, y + 24f, width - 56f, 36f), "第一章完成", _titleStyle);
        GUI.Label(
            new Rect(x + 28f, y + 72f, width - 56f, 110f),
            "你已经完成了赤溪村竖切片：从王都边缘封锁街区进入异常事件房，抵达赤溪村，并在村长屋地下室确认了这一章的真相。\n\n下一步最值得做的是第二章入口，或者补最小存档系统。",
            _bodyStyle);

        float buttonWidth = (width - 84f) * 0.5f;
        float buttonY = y + height - 92f;
        if (GUI.Button(new Rect(x + 28f, buttonY, buttonWidth, 48f), "返回标题", _buttonStyle))
        {
            ReturnToTitle();
        }

        if (GUI.Button(new Rect(x + 40f + buttonWidth, buttonY, buttonWidth, 48f), "重开第一章", _buttonStyle))
        {
            ReplayChapter();
        }

        GUI.Label(new Rect(x + 28f, y + height - 34f, width - 56f, 24f), "Enter / Esc 返回标题    R 重开第一章", _hintStyle);
    }

    private bool ShouldShow()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return (sceneName == SceneLoader.Chapter01EndSceneName || sceneName == SceneLoader.MainSceneName)
            && ChapterState.GetFlag("chapter01_complete");
    }

    private void ReturnToTitle()
    {
        ChapterState.ResetRuntime();
        DialogueEventSystem.ClearFlags();
        SceneLoader.LoadTitle();
    }

    private void ReplayChapter()
    {
        ChapterState.ResetRuntime();
        DialogueEventSystem.ClearFlags();
        SceneLoader.LoadPrologueStreet();
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
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        _titleStyle.normal.textColor = new Color(0.9f, 0.78f, 0.55f);

        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        _bodyStyle.normal.textColor = new Color(0.93f, 0.91f, 0.86f);

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleRight
        };
        _hintStyle.normal.textColor = new Color(0.78f, 0.79f, 0.8f);
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
