using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Ersarn/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string speakerName = "守夜人";
    [SerializeField] private string promptText = "交谈";
    [SerializeField] [TextArea(2, 4)] private List<string> lines = new List<string>
    {
        "夜色压下来以后，村口就没有那么好认了。",
        "要是你准备继续往前走，记得沿着灯火最弱的那条路。"
    };

    public string SpeakerName => speakerName;
    public string PromptText => string.IsNullOrWhiteSpace(promptText) ? "交谈" : promptText.Trim();
    public IReadOnlyList<string> Lines => lines;
}
