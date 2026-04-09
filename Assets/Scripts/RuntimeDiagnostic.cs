using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Text;

/// <summary>
/// 挂在任意对象上，按 F1 在 Console 输出诊断信息。
/// </summary>
public class RuntimeDiagnostic : MonoBehaviour
{
    private readonly StringBuilder _builder = new StringBuilder(1024);
    private string _latestReport = string.Empty;
    private Vector2 _scrollPosition;
    private bool _panelExpanded;
    private bool _developerOverlayEnabled;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            RunDiagnostic();
        }

        if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            _developerOverlayEnabled = !_developerOverlayEnabled;

            if (!_developerOverlayEnabled)
            {
                _panelExpanded = false;
            }
        }
    }

    private void RunDiagnostic()
    {
        _latestReport = BuildReport();
        Debug.Log("=== 运行时诊断 ===");
        Debug.Log(_latestReport);
        Debug.Log("=== 诊断结束 ===");
    }

    private string BuildReport()
    {
        _builder.Clear();

        Scene activeScene = SceneManager.GetActiveScene();
        _builder.AppendLine($"[Scene] name={activeScene.name} path={activeScene.path}");

        PlayerMover pm = FindFirstObjectByType<PlayerMover>();
        if (pm != null)
        {
            Vector3 playerForward = pm.transform.forward;
            _builder.AppendLine($"[Player] name={pm.name} enabled={pm.enabled} position={FormatVector(pm.transform.position)} forward={FormatVector(playerForward)}");
            _builder.AppendLine($"[Player] velocity={FormatVector(pm.Velocity)} interactable={pm.HasInteractableTarget} sprint={pm.IsSprintActive}");
        }
        else
        {
            _builder.AppendLine("[Player] missing");
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        CameraFollow cf = FindFirstObjectByType<CameraFollow>();
        if (mainCamera != null)
        {
            _builder.AppendLine($"[Camera] name={mainCamera.name} enabled={mainCamera.enabled} position={FormatVector(mainCamera.transform.position)} euler={FormatVector(mainCamera.transform.eulerAngles)}");
            _builder.AppendLine($"[Camera] hasFollow={(cf != null ? "yes" : "no")} target={(cf != null && cf.Target != null ? cf.Target.name : "null")}");
        }
        else
        {
            _builder.AppendLine("[Camera] missing");
        }

        GameObject eventRoomRoot = GameObject.Find("__PrologueEventRoomSlice");
        _builder.AppendLine($"[EventRoom] root={(eventRoomRoot != null ? "present" : "missing")} ritualFloor={(GameObject.Find("RitualFloor") != null ? "present" : "missing")} ritualAltar={(GameObject.Find("RitualAltar") != null ? "present" : "missing")} echoCircle={(GameObject.Find("EchoCircle") != null ? "present" : "missing")}");

        QuestTracker tracker = QuestTracker.Instance;
        _builder.AppendLine($"[Quest] current={(tracker != null ? tracker.CurrentObjectiveId : "tracker-missing")}");

        _builder.AppendLine($"[Input] keyboard={(Keyboard.current != null ? "available" : "null")}");
        if (Keyboard.current != null)
        {
            _builder.AppendLine($"[Input] W={Keyboard.current.wKey.isPressed} A={Keyboard.current.aKey.isPressed} S={Keyboard.current.sKey.isPressed} D={Keyboard.current.dKey.isPressed}");
        }

        _builder.AppendLine($"[Dialogue] simpleOpen={SimpleDialogueUI.IsOpen} runnerActive={DialogueRunner.IsActive}");
        _builder.AppendLine($"[Systems] questTracker={(QuestTracker.Instance != null ? "present" : "missing")} dialogueRunner={(DialogueRunner.Instance != null ? "present" : "missing")} simpleDialogue={(SimpleDialogueUI.Instance != null ? "present" : "missing")}");
        _builder.AppendLine($"[Diagnosis] {BuildEventRoomDiagnosis(pm, mainCamera, cf, eventRoomRoot)}");
        return _builder.ToString().TrimEnd();
    }

    private string BuildEventRoomDiagnosis(PlayerMover player, Camera mainCamera, CameraFollow follow, GameObject eventRoomRoot)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != SceneLoader.PrologueEventRoomSceneName)
        {
            return "当前不在事件房场景。";
        }

        if (eventRoomRoot == null)
        {
            return "事件房切片未生成，优先检查 GameBootstrap 的场景切换后一帧补跑逻辑。";
        }

        if (GameObject.Find("RitualFloor") == null)
        {
            return "事件房根节点已存在但 RitualFloor 缺失，优先检查 PrologueEventRoomSlice.Build 是否中断。";
        }

        if (player == null || mainCamera == null || follow == null || follow.Target != player.transform)
        {
            return "事件房对象已生成，但玩家或 CameraFollow.Target 未对齐。先检查玩家出生点与相机绑定。";
        }

        return "事件房切片、玩家和相机绑定都存在；若画面仍错误，下一步只继续收事件房锚点和可视几何。";
    }

    private string FormatVector(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }

    private void OnGUI()
    {
        if (!ShouldShowOverlay())
        {
            return;
        }

        const float buttonWidth = 88f;
        const float buttonHeight = 34f;
        Rect buttonRect = new Rect(Screen.width - buttonWidth - 18f, 18f, buttonWidth, buttonHeight);
        if (GUI.Button(buttonRect, _panelExpanded ? "隐藏诊断" : "场景诊断"))
        {
            _panelExpanded = !_panelExpanded;
            RunDiagnostic();
        }

        if (!_panelExpanded)
        {
            return;
        }

        float panelWidth = Mathf.Min(560f, Screen.width - 36f);
        float panelHeight = Mathf.Min(360f, Screen.height - 72f);
        Rect panelRect = new Rect(Screen.width - panelWidth - 18f, 60f, panelWidth, panelHeight);
        GUI.Box(panelRect, "运行时诊断");

        Rect refreshRect = new Rect(panelRect.x + 12f, panelRect.y + 28f, 72f, 26f);
        if (GUI.Button(refreshRect, "刷新"))
        {
            RunDiagnostic();
        }

        Rect textRect = new Rect(panelRect.x + 12f, panelRect.y + 62f, panelRect.width - 24f, panelRect.height - 74f);
        Rect viewRect = new Rect(0f, 0f, textRect.width - 18f, Mathf.Max(textRect.height, GetTextHeight(_latestReport, textRect.width - 18f) + 12f));
        _scrollPosition = GUI.BeginScrollView(textRect, _scrollPosition, viewRect);
        GUI.TextArea(new Rect(0f, 0f, viewRect.width, viewRect.height), string.IsNullOrWhiteSpace(_latestReport) ? "点击“场景诊断”生成当前报告。" : _latestReport);
        GUI.EndScrollView();
    }

    private bool ShouldShowOverlay()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return _developerOverlayEnabled
            && activeScene.name != SceneLoader.BootSceneName
            && activeScene.name != SceneLoader.TitleSceneName;
    }

    private float GetTextHeight(string text, float width)
    {
        GUIStyle style = GUI.skin.textArea;
        return style.CalcHeight(new GUIContent(text), width);
    }
}
