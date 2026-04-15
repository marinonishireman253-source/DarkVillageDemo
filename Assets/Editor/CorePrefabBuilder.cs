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
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontAssetPath);
        if (font == null)
        {
            font = TMP_Settings.defaultFontAsset;
        }

        GameObject root = new GameObject("InteractionPrompt");
        InteractionPromptUI interactionPrompt = root.AddComponent<InteractionPromptUI>();

        GameObject canvasObject = new GameObject("Canvas");
        canvasObject.transform.SetParent(root.transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        canvas.pixelPerfect = false;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Stretch(canvasRect);

        GameObject panelObject = new GameObject("PromptPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 48f);
        panelRect.sizeDelta = new Vector2(420f, 94f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.11f, 0.92f);

        TMP_Text keyLabel = CreateTextElement(panelObject.transform, "KeyLabel", font, 24, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform keyRect = keyLabel.rectTransform;
        keyRect.anchorMin = new Vector2(0f, 0.5f);
        keyRect.anchorMax = new Vector2(0f, 0.5f);
        keyRect.pivot = new Vector2(0f, 0.5f);
        keyRect.anchoredPosition = new Vector2(28f, 0f);
        keyRect.sizeDelta = new Vector2(54f, 54f);
        keyLabel.text = "E";
        keyLabel.color = new Color(0.94f, 0.91f, 0.85f, 1f);

        TMP_Text displayNameLabel = CreateTextElement(panelObject.transform, "DisplayNameLabel", font, 18, FontStyles.Bold, TextAlignmentOptions.Left);
        RectTransform displayRect = displayNameLabel.rectTransform;
        displayRect.anchorMin = new Vector2(0f, 1f);
        displayRect.anchorMax = new Vector2(1f, 1f);
        displayRect.pivot = new Vector2(0f, 1f);
        displayRect.offsetMin = new Vector2(102f, -34f);
        displayRect.offsetMax = new Vector2(-24f, -6f);
        displayNameLabel.text = "可交互对象";
        displayNameLabel.color = new Color(0.97f, 0.94f, 0.88f, 1f);

        TMP_Text promptLabel = CreateTextElement(panelObject.transform, "PromptLabel", font, 16, FontStyles.Normal, TextAlignmentOptions.Left);
        RectTransform promptRect = promptLabel.rectTransform;
        promptRect.anchorMin = new Vector2(0f, 0f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.pivot = new Vector2(0f, 0f);
        promptRect.offsetMin = new Vector2(102f, 10f);
        promptRect.offsetMax = new Vector2(-24f, -38f);
        promptLabel.text = "交互";
        promptLabel.color = new Color(0.76f, 0.78f, 0.8f, 1f);

        interactionPrompt.ConfigureLocalPrompt(canvas, canvasGroup, panelImage, displayNameLabel, promptLabel, keyLabel);
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
