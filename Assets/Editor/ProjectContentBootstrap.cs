#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectContentBootstrap
{
    private const string DialogueFolder = "Assets/Data/Dialogue";
    private const string NpcDialogueAssetPath = "Assets/Data/Dialogue/TestNpc_Guard.asset";
    private const string StoneDialogueAssetPath = "Assets/Data/Dialogue/TestStone_Inscription.asset";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string NpcPrefabPath = "Assets/Prefabs/TestNpc.prefab";
    private const string StonePrefabPath = "Assets/Prefabs/TestStone.prefab";
    private const string ScenesFolder = "Assets/Scenes";
    private const string BootSceneFolder = "Assets/Scenes/Boot";
    private const string TitleSceneFolder = "Assets/Scenes/Title";
    private const string BootScenePath = "Assets/Scenes/Boot/Boot.unity";
    private const string TitleScenePath = "Assets/Scenes/Title/Title.unity";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string BootSceneControllerScriptPath = "Assets/Scripts/Core/BootSceneController.cs";
    private const string TitleMenuScriptPath = "Assets/Scripts/UI/TitleMenuUI.cs";

    static ProjectContentBootstrap()
    {
        EditorApplication.delayCall += EnsureProjectContent;
    }

    private static void EnsureProjectContent()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += EnsureProjectContent;
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EnsureProjectContent;
            return;
        }

        EnsureFolders();
        DialogueData npcDialogue = EnsureDialogueAsset(
            NpcDialogueAssetPath,
            "守夜人",
            "交谈",
            new[]
            {
                "夜色压下来以后，村口就没有那么好认了。",
                "要是你准备继续往前走，记得沿着灯火最弱的那条路。"
            }
        );
        DialogueData stoneDialogue = EnsureDialogueAsset(
            StoneDialogueAssetPath,
            "石碑",
            "查看",
            new[]
            {
                "石碑表面刻着一行浅白色的字：",
                "愿仍记誓者，先于黑夜到达。"
            }
        );

        EnsureNpcPrefab(npcDialogue);
        EnsureStonePrefab(stoneDialogue);
        EnsureCoreScenes();
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }

        if (!AssetDatabase.IsValidFolder(DialogueFolder))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Dialogue");
        }

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(ScenesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        if (!AssetDatabase.IsValidFolder(BootSceneFolder))
        {
            AssetDatabase.CreateFolder(ScenesFolder, "Boot");
        }

        if (!AssetDatabase.IsValidFolder(TitleSceneFolder))
        {
            AssetDatabase.CreateFolder(ScenesFolder, "Title");
        }
    }

    private static DialogueData EnsureDialogueAsset(string assetPath, string speakerName, string promptText, string[] lines)
    {
        DialogueData existing = AssetDatabase.LoadAssetAtPath<DialogueData>(assetPath);
        if (existing != null)
        {
            return existing;
        }

        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        SerializedObject serializedData = new SerializedObject(data);
        serializedData.FindProperty("speakerName").stringValue = speakerName;
        serializedData.FindProperty("promptText").stringValue = promptText;

        SerializedProperty linesProperty = serializedData.FindProperty("lines");
        linesProperty.ClearArray();
        for (int i = 0; i < lines.Length; i++)
        {
            linesProperty.InsertArrayElementAtIndex(i);
            linesProperty.GetArrayElementAtIndex(i).stringValue = lines[i];
        }

        serializedData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(data, assetPath);
        EditorUtility.SetDirty(data);
        Debug.Log($"[ProjectContentBootstrap] Created dialogue asset at {assetPath}");
        return data;
    }

    private static void EnsureNpcPrefab(DialogueData dialogueData)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);
        if (existing != null)
        {
            bool dirtyExisting = false;
            TestNpcInteractable existingInteractable = existing.GetComponent<TestNpcInteractable>();
            if (existingInteractable != null && existingInteractable.DialogueData == null && dialogueData != null)
            {
                SerializedObject serializedInteractable = new SerializedObject(existingInteractable);
                serializedInteractable.FindProperty("dialogueData").objectReferenceValue = dialogueData;
                serializedInteractable.ApplyModifiedPropertiesWithoutUndo();
                dirtyExisting = true;
            }

            if (dirtyExisting)
            {
                EditorUtility.SetDirty(existing);
            }
            return;
        }

        GameObject root = new GameObject("TestNpc");
        try
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            visual.transform.localScale = Vector3.one;

            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Object.DestroyImmediate(visualCollider);
            }

            CapsuleCollider rootCollider = root.AddComponent<CapsuleCollider>();
            rootCollider.center = new Vector3(0f, 1f, 0f);
            rootCollider.height = 2f;
            rootCollider.radius = 0.35f;

            TestNpcInteractable interactable = root.AddComponent<TestNpcInteractable>();
            Renderer renderer = visual.GetComponent<Renderer>();

            SerializedObject serializedInteractable = new SerializedObject(interactable);
            serializedInteractable.FindProperty("dialogueData").objectReferenceValue = dialogueData;
            serializedInteractable.FindProperty("highlightRenderer").objectReferenceValue = renderer;
            serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, NpcPrefabPath);
            Debug.Log($"[ProjectContentBootstrap] Created NPC prefab at {NpcPrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void EnsureStonePrefab(DialogueData dialogueData)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(StonePrefabPath);
        if (existing != null)
        {
            bool dirtyExisting = false;
            TestStoneInteractable existingInteractable = existing.GetComponent<TestStoneInteractable>();
            if (existingInteractable != null && existingInteractable.DialogueData == null && dialogueData != null)
            {
                SerializedObject serializedInteractable = new SerializedObject(existingInteractable);
                serializedInteractable.FindProperty("dialogueData").objectReferenceValue = dialogueData;
                serializedInteractable.ApplyModifiedPropertiesWithoutUndo();
                dirtyExisting = true;
            }

            if (dirtyExisting)
            {
                EditorUtility.SetDirty(existing);
            }
            return;
        }

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "TestStone";
        try
        {
            root.transform.localScale = new Vector3(1.2f, 1.5f, 0.8f);
            TestStoneInteractable interactable = root.AddComponent<TestStoneInteractable>();

            SerializedObject serializedInteractable = new SerializedObject(interactable);
            serializedInteractable.FindProperty("dialogueData").objectReferenceValue = dialogueData;
            serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, StonePrefabPath);
            Debug.Log($"[ProjectContentBootstrap] Created stone prefab at {StonePrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void EnsureCoreScenes()
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            EnsureBootScene();
            EnsureTitleScene();
        }
        finally
        {
            if (previousSetup != null && previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }
    }

    private static void EnsureBootScene()
    {
        if (File.Exists(BootScenePath))
        {
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("BootSceneController");
        AddScriptComponent(root, BootSceneControllerScriptPath);

        EditorSceneManager.SaveScene(scene, BootScenePath);
        Debug.Log($"[ProjectContentBootstrap] Created scene at {BootScenePath}");
    }

    private static void EnsureTitleScene()
    {
        if (File.Exists(TitleScenePath))
        {
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraRoot = new GameObject("Main Camera");
        cameraRoot.tag = "MainCamera";
        Camera camera = cameraRoot.AddComponent<Camera>();
        camera.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraRoot.AddComponent<AudioListener>();

        GameObject titleRoot = new GameObject("TitleMenuUI");
        AddScriptComponent(titleRoot, TitleMenuScriptPath);

        EditorSceneManager.SaveScene(scene, TitleScenePath);
        Debug.Log($"[ProjectContentBootstrap] Created scene at {TitleScenePath}");
    }

    private static void EnsureBuildSettings()
    {
        var preferredOrder = new List<string> { BootScenePath, TitleScenePath };

        if (File.Exists(MainScenePath))
        {
            preferredOrder.Add(MainScenePath);
        }

        var existingScenes = EditorBuildSettings.scenes.ToDictionary(scene => scene.path, scene => scene);
        var newScenes = new List<EditorBuildSettingsScene>();

        foreach (string path in preferredOrder)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            if (existingScenes.TryGetValue(path, out EditorBuildSettingsScene existing))
            {
                existing.enabled = true;
                newScenes.Add(existing);
            }
            else
            {
                newScenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (newScenes.Any(existing => existing.path == scene.path))
            {
                continue;
            }

            newScenes.Add(scene);
        }

        EditorBuildSettings.scenes = newScenes.ToArray();
    }

    private static void AddScriptComponent(GameObject target, string scriptPath)
    {
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        if (script == null)
        {
            Debug.LogWarning($"[ProjectContentBootstrap] Script not found at {scriptPath}");
            return;
        }

        System.Type componentType = script.GetClass();
        if (componentType == null)
        {
            Debug.LogWarning($"[ProjectContentBootstrap] Script at {scriptPath} has no class yet");
            return;
        }

        target.AddComponent(componentType);
    }
}
#endif
