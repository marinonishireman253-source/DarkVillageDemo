using System.IO;
using UnityEditor;
using UnityEngine;

public static class MirrorCorridorDialogueAssetBuilder
{
    private const string DataFolder = "Assets/Data/Dialogue/MirrorCorridor";
    private const string ResourcesFolder = "Assets/Resources/Dialogue";
    private const string LandingNodePath = DataFolder + "/MirrorCorridor_LandingMonologue.asset";
    private const string RuleNodePath = DataFolder + "/MirrorCorridor_RuleInscription.asset";
    private const string ChoiceNodePath = DataFolder + "/MirrorCorridor_ChoiceEcho.asset";
    private const string DialogueSetPath = ResourcesFolder + "/MirrorCorridorDialogueSet.asset";

    [MenuItem("Tools/DarkVillage/Build Mirror Corridor Dialogue Assets")]
    public static void Build()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Dialogue");
        EnsureFolder(DataFolder);
        EnsureFolder("Assets/Resources");
        EnsureFolder(ResourcesFolder);

        DialogueNode landingNode = CreateOrUpdateNode(
            LandingNodePath,
            "伊尔萨恩",
            "空气变稠了。灰烬不再飘落——它们悬浮着，像凝固的雨。",
            "这一层更窄。墙壁在往里挤。",
            "第二盏灯……藏在更深的地方。");

        DialogueNode ruleNode = CreateOrUpdateNode(
            RuleNodePath,
            "铜镜长廊",
            "走廊的尽头不是出口，是镜子。",
            "不要相信镜中所见。也不要不信。");

        DialogueNode choiceNode = CreateOrUpdateNode(
            ChoiceNodePath,
            "伊尔萨恩",
            "镜中那个人影一直没有正面对你。",
            "真要紧的不是它看见了什么，而是它在等你先选。");

        FloorDialogueSet dialogueSet = AssetDatabase.LoadAssetAtPath<FloorDialogueSet>(DialogueSetPath);
        if (dialogueSet == null)
        {
            dialogueSet = ScriptableObject.CreateInstance<FloorDialogueSet>();
            AssetDatabase.CreateAsset(dialogueSet, DialogueSetPath);
        }

        SerializedObject serializedSet = new SerializedObject(dialogueSet);
        serializedSet.FindProperty("landingMonologueNode").objectReferenceValue = landingNode;
        serializedSet.FindProperty("ruleInscriptionNode").objectReferenceValue = ruleNode;
        serializedSet.FindProperty("choiceEchoNode").objectReferenceValue = choiceNode;
        serializedSet.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialogueSet);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MirrorCorridorDialogueAssetBuilder] Mirror Corridor dialogue assets are ready.");
    }

    private static DialogueNode CreateOrUpdateNode(string assetPath, string speakerName, params string[] lines)
    {
        DialogueNode node = AssetDatabase.LoadAssetAtPath<DialogueNode>(assetPath);
        if (node == null)
        {
            node = ScriptableObject.CreateInstance<DialogueNode>();
            AssetDatabase.CreateAsset(node, assetPath);
        }

        SerializedObject serializedNode = new SerializedObject(node);
        serializedNode.FindProperty("speakerName").stringValue = speakerName;

        SerializedProperty linesProperty = serializedNode.FindProperty("lines");
        linesProperty.arraySize = lines != null ? lines.Length : 0;
        if (lines != null)
        {
            for (int index = 0; index < lines.Length; index++)
            {
                linesProperty.GetArrayElementAtIndex(index).stringValue = lines[index];
            }
        }

        serializedNode.FindProperty("completeObjectiveId").stringValue = string.Empty;
        serializedNode.FindProperty("nextNode").objectReferenceValue = null;
        serializedNode.FindProperty("choices").arraySize = 0;
        serializedNode.FindProperty("onEnterEvents").arraySize = 0;
        serializedNode.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(node);
        return node;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(leaf))
        {
            return;
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
