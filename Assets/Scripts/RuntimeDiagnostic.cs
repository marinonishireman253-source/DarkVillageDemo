using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 挂在任意对象上，按 F1 在 Console 输出诊断信息。
/// </summary>
public class RuntimeDiagnostic : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            RunDiagnostic();
        }
    }

    private void RunDiagnostic()
    {
        Debug.Log("=== 运行时诊断 ===");

        // Player
        PlayerMover pm = FindFirstObjectByType<PlayerMover>();
        if (pm != null)
        {
            Debug.Log($"[PlayerMover] 位置={pm.transform.position} 速度={pm.Velocity} 有交互目标={pm.HasInteractableTarget} enabled={pm.enabled}");
            Debug.Log($"[PlayerMover] IsSprintActive={pm.IsSprintActive}");
        }
        else
        {
            Debug.LogWarning("[PlayerMover] 未找到！");
        }

        // Camera
        CameraFollow cf = FindFirstObjectByType<CameraFollow>();
        if (cf != null)
        {
            Debug.Log($"[CameraFollow] 位置={cf.transform.position} target={cf.Target}");
        }
        else
        {
            Debug.LogWarning("[CameraFollow] 未找到！");
        }

        // Input
        Debug.Log($"[Input] Keyboard.current={(Keyboard.current != null ? "可用" : "NULL")}");
        if (Keyboard.current != null)
        {
            Debug.Log($"[Input] W={Keyboard.current.wKey.isPressed} A={Keyboard.current.aKey.isPressed} S={Keyboard.current.sKey.isPressed} D={Keyboard.current.dKey.isPressed}");
        }

        // Dialogue
        Debug.Log($"[Dialogue] SimpleDialogueUI.IsOpen={SimpleDialogueUI.IsOpen}");
        Debug.Log($"[Dialogue] DialogueRunner.IsActive={DialogueRunner.IsActive}");

        // Systems
        Debug.Log($"[System] QuestTracker={(QuestTracker.Instance != null ? "存在" : "NULL")}");
        Debug.Log($"[System] DialogueRunner={(DialogueRunner.Instance != null ? "存在" : "NULL")}");
        Debug.Log($"[System] SimpleDialogueUI={(SimpleDialogueUI.Instance != null ? "存在" : "NULL")}");

        Debug.Log("=== 诊断结束 ===");
    }
}
