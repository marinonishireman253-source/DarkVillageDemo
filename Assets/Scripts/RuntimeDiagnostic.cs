using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RuntimeDiagnostic : MonoBehaviour
{
    private readonly StringBuilder _builder = new StringBuilder(1024);
    private string _latestReport = string.Empty;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            RunDiagnostic();
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

        PlayerMover player = FindFirstObjectByType<PlayerMover>();
        if (player != null)
        {
            _builder.AppendLine($"[Player] name={player.name} enabled={player.enabled} position={FormatVector(player.transform.position)} forward={FormatVector(player.transform.forward)}");
            _builder.AppendLine($"[Player] velocity={FormatVector(player.Velocity)} interactable={player.HasInteractableTarget} sprint={player.IsSprintActive}");
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

        CameraFollow follow = FindFirstObjectByType<CameraFollow>();
        if (mainCamera != null)
        {
            _builder.AppendLine($"[Camera] name={mainCamera.name} enabled={mainCamera.enabled} position={FormatVector(mainCamera.transform.position)} euler={FormatVector(mainCamera.transform.eulerAngles)}");
            _builder.AppendLine($"[Camera] hasFollow={(follow != null ? "yes" : "no")} target={(follow != null && follow.Target != null ? follow.Target.name : "null")}");
        }
        else
        {
            _builder.AppendLine("[Camera] missing");
        }

        GameObject interiorRoot = GameObject.Find("__TowerInteriorSlice");
        _builder.AppendLine($"[Interior] root={(interiorRoot != null ? "present" : "missing")}");

        GameStateHub gameStateHub = GameStateHub.Instance;
        _builder.AppendLine($"[Quest] current={(gameStateHub != null ? gameStateHub.CurrentObjectiveId : "hub-missing")}");

        _builder.AppendLine($"[Input] keyboard={(Keyboard.current != null ? "available" : "null")}");
        if (Keyboard.current != null)
        {
            _builder.AppendLine($"[Input] W={Keyboard.current.wKey.isPressed} A={Keyboard.current.aKey.isPressed} S={Keyboard.current.sKey.isPressed} D={Keyboard.current.dKey.isPressed}");
        }

        _builder.AppendLine($"[Dialogue] simpleOpen={SimpleDialogueUI.IsOpen} runnerActive={DialogueRunner.IsActive}");
        _builder.AppendLine($"[Systems] gameStateHub={(gameStateHub != null ? "present" : "missing")} dialogueRunner={(DialogueRunner.Instance != null ? "present" : "missing")} simpleDialogue={(SimpleDialogueUI.Instance != null ? "present" : "missing")}");
        _builder.AppendLine($"[Diagnosis] {BuildInteriorDiagnosis(player, mainCamera, follow, interiorRoot)}");
        return _builder.ToString().TrimEnd();
    }

    private string BuildInteriorDiagnosis(PlayerMover player, Camera mainCamera, CameraFollow follow, GameObject interiorRoot)
    {
        if (!IsMainScene())
        {
            return "当前不在 Main 室内场景。";
        }

        if (interiorRoot == null)
        {
            return "室内切片未生成，优先检查 GameBootstrap 和 TowerInteriorSlice 是否执行。";
        }

        if (player == null || mainCamera == null || follow == null || follow.Target != player.transform)
        {
            return "室内已生成，但玩家或 CameraFollow.Target 未对齐。先检查玩家生成和相机绑定。";
        }

        return "Main 场景、室内切片、玩家和相机绑定都正常。";
    }

    private bool IsMainScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.name == SceneLoader.MainSceneName || activeScene.path == SceneLoader.MainScenePath;
    }

    private string FormatVector(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }
}
