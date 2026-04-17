using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CorePrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string CatalogFolder = "Assets/Resources/Prefabs";
    private const string CatalogAssetPath = CatalogFolder + "/CorePrefabCatalog.asset";
    private const string PlayerPrefabPath = PrefabFolder + "/Player.prefab";
    private const string StandardEnemyPrefabPath = PrefabFolder + "/StandardEnemy.prefab";
    private const string InteractionPromptPrefabPath = PrefabFolder + "/InteractionPrompt.prefab";
    private const string BrazierPrefabPath = PrefabFolder + "/Brazier.prefab";
    private const string DisplayFontAssetPath = "Assets/Resources/Fonts/TMP/Hiragino Sans GB UI Display SDF.asset";

    [MenuItem("Tools/DarkVillage/Build Core Prefabs")]
    public static void BuildCorePrefabs()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(CatalogFolder);

        GameObject playerPrefab = SavePrefab(CreatePlayerPrefabRoot(), PlayerPrefabPath);
        GameObject standardEnemyPrefab = SavePrefab(CreateStandardEnemyPrefabRoot(), StandardEnemyPrefabPath);
        GameObject interactionPromptPrefab = SavePrefab(CreateInteractionPromptPrefabRoot(), InteractionPromptPrefabPath);
        GameObject brazierPrefab = SavePrefab(CreateBrazierPrefabRoot(), BrazierPrefabPath);

        CorePrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<CorePrefabCatalog>(CatalogAssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CorePrefabCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
        }

        catalog.Configure(playerPrefab, standardEnemyPrefab, interactionPromptPrefab, brazierPrefab);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CorePrefabBuilder] Core prefabs and runtime catalog rebuilt.");
    }

    private static GameObject CreatePlayerPrefabRoot()
    {
        GameObject root = new GameObject("Player");

        root.AddComponent<CombatantHealth>();
        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1f, 0f);
        capsule.height = 2f;
        capsule.radius = 0.33f;

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        root.AddComponent<PlayerMover>();
        root.AddComponent<PlayerCombat>();
        root.AddComponent<PlayerSpriteVisual>();
        root.AddComponent<PlayerSplashEffect>();

        GameObject spriteVisual = new GameObject("SpriteVisual");
        spriteVisual.transform.SetParent(root.transform, false);
        SpriteRenderer spriteRenderer = spriteVisual.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 10;

        return root;
    }

    private static GameObject CreateStandardEnemyPrefabRoot()
    {
        GameObject root = new GameObject("StandardEnemy");

        root.AddComponent<CombatantHealth>();
        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.58f, 0f);
        capsule.height = 1.16f;
        capsule.radius = 0.24f;

        root.AddComponent<SimpleEnemyController>();
        root.AddComponent<MonsterSpriteVisual>();

        GameObject visualRoot = new GameObject("MonsterSpriteVisualRoot");
        visualRoot.transform.SetParent(root.transform, false);
        SpriteRenderer spriteRenderer = visualRoot.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 10;

        return root;
    }

    private static GameObject CreateInteractionPromptPrefabRoot()
    {
        GameObject root = new GameObject("InteractionPrompt");
        root.AddComponent<InteractionPromptUI>();
        return root;
    }

    private static GameObject CreateBrazierPrefabRoot()
    {
        GameObject root = new GameObject("Brazier");
        BoxCollider interaction = root.AddComponent<BoxCollider>();
        interaction.isTrigger = true;
        interaction.center = new Vector3(0f, 0.96f, 0.04f);
        interaction.size = new Vector3(1.6f, 1.8f, 1.5f);

        Renderer[] renderers =
        {
            CreateDecorCube(root.transform, "Pedestal", new Vector3(0f, 0.34f, 0f), new Vector3(0.86f, 0.68f, 0.86f), new Color(0.22f, 0.2f, 0.19f)),
            CreateDecorCube(root.transform, "Bowl", new Vector3(0f, 0.92f, 0.04f), new Vector3(1.08f, 0.18f, 1.08f), new Color(0.3f, 0.24f, 0.2f)),
            CreateDecorCube(root.transform, "Ember", new Vector3(0f, 1.1f, 0.04f), new Vector3(0.5f, 0.34f, 0.5f), new Color(0.28f, 0.2f, 0.16f))
        };

        GameObject glowRoot = new GameObject("Glow");
        glowRoot.transform.SetParent(root.transform, false);
        glowRoot.transform.localPosition = new Vector3(0f, 1.18f, 0.04f);
        Light flameLight = glowRoot.AddComponent<Light>();
        flameLight.type = LightType.Point;
        flameLight.shadows = LightShadows.None;
        flameLight.range = 1.8f;
        flameLight.intensity = 0.08f;
        flameLight.color = new Color(0.46f, 0.32f, 0.22f, 1f);

        AshParlorBrazierInteractable brazier = root.AddComponent<AshParlorBrazierInteractable>();
        brazier.Configure(null, 1, flameLight, renderers);
        return root;
    }

    private static Renderer CreateDecorCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return cube.GetComponent<Renderer>();
    }

    private static TMP_Text CreateTextElement(Transform parent, string name, TMP_FontAsset font, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static GameObject SavePrefab(GameObject root, string assetPath)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
