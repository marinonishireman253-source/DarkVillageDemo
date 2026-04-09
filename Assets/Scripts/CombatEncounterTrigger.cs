using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CombatEncounterTrigger : MonoBehaviour
{
    [Header("Encounter")]
    [SerializeField] private string encounterName = "第一次遭遇";
    [SerializeField] private string objectiveId = "defeat_ritual_echo";
    [SerializeField] private string objectiveText = "击败从残响中现身的异化怪物";
    [SerializeField] private string objectiveMarker = "战斗";
    [SerializeField] private string completionFlagId = "combat_encounter_cleared";
    [SerializeField] private string requiredItemId;
    [SerializeField] private string requiredObjectiveId;
    [SerializeField] private bool restorePlayerHealthOnStart;

    [Header("Enemy")]
    [SerializeField] private string enemyName = "仪式回响";
    [SerializeField] private int enemyHealth = 3;
    [SerializeField] private int enemyDamage = 1;
    [SerializeField] private Vector3 enemySpawnOffset = new Vector3(0f, 1f, 1.9f);

    [Header("Dialogue")]
    [SerializeField] private string preBattleSpeaker = "残响";
    [SerializeField] [TextArea(2, 4)] private string[] preBattleLines =
    {
        "那团黑雾像是突然找到了骨架，开始朝你站立起来。"
    };
    [SerializeField] private string postBattleSpeaker = "伊尔萨恩";
    [SerializeField] [TextArea(2, 4)] private string[] postBattleLines =
    {
        "它更像一段被硬拽出来的记忆，不像真正活着的怪物。"
    };
    [SerializeField] private string nextSceneName;
    [SerializeField] private float nextSceneDelay = 0.5f;
    [SerializeField] private bool useLocalWarp;
    [SerializeField] private Vector3 localWarpPosition;
    [SerializeField] private Vector3 localWarpForward;
    [SerializeField] private string nextObjectiveAfterBattle;

    [Header("Locked State")]
    [SerializeField] private string lockedSpeaker = "伊尔萨恩";
    [SerializeField] [TextArea(2, 4)] private string[] lockedLines =
    {
        "祭台边上的东西还没看完。先把散落的线索收起来。"
    };

    public static CombatEncounterTrigger ActiveEncounter { get; private set; }
    public static SimpleEnemyController ActiveEnemy { get; private set; }

    public string EncounterName => encounterName;

    private bool _triggered;
    private bool _completed;

    private void Awake()
    {
        if (TryGetComponent(out Collider colliderComponent))
        {
            colliderComponent.isTrigger = true;
        }
    }

    private void OnDisable()
    {
        if (ActiveEncounter == this)
        {
            ActiveEncounter = null;
        }

        if (ActiveEnemy != null && ActiveEnemy.transform.IsChildOf(transform))
        {
            ActiveEnemy = null;
        }
    }

    public void Configure(
        string newEncounterName,
        string newObjectiveId,
        string newObjectiveText,
        string newObjectiveMarker,
        string newEnemyName,
        int newEnemyHealth,
        int newEnemyDamage,
        string sceneAfterBattle)
    {
        encounterName = string.IsNullOrWhiteSpace(newEncounterName) ? encounterName : newEncounterName.Trim();
        objectiveId = string.IsNullOrWhiteSpace(newObjectiveId) ? objectiveId : newObjectiveId.Trim();
        objectiveText = string.IsNullOrWhiteSpace(newObjectiveText) ? objectiveText : newObjectiveText.Trim();
        objectiveMarker = string.IsNullOrWhiteSpace(newObjectiveMarker) ? objectiveMarker : newObjectiveMarker.Trim();
        enemyName = string.IsNullOrWhiteSpace(newEnemyName) ? enemyName : newEnemyName.Trim();
        enemyHealth = Mathf.Max(1, newEnemyHealth);
        enemyDamage = Mathf.Max(1, newEnemyDamage);
        nextSceneName = sceneAfterBattle;
        useLocalWarp = false;
    }

    public void ConfigureDialogue(string beforeSpeaker, string[] beforeLines, string afterSpeaker, string[] afterLines)
    {
        if (!string.IsNullOrWhiteSpace(beforeSpeaker))
        {
            preBattleSpeaker = beforeSpeaker.Trim();
        }

        if (beforeLines != null && beforeLines.Length > 0)
        {
            preBattleLines = beforeLines;
        }

        if (!string.IsNullOrWhiteSpace(afterSpeaker))
        {
            postBattleSpeaker = afterSpeaker.Trim();
        }

        if (afterLines != null && afterLines.Length > 0)
        {
            postBattleLines = afterLines;
        }
    }

    public void ConfigureRequirement(string itemId, string speakerName, string[] lines)
    {
        requiredItemId = itemId;

        if (!string.IsNullOrWhiteSpace(speakerName))
        {
            lockedSpeaker = speakerName.Trim();
        }

        if (lines != null && lines.Length > 0)
        {
            lockedLines = lines;
        }
    }

    public void ConfigureObjectiveRequirement(string objectiveIdToRequire)
    {
        requiredObjectiveId = string.IsNullOrWhiteSpace(objectiveIdToRequire)
            ? string.Empty
            : objectiveIdToRequire.Trim();
    }

    public void ConfigurePlayerRestore(bool shouldRestore)
    {
        restorePlayerHealthOnStart = shouldRestore;
    }

    public void ConfigureLocalWarp(Vector3 worldPosition, Vector3 worldForward, string nextObjectiveId = null, float delay = 0.5f)
    {
        useLocalWarp = true;
        localWarpPosition = worldPosition;
        localWarpForward = worldForward;
        nextSceneName = string.Empty;
        nextObjectiveAfterBattle = string.IsNullOrWhiteSpace(nextObjectiveId) ? string.Empty : nextObjectiveId.Trim();
        nextSceneDelay = Mathf.Max(0f, delay);
    }

    public void DisableSceneTransition()
    {
        useLocalWarp = false;
        nextSceneName = string.Empty;
    }

    public void ConfigurePostBattleObjective(string objectiveId)
    {
        nextObjectiveAfterBattle = string.IsNullOrWhiteSpace(objectiveId)
            ? string.Empty
            : objectiveId.Trim();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartEncounter(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartEncounter(other);
    }

    private void TryStartEncounter(Collider other)
    {
        if (_triggered || other.GetComponent<PlayerMover>() == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(requiredObjectiveId))
        {
            if (QuestTracker.Instance == null || QuestTracker.Instance.CurrentObjectiveId != requiredObjectiveId)
            {
                if (SimpleDialogueUI.Instance != null && !SimpleDialogueUI.IsOpen)
                {
                    SimpleDialogueUI.Instance.Show(lockedSpeaker, lockedLines);
                }
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(requiredItemId) && !ChapterState.HasItem(requiredItemId))
        {
            if (SimpleDialogueUI.Instance != null && !SimpleDialogueUI.IsOpen)
            {
                SimpleDialogueUI.Instance.Show(lockedSpeaker, lockedLines);
            }
            return;
        }

        _triggered = true;
        StartCoroutine(RunEncounter());
    }

    private IEnumerator RunEncounter()
    {
        ActiveEncounter = this;

        PlayerCombat playerCombat = FindFirstObjectByType<PlayerCombat>();
        if (restorePlayerHealthOnStart && playerCombat != null && playerCombat.Health != null)
        {
            playerCombat.Health.RestoreFull();
        }

        if (SimpleDialogueUI.Instance != null)
        {
            SimpleDialogueUI.Instance.Show(preBattleSpeaker, preBattleLines);
            yield return WaitForDialogueClose();
        }

        SpawnEnemy();

        if (QuestTracker.Instance != null)
        {
            Transform target = ActiveEnemy != null ? ActiveEnemy.transform : transform;
            QuestTracker.Instance.SetObjective(objectiveId, objectiveText, target, objectiveMarker);
        }
    }

    private void SpawnEnemy()
    {
        GameObject enemyRoot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyRoot.name = enemyName.Replace(" ", string.Empty);
        enemyRoot.transform.SetParent(transform, true);
        enemyRoot.transform.position = transform.position + transform.rotation * enemySpawnOffset;
        enemyRoot.transform.rotation = Quaternion.LookRotation(-transform.forward, Vector3.up);

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        Renderer renderer = enemyRoot.GetComponent<Renderer>();
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", new Color(0.34f, 0.08f, 0.1f));
        propertyBlock.SetColor("_Color", new Color(0.34f, 0.08f, 0.1f));
        renderer.SetPropertyBlock(propertyBlock);

        CombatantHealth health = enemyRoot.AddComponent<CombatantHealth>();
        health.Configure(enemyHealth);

        SimpleEnemyController enemy = enemyRoot.AddComponent<SimpleEnemyController>();
        enemy.Configure(enemyName, enemyHealth, enemyDamage);
        enemy.OnDefeated += HandleEnemyDefeated;
        ActiveEnemy = enemy;
    }

    private void HandleEnemyDefeated(SimpleEnemyController enemy)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        enemy.OnDefeated -= HandleEnemyDefeated;
        StartCoroutine(FinishEncounter());
    }

    private IEnumerator FinishEncounter()
    {
        if (QuestTracker.Instance != null)
        {
            QuestTracker.Instance.CompleteObjective(objectiveId);
        }

        if (!string.IsNullOrWhiteSpace(completionFlagId))
        {
            ChapterState.SetFlag(completionFlagId, true);
        }

        if (SimpleDialogueUI.Instance != null)
        {
            SimpleDialogueUI.Instance.Show(postBattleSpeaker, postBattleLines);
        }

        float delay = Mathf.Max(nextSceneDelay, 1.2f);
        yield return new WaitForSeconds(delay);

        while (SimpleDialogueUI.IsOpen)
        {
            yield return null;
        }

        ActiveEnemy = null;
        ActiveEncounter = null;

        if (useLocalWarp)
        {
            QuestFlowUtility.WarpPlayer(localWarpPosition, localWarpForward);
        }
        else if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneLoader.Load(nextSceneName);
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(nextObjectiveAfterBattle))
        {
            QuestFlowUtility.RegisterObjectiveById(nextObjectiveAfterBattle);
        }
    }

    private IEnumerator WaitForDialogueClose()
    {
        yield return null;

        while (SimpleDialogueUI.IsOpen)
        {
            yield return null;
        }
    }
}
