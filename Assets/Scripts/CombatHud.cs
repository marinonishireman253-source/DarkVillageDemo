using UnityEngine;

public class CombatHud : MonoBehaviour
{
    private static Texture2D s_WhiteTexture;

    private PlayerCombat _playerCombat;
    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;

    private void Awake()
    {
        EnsureWhiteTexture();
    }

    private void Update()
    {
        if (_playerCombat == null)
        {
            PlayerMover player = FindFirstObjectByType<PlayerMover>();
            _playerCombat = player != null ? player.GetComponent<PlayerCombat>() : null;
        }
    }

    private void OnGUI()
    {
        EnsureWhiteTexture();

        if (_playerCombat == null || _playerCombat.Health == null)
        {
            return;
        }

        EnsureStyles();

        float x = 18f;
        float y = 18f;
        float width = 320f;
        float height = CombatEncounterTrigger.ActiveEncounter != null ? 116f : 74f;

        DrawRect(new Rect(x, y, width, height), new Color(0.04f, 0.05f, 0.07f, 0.76f));
        DrawRect(new Rect(x + 3f, y + 3f, width - 6f, height - 6f), new Color(0.12f, 0.14f, 0.17f, 0.62f));

        GUI.Label(new Rect(x + 14f, y + 10f, width - 28f, 24f), $"伊尔萨恩  HP {_playerCombat.Health.CurrentHealth}/{_playerCombat.Health.MaxHealth}", _titleStyle);
        GUI.Label(new Rect(x + 14f, y + 38f, width - 28f, 24f), "攻击键: Space / J / 鼠标左键", _bodyStyle);

        if (CombatEncounterTrigger.ActiveEncounter == null)
        {
            return;
        }

        string enemyLine = CombatEncounterTrigger.ActiveEnemy != null && CombatEncounterTrigger.ActiveEnemy.Health != null
            ? $"{CombatEncounterTrigger.ActiveEnemy.EnemyName}  HP {CombatEncounterTrigger.ActiveEnemy.Health.CurrentHealth}/{CombatEncounterTrigger.ActiveEnemy.Health.MaxHealth}"
            : "敌人未锁定";

        GUI.Label(new Rect(x + 14f, y + 64f, width - 28f, 24f), $"战斗: {CombatEncounterTrigger.ActiveEncounter.EncounterName}", _bodyStyle);
        GUI.Label(new Rect(x + 14f, y + 88f, width - 28f, 24f), enemyLine, _bodyStyle);
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
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        _titleStyle.normal.textColor = new Color(0.92f, 0.9f, 0.85f);

        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14
        };
        _bodyStyle.normal.textColor = new Color(0.78f, 0.79f, 0.8f);
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, s_WhiteTexture, ScaleMode.StretchToFill);
        GUI.color = previousColor;
    }
}
