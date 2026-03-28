using UnityEngine;

/// <summary>
/// 桥接 DialogueRunner 与 SimpleDialogueUI / InteractionPromptUI。
/// 当 DialogueRunner 启动时，自动驱动 SimpleDialogueUI 显示当前节点的对话文本。
/// 同时锁住玩家移动和交互提示。
/// </summary>
public class DialogueUIBridge : MonoBehaviour
{
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
        if (node == null)
        {
            return;
        }

        if (SimpleDialogueUI.Instance != null)
        {
            SimpleDialogueUI.Instance.Show(node.SpeakerName, node.Lines);
        }
    }

    private void HandleDialogueEnded()
    {
        if (SimpleDialogueUI.Instance != null && SimpleDialogueUI.IsOpen)
        {
            SimpleDialogueUI.Instance.Hide();
        }
    }
}
