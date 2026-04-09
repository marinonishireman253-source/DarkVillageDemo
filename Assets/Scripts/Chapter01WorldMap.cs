using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Chapter01WorldMap : MonoBehaviour
{
    private const string RootName = "__Chapter01WorldMap";
    private const string WorldEntryMarkerName = "WorldEntryMarker";
    private const string EventRoomRootName = "__PrologueEventRoomSlice";
    private const string EntranceRootName = "__RedCreekEntranceSlice";
    private const string CoreRootName = "__RedCreekCoreSlice";
    private const string BossRootName = "__RedCreekBossHouseSlice";
    private const string EndRootName = "__RedCreekEndSlice";

    private PlayerMover _player;
    private Vector3 _forward;
    private Vector3 _right;

    private Vector3 _eventRoomOffset;
    private Vector3 _entranceOffset;
    private Vector3 _coreOffset;
    private Vector3 _bossOffset;
    private Vector3 _endOffset;
    private Vector3 _eventRoomApproach;
    private Vector3 _eventRoomEntrance;
    private Vector3 _streetToVillageJunction;
    private Vector3 _bossCellarThreshold;
    private Vector3 _endEntry;

    public static void Ensure(PlayerMover player)
    {
        if (player == null || SceneManager.GetActiveScene().name != SceneLoader.MainSceneName)
        {
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        Chapter01WorldMap map = root.AddComponent<Chapter01WorldMap>();
        map.Build(player);
    }

    private void Build(PlayerMover player)
    {
        _player = player;
        _forward = Camera.main != null
            ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up)
            : new Vector3(1f, 0f, 1f);

        if (_forward.sqrMagnitude <= 0.001f)
        {
            _forward = new Vector3(1f, 0f, 1f);
        }

        _forward.Normalize();
        _right = Vector3.Cross(Vector3.up, _forward).normalized;

        _eventRoomOffset = _forward * 58f + _right * 18f;
        _entranceOffset = _forward * 92f + _right * 3f;
        _coreOffset = _forward * 136f + _right * 3f;
        _bossOffset = _forward * 182f + _right * 5f;
        _endOffset = _bossOffset + _forward * 26f + _right * 2f;
        _streetToVillageJunction = _forward * 45f + _right * 1.5f;
        _eventRoomApproach = _forward * 50f + _right * 10f;
        _eventRoomEntrance = _eventRoomOffset - _forward * 3f;
        _bossCellarThreshold = _bossOffset + _forward * 9.5f + _right * 3.5f;
        _endEntry = _endOffset - _forward * 8f;

        PrologueEventRoomSlice.EnsureWorld(_player, _eventRoomOffset, _forward);
        Chapter01RedCreekEntranceSlice.EnsureWorld(_player, _entranceOffset, _forward);
        Chapter01RedCreekCoreSlice.EnsureWorld(_player, _coreOffset, _forward);
        Chapter01BossHouseSlice.EnsureWorld(_player, _bossOffset, _forward);
        Chapter01EndSlice.EnsureWorld(_player, _endOffset, _forward);

        BuildOutdoorConnectors();

        ConfigureTransitions();
    }

    private void BuildOutdoorConnectors()
    {
        CreateLane(
            "WorldRoad_MainToEntrance",
            _forward * 33f,
            _entranceOffset - _forward * 10f,
            7.4f,
            new Color(0.24f, 0.21f, 0.18f),
            new Color(0.2f, 0.25f, 0.18f));
        CreateLane(
            "WorldRoad_EntranceToCore",
            _entranceOffset + _forward * 6f,
            _coreOffset - _forward * 9f,
            7.2f,
            new Color(0.24f, 0.21f, 0.18f),
            new Color(0.2f, 0.25f, 0.18f));
        CreateLane(
            "WorldRoad_CoreToBoss",
            _coreOffset + _forward * 7f,
            _bossOffset - _forward * 10f,
            7.2f,
            new Color(0.23f, 0.2f, 0.18f),
            new Color(0.19f, 0.23f, 0.17f));
        CreateLane(
            "WorldRoad_EventRoomBranch",
            _streetToVillageJunction,
            _eventRoomApproach,
            5.2f,
            new Color(0.22f, 0.19f, 0.17f),
            new Color(0.18f, 0.22f, 0.16f));
        CreateLane(
            "WorldRoad_EventRoomEntrance",
            _eventRoomApproach,
            _eventRoomEntrance,
            4.6f,
            new Color(0.21f, 0.18f, 0.16f),
            new Color(0.17f, 0.21f, 0.16f));
        CreateLane(
            "WorldRoad_BossToCellar",
            _bossCellarThreshold,
            _endEntry,
            5.4f,
            new Color(0.2f, 0.18f, 0.17f),
            new Color(0.16f, 0.18f, 0.16f));

        CreatePocket("WorldPocket_EventRoomYard", _eventRoomEntrance - _forward * 1.5f, new Vector3(12f, 0.12f, 8f), new Color(0.19f, 0.17f, 0.16f));
        CreatePocket("WorldPocket_EndAntechamber", _endEntry + _forward * 2f, new Vector3(12f, 0.12f, 10f), new Color(0.17f, 0.16f, 0.16f));
        CreateBlock("WorldEventRoomArchLeft", _eventRoomApproach - _right * 2.2f + Vector3.up * 2f, new Vector3(0.7f, 4f, 0.9f), Quaternion.LookRotation(_forward, Vector3.up), new Color(0.24f, 0.22f, 0.2f));
        CreateBlock("WorldEventRoomArchRight", _eventRoomApproach + _right * 2.2f + Vector3.up * 2f, new Vector3(0.7f, 4f, 0.9f), Quaternion.LookRotation(_forward, Vector3.up), new Color(0.24f, 0.22f, 0.2f));
        CreateBlock("WorldEventRoomArchLintel", _eventRoomApproach + Vector3.up * 4f, new Vector3(5f, 0.6f, 0.9f), Quaternion.LookRotation(_forward, Vector3.up), new Color(0.24f, 0.22f, 0.2f));
        CreateBlock("WorldCellarArchLeft", _bossCellarThreshold - _right * 1.6f + Vector3.up * 1.8f, new Vector3(0.7f, 3.6f, 0.8f), Quaternion.LookRotation(_forward, Vector3.up), new Color(0.22f, 0.19f, 0.18f));
        CreateBlock("WorldCellarArchRight", _bossCellarThreshold + _right * 1.6f + Vector3.up * 1.8f, new Vector3(0.7f, 3.6f, 0.8f), Quaternion.LookRotation(_forward, Vector3.up), new Color(0.22f, 0.19f, 0.18f));
        CreateBlock("WorldCellarArchLintel", _bossCellarThreshold + Vector3.up * 3.5f, new Vector3(3.8f, 0.5f, 0.8f), Quaternion.LookRotation(_forward, Vector3.up), new Color(0.22f, 0.19f, 0.18f));
    }

    private void ConfigureTransitions()
    {
        ConfigureSceneTriggerObjectiveOnly("AnomalyThreshold", "inspect_ritual_altar");
        ConfigureCombatObjectiveOnly("EchoCircle", "talk_old_woman");
        ConfigureSceneTriggerObjectiveOnly("VillageCoreGate", "inspect_dinner_table");
        ConfigureSceneTriggerObjectiveOnly("BossHouseApproach", "inspect_mayor_desk");
        ConfigureCombatObjectiveOnly("CellarDescent", "inspect_bound_heart");
    }

    private void ConfigureSceneTriggerWarp(string triggerName, Vector3 worldPosition, string nextObjectiveId)
    {
        GameObject trigger = GameObject.Find(triggerName);
        if (trigger == null || !trigger.TryGetComponent(out SceneDialogueTrigger dialogueTrigger))
        {
            return;
        }

        dialogueTrigger.ConfigureLocalWarp(worldPosition, _forward, 0.45f);
        dialogueTrigger.ConfigurePostDialogueObjective(nextObjectiveId);
    }

    private void ConfigureSceneTriggerObjectiveOnly(string triggerName, string nextObjectiveId)
    {
        GameObject trigger = GameObject.Find(triggerName);
        if (trigger == null || !trigger.TryGetComponent(out SceneDialogueTrigger dialogueTrigger))
        {
            return;
        }

        dialogueTrigger.DisableSceneTransition();
        dialogueTrigger.ConfigurePostDialogueObjective(nextObjectiveId);
    }

    private void ConfigureCombatWarp(string triggerName, Vector3 worldPosition, string nextObjectiveId)
    {
        GameObject trigger = GameObject.Find(triggerName);
        if (trigger == null || !trigger.TryGetComponent(out CombatEncounterTrigger combatTrigger))
        {
            return;
        }

        combatTrigger.ConfigureLocalWarp(worldPosition, _forward, nextObjectiveId, 0.5f);
    }

    private void ConfigureCombatObjectiveOnly(string triggerName, string nextObjectiveId)
    {
        GameObject trigger = GameObject.Find(triggerName);
        if (trigger == null || !trigger.TryGetComponent(out CombatEncounterTrigger combatTrigger))
        {
            return;
        }

        combatTrigger.DisableSceneTransition();
        combatTrigger.ConfigurePostBattleObjective(nextObjectiveId);
    }

    private Vector3 FindMarkerPosition(string rootName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root != null)
        {
            Transform marker = root.transform.Find(WorldEntryMarkerName);
            if (marker != null)
            {
                return marker.position;
            }

            return root.transform.position + Vector3.up;
        }

        return Vector3.up;
    }

    private void CreateBlock(string name, Vector3 position, Vector3 scale, Quaternion rotation, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(transform, true);
        block.transform.position = position;
        block.transform.rotation = rotation;
        block.transform.localScale = scale;

        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = null;
            if (renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }

    private void CreateLane(string name, Vector3 start, Vector3 end, float width, Color roadColor, Color sideColor)
    {
        Vector3 delta = end - start;
        Vector3 flatDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
        if (flatDelta.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(flatDelta.normalized, Vector3.up);
        Vector3 midpoint = (start + end) * 0.5f;
        float length = flatDelta.magnitude;

        CreateBlock($"{name}_Road", midpoint + Vector3.up * 0.04f, new Vector3(width, 0.1f, length), rotation, roadColor);
        CreateBlock($"{name}_Left", midpoint - _right * (width * 0.92f) + Vector3.up * 0.02f, new Vector3(width * 0.9f, 0.06f, length), rotation, sideColor);
        CreateBlock($"{name}_Right", midpoint + _right * (width * 0.92f) + Vector3.up * 0.02f, new Vector3(width * 0.9f, 0.06f, length), rotation, sideColor);
    }

    private void CreatePocket(string name, Vector3 center, Vector3 scale, Color color)
    {
        CreateBlock(name, center + Vector3.up * 0.03f, scale, Quaternion.LookRotation(_forward, Vector3.up), color);
    }
}
