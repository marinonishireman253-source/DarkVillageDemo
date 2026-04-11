using UnityEngine;

/// <summary>
/// 挂在场景触发区域上，玩家进入时播放旁白语音。
/// 通过 SimpleDialogueUI 显示文字 + DialogueVoicePlayer 播放语音。
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class NarrationTrigger : MonoBehaviour
{
    [SerializeField] private string speakerName = "Sagiri";
    [SerializeField] [TextArea(1, 3)] private string[] lines;
    [SerializeField] private int voiceLineIndex = 0;
    [SerializeField] private bool oneShot = true;

    private bool _triggered;
    private BoxCollider _trigger;

    private void Awake()
    {
        _trigger = GetComponent<BoxCollider>();
        _trigger.isTrigger = true;
    }

    /// <summary>
    /// Code-driven setup from slice scripts.
    /// </summary>
    public void Configure(string speaker, string[] narrationLines, int startLineIndex, Vector3 center, Vector3 size)
    {
        speakerName = speaker;
        lines = narrationLines;
        voiceLineIndex = startLineIndex;

        _trigger = GetComponent<BoxCollider>();
        _trigger.isTrigger = true;
        _trigger.center = center;
        _trigger.size = size;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered && oneShot) return;

        PlayerMover player = other.GetComponentInParent<PlayerMover>();
        if (player == null) return;

        if (SimpleDialogueUI.IsOpen) return;

        _triggered = true;
        SimpleDialogueUI.Instance?.Show(speakerName, lines);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
