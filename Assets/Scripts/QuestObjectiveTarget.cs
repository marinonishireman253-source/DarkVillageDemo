using UnityEngine;

public class QuestObjectiveTarget : MonoBehaviour
{
    [SerializeField] private string objectiveId = "talk_to_watchman";
    [SerializeField] private string objectiveText = "探索塔内";
    [SerializeField] private string markerText = "主目标";
    [SerializeField] private bool autoRegisterOnStart = true;
    [SerializeField] private bool completeOnInteract = true;
    [SerializeField] private QuestObjectiveTarget nextObjective;

    public string ObjectiveId => objectiveId;
    public string ObjectiveText => objectiveText;
    public string MarkerText => markerText;
    public bool AutoRegisterOnStart => autoRegisterOnStart;
    public QuestObjectiveTarget NextObjective => nextObjective;

    private bool _pendingAutoRegister;

    private void Start()
    {
        if (!autoRegisterOnStart)
        {
            return;
        }

        _pendingAutoRegister = true;
        TryAutoRegister();
    }

    private void Update()
    {
        if (!_pendingAutoRegister)
        {
            return;
        }

        TryAutoRegister();
    }

    public void RegisterAsCurrentObjective()
    {
        if (GameStateHub.Instance == null)
        {
            _pendingAutoRegister = true;
            return;
        }

        GameStateHub.Instance.SetObjective(objectiveId, objectiveText, transform, markerText);
        _pendingAutoRegister = false;
        TryResolveImmediateCompletion();
    }

    public void NotifyInteracted()
    {
        if (!completeOnInteract || GameStateHub.Instance == null)
        {
            return;
        }

        bool completed = GameStateHub.Instance.CompleteObjective(objectiveId);
        if (!completed)
        {
            return;
        }

        if (nextObjective != null)
        {
            nextObjective.RegisterAsCurrentObjective();
        }
    }

    public void Configure(string id, string text, string marker, bool autoRegister, bool completeOnUse)
    {
        objectiveId = id;
        objectiveText = text;
        markerText = marker;
        autoRegisterOnStart = autoRegister;
        completeOnInteract = completeOnUse;
    }

    public void SetNextObjective(QuestObjectiveTarget target)
    {
        nextObjective = target;
    }

    private void TryAutoRegister()
    {
        if (!_pendingAutoRegister || GameStateHub.Instance == null)
        {
            return;
        }

        RegisterAsCurrentObjective();
    }

    private void TryResolveImmediateCompletion()
    {
        if (!completeOnInteract || GameStateHub.Instance == null)
        {
            return;
        }

        if (!TryGetComponent(out PickupInteractable pickupInteractable) || !pickupInteractable.IsCollected)
        {
            return;
        }

        bool completed = GameStateHub.Instance.CompleteObjective(objectiveId);
        if (!completed)
        {
            return;
        }

        if (nextObjective != null)
        {
            nextObjective.RegisterAsCurrentObjective();
        }
    }
}
