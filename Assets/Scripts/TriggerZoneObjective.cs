using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerZoneObjective : MonoBehaviour
{
    [SerializeField] private string objectiveId = "reach_gate";
    [SerializeField] private bool registerOnStart;
    [SerializeField] private bool completeOnPlayerEnter = true;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerObjectName = "Player";

    private bool _triggered;

    private void Awake()
    {
        Collider colliderComponent = GetComponent<Collider>();
        colliderComponent.isTrigger = true;
    }

    private void Start()
    {
        if (registerOnStart && TryGetComponent(out QuestObjectiveTarget objectiveTarget))
        {
            objectiveTarget.RegisterAsCurrentObjective();
        }
    }

    public void Configure(string id, bool registerAtStart, bool completeOnEnter, bool once)
    {
        objectiveId = id;
        registerOnStart = registerAtStart;
        completeOnPlayerEnter = completeOnEnter;
        triggerOnce = once;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered && triggerOnce)
        {
            return;
        }

        if (!completeOnPlayerEnter || QuestTracker.Instance == null)
        {
            return;
        }

        bool isPlayer = other.GetComponent<PlayerMover>() != null || other.name == playerObjectName;
        if (!isPlayer)
        {
            return;
        }

        bool completed = QuestTracker.Instance.CompleteObjective(objectiveId);
        if (!completed)
        {
            return;
        }

        if (TryGetComponent(out QuestObjectiveTarget objectiveTarget) && objectiveTarget.NextObjective != null)
        {
            objectiveTarget.NextObjective.RegisterAsCurrentObjective();
        }

        _triggered = true;
    }
}
