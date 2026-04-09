using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SceneDialogueTrigger : MonoBehaviour
{
    [SerializeField] private string speakerName = "异象";
    [SerializeField] [TextArea(2, 5)] private string[] lines =
    {
        "雾气像有脉搏一样收缩了一次。",
        "街巷尽头的人影停住，像是在等你靠近。"
    };
    [SerializeField] private string flagId = "scene_dialogue_seen";
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string loadSceneName;
    [SerializeField] private float loadDelay = 0.35f;
    [SerializeField] private bool useLocalWarp;
    [SerializeField] private Vector3 localWarpPosition;
    [SerializeField] private Vector3 localWarpForward;
    [SerializeField] private string nextObjectiveId;
    [SerializeField] private string requiredItemId;
    [SerializeField] private string requiredObjectiveId;
    [SerializeField] private string lockedSpeaker = "伊尔萨恩";
    [SerializeField] [TextArea(2, 5)] private string[] lockedLines =
    {
        "现在还不能继续往前。先把这一段的线索查清。"
    };

    private bool _triggered;
    private bool _transitionScheduled;

    private void Awake()
    {
        if (TryGetComponent(out Collider colliderComponent))
        {
            colliderComponent.isTrigger = true;
        }
    }

    public void Configure(string newSpeakerName, string newFlagId, params string[] newLines)
    {
        if (!string.IsNullOrWhiteSpace(newSpeakerName))
        {
            speakerName = newSpeakerName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(newFlagId))
        {
            flagId = newFlagId.Trim();
        }

        if (newLines != null && newLines.Length > 0)
        {
            lines = newLines;
        }
    }

    public void ConfigureSceneTransition(string sceneName, float delay = 0.35f)
    {
        loadSceneName = sceneName;
        loadDelay = Mathf.Max(0f, delay);
        useLocalWarp = false;
    }

    public void ConfigureLocalWarp(Vector3 worldPosition, Vector3 worldForward, float delay = 0.35f)
    {
        useLocalWarp = true;
        localWarpPosition = worldPosition;
        localWarpForward = worldForward;
        loadSceneName = string.Empty;
        loadDelay = Mathf.Max(0f, delay);
    }

    public void ConfigurePostDialogueObjective(string objectiveId)
    {
        nextObjectiveId = string.IsNullOrWhiteSpace(objectiveId) ? string.Empty : objectiveId.Trim();
    }

    public void DisableSceneTransition()
    {
        loadSceneName = string.Empty;
        useLocalWarp = false;
    }

    public void ConfigureRequirement(string itemId, string objectiveId, string speakerName, params string[] requirementLines)
    {
        requiredItemId = itemId;
        requiredObjectiveId = objectiveId;

        if (!string.IsNullOrWhiteSpace(speakerName))
        {
            lockedSpeaker = speakerName.Trim();
        }

        if (requirementLines != null && requirementLines.Length > 0)
        {
            lockedLines = requirementLines;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTrigger(other);
    }

    private void TryTrigger(Collider other)
    {
        if (_triggered && triggerOnce)
        {
            return;
        }

        if (other.GetComponent<PlayerMover>() == null)
        {
            return;
        }

        bool missingItem = !string.IsNullOrWhiteSpace(requiredItemId) && !ChapterState.HasItem(requiredItemId);
        bool wrongObjective = !string.IsNullOrWhiteSpace(requiredObjectiveId)
            && (QuestTracker.Instance == null || QuestTracker.Instance.CurrentObjectiveId != requiredObjectiveId);
        if (missingItem || wrongObjective)
        {
            if (SimpleDialogueUI.Instance != null && !SimpleDialogueUI.IsOpen)
            {
                SimpleDialogueUI.Instance.Show(lockedSpeaker, lockedLines);
            }
            return;
        }

        _triggered = true;

        if (!string.IsNullOrWhiteSpace(flagId))
        {
            ChapterState.SetFlag(flagId, true);
        }

        if (SimpleDialogueUI.Instance != null && !SimpleDialogueUI.IsOpen)
        {
            SimpleDialogueUI.Instance.Show(speakerName, lines);
        }

        if ((!string.IsNullOrWhiteSpace(loadSceneName) || useLocalWarp || !string.IsNullOrWhiteSpace(nextObjectiveId)) && !_transitionScheduled)
        {
            _transitionScheduled = true;
            StartCoroutine(LoadAfterDialogue());
        }
    }

    private IEnumerator LoadAfterDialogue()
    {
        float delay = Mathf.Max(loadDelay, 1.1f);
        yield return new WaitForSeconds(delay);

        while (SimpleDialogueUI.IsOpen)
        {
            yield return null;
        }

        if (useLocalWarp)
        {
            QuestFlowUtility.WarpPlayer(localWarpPosition, localWarpForward);
        }
        else if (!string.IsNullOrWhiteSpace(loadSceneName))
        {
            SceneLoader.Load(loadSceneName);
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(nextObjectiveId))
        {
            QuestFlowUtility.RegisterObjectiveById(nextObjectiveId);
        }
    }
}
