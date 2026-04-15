using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class LightZoneEffect : MonoBehaviour
{
    private static readonly List<LightZoneEffect> ActiveZones = new List<LightZoneEffect>();

    public static event Action<LightZoneEffect> OnCurrentPlayerZoneChanged;

    [Header("State")]
    [SerializeField] private string zoneLabel = "区域";
    [SerializeField] private bool isLit = true;

    [Header("Player Multipliers")]
    [SerializeField] private float litInteractionMultiplier = 1.5f;
    [SerializeField] private float darkInteractionMultiplier = 0.7f;
    [SerializeField] private float litSprintMultiplier = 1f;
    [SerializeField] private float darkSprintMultiplier = 0.8f;

    [Header("Enemy Multipliers")]
    [SerializeField] private float litEnemyAttackCooldownMultiplier = 1f;
    [SerializeField] private float darkEnemyAttackCooldownMultiplier = 0.7f;

    private readonly HashSet<PlayerMover> _players = new HashSet<PlayerMover>();
    private readonly HashSet<SimpleEnemyController> _enemies = new HashSet<SimpleEnemyController>();
    private BoxCollider _trigger;

    public bool IsLit => isLit;
    public string ZoneLabel => string.IsNullOrWhiteSpace(zoneLabel) ? "区域" : zoneLabel.Trim();

    private void Awake()
    {
        _trigger = GetComponent<BoxCollider>();
        _trigger.isTrigger = true;
    }

    private void OnEnable()
    {
        if (!ActiveZones.Contains(this))
        {
            ActiveZones.Add(this);
        }

        RefreshOccupantsFromScene();
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
        ResetTrackedOccupants();
        NotifyPlayerZoneChangedIfNeeded();
    }

    public void Configure(Vector2 horizontalBounds, float walkDepth, float roomHeight, string label, bool initialLit)
    {
        zoneLabel = string.IsNullOrWhiteSpace(label) ? zoneLabel : label.Trim();
        isLit = initialLit;

        float width = Mathf.Max(0.5f, horizontalBounds.y - horizontalBounds.x);
        float localCenterX = (horizontalBounds.x + horizontalBounds.y) * 0.5f - transform.position.x;

        _trigger = GetComponent<BoxCollider>();
        _trigger.isTrigger = true;
        _trigger.center = new Vector3(localCenterX, roomHeight * 0.5f, walkDepth);
        _trigger.size = new Vector3(width, Mathf.Max(2.6f, roomHeight), 5.8f);
    }

    public void SetLit(bool lit)
    {
        if (isLit == lit)
        {
            return;
        }

        isLit = lit;
        ReapplyEffects();
        NotifyPlayerZoneChangedIfNeeded();
    }

    public static LightZoneEffect FindBest(Vector3 worldPosition)
    {
        LightZoneEffect best = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < ActiveZones.Count; i++)
        {
            LightZoneEffect zone = ActiveZones[i];
            if (zone == null || !zone.isActiveAndEnabled || zone._trigger == null)
            {
                continue;
            }

            Bounds bounds = zone._trigger.bounds;
            if (bounds.Contains(worldPosition))
            {
                return zone;
            }

            float distance = Mathf.Abs(bounds.ClosestPoint(worldPosition).x - worldPosition.x);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = zone;
            }
        }

        return best;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TryTrackPlayer(other))
        {
            return;
        }

        TryTrackEnemy(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (TryUntrackPlayer(other))
        {
            return;
        }

        TryUntrackEnemy(other);
    }

    private bool TryTrackPlayer(Collider other)
    {
        PlayerMover player = other.GetComponentInParent<PlayerMover>();
        if (player == null || !_players.Add(player))
        {
            return false;
        }

        ApplyToPlayer(player);
        NotifyPlayerZoneChangedIfNeeded(player);
        return true;
    }

    private bool TryUntrackPlayer(Collider other)
    {
        PlayerMover player = other.GetComponentInParent<PlayerMover>();
        if (player == null || !_players.Remove(player))
        {
            return false;
        }

        RefreshPlayerFromBestZone(player);
        NotifyPlayerZoneChangedIfNeeded(player);
        return true;
    }

    private void TryTrackEnemy(Collider other)
    {
        SimpleEnemyController enemy = other.GetComponentInParent<SimpleEnemyController>();
        if (enemy == null || !_enemies.Add(enemy))
        {
            return;
        }

        ApplyToEnemy(enemy);
    }

    private void TryUntrackEnemy(Collider other)
    {
        SimpleEnemyController enemy = other.GetComponentInParent<SimpleEnemyController>();
        if (enemy == null || !_enemies.Remove(enemy))
        {
            return;
        }

        RefreshEnemyFromBestZone(enemy);
    }

    private void ReapplyEffects()
    {
        foreach (PlayerMover player in _players)
        {
            if (player != null)
            {
                ApplyToPlayer(player);
            }
        }

        foreach (SimpleEnemyController enemy in _enemies)
        {
            if (enemy != null)
            {
                ApplyToEnemy(enemy);
            }
        }
    }

    private void ApplyToPlayer(PlayerMover player)
    {
        if (player == null)
        {
            return;
        }

        player.SetLightZoneSprintMultiplier(isLit ? litSprintMultiplier : darkSprintMultiplier);
        player.SetLightZoneInteractionMultiplier(isLit ? litInteractionMultiplier : darkInteractionMultiplier);
    }

    private void ApplyToEnemy(SimpleEnemyController enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.SetLightZoneAttackCooldownMultiplier(isLit ? litEnemyAttackCooldownMultiplier : darkEnemyAttackCooldownMultiplier);
    }

    private void RefreshPlayerFromBestZone(PlayerMover player)
    {
        if (player == null)
        {
            return;
        }

        LightZoneEffect fallbackZone = FindBest(player.transform.position);
        if (fallbackZone != null && fallbackZone != this)
        {
            fallbackZone.ApplyToPlayer(player);
            return;
        }

        player.ResetLightZoneMultipliers();
    }

    private void RefreshEnemyFromBestZone(SimpleEnemyController enemy)
    {
        if (enemy == null)
        {
            return;
        }

        LightZoneEffect fallbackZone = FindBest(enemy.transform.position);
        if (fallbackZone != null && fallbackZone != this)
        {
            fallbackZone.ApplyToEnemy(enemy);
            return;
        }

        enemy.ResetLightZoneMultipliers();
    }

    private void RefreshOccupantsFromScene()
    {
        _players.Clear();
        _enemies.Clear();

        if (_trigger == null)
        {
            return;
        }

        PlayerMover player = PlayerMover.LocalInstance;
        if (player != null && _trigger.bounds.Contains(player.transform.position))
        {
            _players.Add(player);
            ApplyToPlayer(player);
        }

        SimpleEnemyController[] enemies = FindObjectsByType<SimpleEnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            SimpleEnemyController enemy = enemies[i];
            if (enemy == null || !_trigger.bounds.Contains(enemy.transform.position))
            {
                continue;
            }

            _enemies.Add(enemy);
            ApplyToEnemy(enemy);
        }
    }

    private void ResetTrackedOccupants()
    {
        foreach (PlayerMover player in _players)
        {
            if (player != null)
            {
                RefreshPlayerFromBestZone(player);
            }
        }

        foreach (SimpleEnemyController enemy in _enemies)
        {
            if (enemy != null)
            {
                RefreshEnemyFromBestZone(enemy);
            }
        }

        _players.Clear();
        _enemies.Clear();
    }

    private void NotifyPlayerZoneChangedIfNeeded(PlayerMover explicitPlayer = null)
    {
        PlayerMover player = explicitPlayer != null ? explicitPlayer : PlayerMover.LocalInstance;
        if (player == null)
        {
            OnCurrentPlayerZoneChanged?.Invoke(null);
            return;
        }

        OnCurrentPlayerZoneChanged?.Invoke(FindBest(player.transform.position));
    }
}
