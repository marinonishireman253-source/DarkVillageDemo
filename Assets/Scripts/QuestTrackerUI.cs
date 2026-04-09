using UnityEngine;

public class QuestTrackerUI : MonoBehaviour
{
    private static Texture2D s_WhiteTexture;

    [SerializeField] private float completedBannerDuration = 2.5f;

    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _markerStyle;
    private float _completedBannerUntil;
    private string _completedBannerText = string.Empty;
    private bool _lastCompletedState;

    private void Awake()
    {
        EnsureWhiteTexture();
    }

    private void Update()
    {
        QuestTracker tracker = QuestTracker.Instance;
        if (tracker == null)
        {
            _lastCompletedState = false;
            return;
        }

        if (!_lastCompletedState && tracker.IsCompleted)
        {
            _completedBannerText = string.IsNullOrWhiteSpace(tracker.LastCompletedObjectiveText)
                ? "当前目标已完成"
                : tracker.LastCompletedObjectiveText;
            _completedBannerUntil = Time.unscaledTime + Mathf.Max(0.5f, completedBannerDuration);
        }

        _lastCompletedState = tracker.IsCompleted;
    }

    private void OnGUI()
    {
        EnsureWhiteTexture();

        QuestTracker tracker = QuestTracker.Instance;
        if (tracker == null)
        {
            return;
        }

        EnsureStyles();
        DrawObjectivePanel(tracker);
        DrawWorldMarker(tracker);
        DrawCompletionBanner();
    }

    private void DrawObjectivePanel(QuestTracker tracker)
    {
        float width = Mathf.Clamp(Screen.width * 0.28f, 280f, 420f);
        float contentWidth = width - 28f;
        float bodyHeight = Mathf.Max(24f, _bodyStyle.CalcHeight(new GUIContent(tracker.CurrentObjectiveText), contentWidth));
        float height = Mathf.Clamp(bodyHeight + 40f, 64f, 118f);
        Rect rect = new Rect(Screen.width - width - 24f, 24f, width, height);

        DrawRect(rect, new Color(0.05f, 0.06f, 0.08f, 0.82f));
        DrawRect(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), new Color(0.12f, 0.14f, 0.17f, 0.62f));

        GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 20f), tracker.IsCompleted ? "任务更新" : "当前目标", _titleStyle);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 28f, rect.width - 28f, rect.height - 36f), tracker.CurrentObjectiveText, _bodyStyle);
    }

    private void DrawWorldMarker(QuestTracker tracker)
    {
        if (tracker.IsCompleted || tracker.CurrentTarget == null || Camera.main == null)
        {
            return;
        }

        Vector3 worldPosition = tracker.CurrentTarget.position + Vector3.up * 2.4f;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
        {
            return;
        }

        float x = screenPosition.x - 48f;
        float y = Screen.height - screenPosition.y - 56f;
        float width = 96f;
        float height = 28f;
        x = Mathf.Clamp(x, 16f, Screen.width - width - 16f);
        y = Mathf.Clamp(y, 96f, Screen.height - height - 16f);
        Rect rect = new Rect(x, y, width, height);

        DrawRect(rect, new Color(0.83f, 0.71f, 0.49f, 0.92f));
        GUI.Label(rect, tracker.CurrentMarkerText, _markerStyle);
    }

    private void DrawCompletionBanner()
    {
        if (string.IsNullOrWhiteSpace(_completedBannerText) || Time.unscaledTime > _completedBannerUntil)
        {
            return;
        }

        float width = Mathf.Clamp(Screen.width * 0.3f, 320f, 460f);
        float bodyHeight = Mathf.Max(24f, _bodyStyle.CalcHeight(new GUIContent(_completedBannerText), width - 32f));
        float height = Mathf.Clamp(bodyHeight + 28f, 52f, 96f);
        float x = (Screen.width - width) * 0.5f;
        float y = 26f;
        Rect rect = new Rect(x, y, width, height);

        DrawRect(rect, new Color(0.12f, 0.17f, 0.12f, 0.88f));
        DrawRect(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), new Color(0.2f, 0.28f, 0.18f, 0.76f));
        GUI.Label(new Rect(rect.x + 16f, rect.y + 14f, rect.width - 32f, rect.height - 20f), _completedBannerText, _bodyStyle);
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
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        _titleStyle.normal.textColor = new Color(0.86f, 0.72f, 0.48f);

        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        _bodyStyle.normal.textColor = new Color(0.95f, 0.93f, 0.88f);

        _markerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _markerStyle.normal.textColor = new Color(0.12f, 0.09f, 0.05f);
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
