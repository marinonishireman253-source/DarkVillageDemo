using UnityEngine;

[CreateAssetMenu(fileName = "CorePrefabCatalog", menuName = "DarkVillage/Core Prefab Catalog")]
public sealed class CorePrefabCatalog : ScriptableObject
{
    private const string ResourcePath = "Prefabs/CorePrefabCatalog";
    private static CorePrefabCatalog s_RuntimeCatalog;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject standardEnemyPrefab;
    [SerializeField] private GameObject interactionPromptPrefab;
    [SerializeField] private GameObject brazierPrefab;

    public GameObject PlayerPrefab => playerPrefab;
    public GameObject StandardEnemyPrefab => standardEnemyPrefab;
    public GameObject InteractionPromptPrefab => interactionPromptPrefab;
    public GameObject BrazierPrefab => brazierPrefab;

    public static CorePrefabCatalog Load()
    {
        if (s_RuntimeCatalog != null)
        {
            return s_RuntimeCatalog;
        }

        s_RuntimeCatalog = Resources.Load<CorePrefabCatalog>(ResourcePath);
        if (s_RuntimeCatalog == null)
        {
            Debug.LogError($"[CorePrefabCatalog] Missing resource asset at Resources/{ResourcePath}.", null);
        }

        return s_RuntimeCatalog;
    }

    public void Configure(GameObject player, GameObject standardEnemy, GameObject interactionPrompt, GameObject brazier)
    {
        playerPrefab = player;
        standardEnemyPrefab = standardEnemy;
        interactionPromptPrefab = interactionPrompt;
        brazierPrefab = brazier;
    }
}
