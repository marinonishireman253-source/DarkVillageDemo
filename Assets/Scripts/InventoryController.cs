using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class InventoryController : MonoBehaviour
{
    public readonly struct FloorCollectionSummary
    {
        public FloorCollectionSummary(int collectedCount, int totalCollectibleCount)
        {
            CollectedCount = Mathf.Max(0, collectedCount);
            TotalCollectibleCount = Mathf.Max(0, totalCollectibleCount);
        }

        public int CollectedCount { get; }
        public int TotalCollectibleCount { get; }
    }

    public static InventoryController Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance._isOpen;
    public static event Action<WeaponData> OnWeaponEquipRequested;
    public static event Action<bool> OnOpenStateChanged;

    private readonly List<InventoryItemCatalog.ItemViewData> _items = new List<InventoryItemCatalog.ItemViewData>();

    private bool _isOpen;
    private int _selectedIndex;
    private float _previousTimeScale = 1f;
    private InventoryCanvasView.PanelTab _activeTab = InventoryCanvasView.PanelTab.Character;
    private GameStateHub _gameStateHub;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        PlayerCombat.OnWeaponEquipped += HandleWeaponEquipped;
        GameStateHub.OnInstanceChanged += HandleGameStateHubChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindGameStateHub(GameStateHub.Instance);
    }

    private void OnDisable()
    {
        PlayerCombat.OnWeaponEquipped -= HandleWeaponEquipped;
        GameStateHub.OnInstanceChanged -= HandleGameStateHubChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        BindGameStateHub(null);

        if (Instance == this)
        {
            HideInventory();
        }
    }

    public static int GetCollectedItemCount()
    {
        return InventoryItemCatalog.GetCollectedItems().Count;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            HideInventory();
            Instance = null;
        }
    }

    private void Update()
    {
        if (ShouldToggleThisFrame())
        {
            ToggleInventory();
            return;
        }

        if (!_isOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HideInventory();
            return;
        }

        InventoryCanvasView.PanelTab? directTab = GetDirectTabSelection();
        if (directTab.HasValue)
        {
            SetActiveTab(directTab.Value);
            return;
        }

        int tabDirection = GetTabSwitchDirection();
        if (tabDirection != 0)
        {
            SwitchTab(tabDirection);
            return;
        }

        if (_activeTab == InventoryCanvasView.PanelTab.Inventory)
        {
            int navigation = GetNavigationDirection();
            if (navigation != 0)
            {
                MoveSelection(navigation);
                return;
            }

            if (ShouldEquipSelectedItemThisFrame())
            {
                TryEquipSelectedWeapon();
            }
        }
    }

    public void ToggleInventory()
    {
        if (_isOpen)
        {
            HideInventory();
            return;
        }

        if (!CanOpen())
        {
            return;
        }

        ShowInventory();
    }

    public static FloorCollectionSummary GetCurrentFloorCollectionSummary()
    {
        HashSet<string> collectedIds = new HashSet<string>();
        string[] snapshot = GameStateHub.Instance != null
            ? GameStateHub.Instance.GetCollectedItemSnapshot()
            : System.Array.Empty<string>();
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(snapshot[i]))
            {
                continue;
            }

            collectedIds.Add(snapshot[i].Trim());
        }

        HashSet<string> totalIds = new HashSet<string>(collectedIds);
        PickupInteractable[] pickups = FindObjectsByType<PickupInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
        {
            PickupInteractable pickup = pickups[i];
            if (pickup == null || string.IsNullOrWhiteSpace(pickup.ItemId))
            {
                continue;
            }

            totalIds.Add(pickup.ItemId);
        }

        return new FloorCollectionSummary(Mathf.Min(collectedIds.Count, totalIds.Count), totalIds.Count);
    }

    public static FloorCollectionSummary GetCurrentFloorCollectionSummary(IEnumerable<string> floorItemIds)
    {
        if (floorItemIds == null)
        {
            return GetCurrentFloorCollectionSummary();
        }

        HashSet<string> uniqueFloorIds = new HashSet<string>();
        int collectedCount = 0;

        foreach (string itemId in floorItemIds)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                continue;
            }

            string normalizedItemId = itemId.Trim();
            if (!uniqueFloorIds.Add(normalizedItemId))
            {
                continue;
            }

            if (GameStateHub.Instance != null && GameStateHub.Instance.HasCollectedItem(normalizedItemId))
            {
                collectedCount++;
            }
        }

        return new FloorCollectionSummary(collectedCount, uniqueFloorIds.Count);
    }

    private bool CanOpen()
    {
        UiStateCoordinator coordinator = UiStateCoordinator.Instance;
        return coordinator == null || UiStateCoordinator.AllowsInventoryForMode(coordinator.CurrentMode);
    }

    private void ShowInventory()
    {
        RefreshItems();
        _activeTab = InventoryCanvasView.PanelTab.Character;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _isOpen = true;
        OnOpenStateChanged?.Invoke(true);
        RefreshView();
    }

    private void HideInventory()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        Time.timeScale = _previousTimeScale;
        OnOpenStateChanged?.Invoke(false);

        if (UiBootstrap.TryGetInventoryView(out InventoryCanvasView view))
        {
            view.HideInventory();
        }
    }

    private void MoveSelection(int direction)
    {
        if (_items.Count <= 0)
        {
            _selectedIndex = 0;
            RefreshView();
            return;
        }

        _selectedIndex = Mathf.Clamp(_selectedIndex + direction, 0, _items.Count - 1);
        RefreshView();
    }

    private void RefreshItems()
    {
        _items.Clear();
        _items.AddRange(InventoryItemCatalog.GetCollectedItems());

        if (_items.Count <= 0)
        {
            _selectedIndex = 0;
            return;
        }

        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _items.Count - 1);
    }

    private void RefreshView()
    {
        if (!UiBootstrap.TryGetInventoryView(out InventoryCanvasView view))
        {
            return;
        }

        InventoryCanvasView.CharacterProfileData profile = BuildCharacterProfileData();
        InventoryItemCatalog.ItemViewData? selectedItem = _items.Count > 0
            ? _items[_selectedIndex]
            : (InventoryItemCatalog.ItemViewData?)null;

        view.ShowPanel(
            _items,
            _selectedIndex,
            selectedItem,
            _activeTab,
            profile,
            "人物面板",
            GetSummaryText(),
            GetHintText());
    }

    private void HandleCollectedItemsChanged()
    {
        RefreshItems();
        if (_isOpen)
        {
            RefreshView();
        }
    }

    private void HandleWeaponEquipped(WeaponData weapon)
    {
        if (_isOpen)
        {
            RefreshView();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HideInventory();
    }

    private void HandleGameStateHubChanged(GameStateHub hub)
    {
        BindGameStateHub(hub);
    }

    private void BindGameStateHub(GameStateHub hub)
    {
        if (_gameStateHub == hub)
        {
            return;
        }

        if (_gameStateHub != null)
        {
            _gameStateHub.OnCollectedItemsChanged -= HandleCollectedItemsChanged;
            _gameStateHub.OnFlagChanged -= HandleHubFlagChanged;
        }

        _gameStateHub = hub;

        if (_gameStateHub != null)
        {
            _gameStateHub.OnCollectedItemsChanged += HandleCollectedItemsChanged;
            _gameStateHub.OnFlagChanged += HandleHubFlagChanged;
        }
    }

    private void HandleHubFlagChanged(string flagId)
    {
        if (_isOpen)
        {
            RefreshView();
        }
    }

    private static bool ShouldToggleThisFrame()
    {
        if (Keyboard.current != null
            && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame))
        {
            return true;
        }

        return Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame;
    }

    private void SetActiveTab(InventoryCanvasView.PanelTab tab)
    {
        if (_activeTab == tab)
        {
            return;
        }

        _activeTab = tab;
        RefreshView();
    }

    private void SwitchTab(int direction)
    {
        InventoryCanvasView.PanelTab nextTab = direction > 0
            ? InventoryCanvasView.PanelTab.Inventory
            : InventoryCanvasView.PanelTab.Character;
        SetActiveTab(nextTab);
    }

    private string GetSummaryText()
    {
        return _activeTab == InventoryCanvasView.PanelTab.Character
            ? "查看角色状态、当前目标与探索记录"
            : $"查看随身物件与线索    已收集 {_items.Count} 件";
    }

    private string GetHintText()
    {
        return _activeTab == InventoryCanvasView.PanelTab.Character
            ? "A/D 或 ←/→ 切换页签    1/2 直达页签    Tab / I 关闭    Esc 返回"
            : CanEquipSelectedWeapon()
                ? "A/D 或 ←/→ 切换页签    1/2 直达页签    W/S 或 ↑/↓ 切换物品    E / Enter 装备    Tab / I 关闭    Esc 返回"
                : "A/D 或 ←/→ 切换页签    1/2 直达页签    W/S 或 ↑/↓ 切换物品    Tab / I 关闭    Esc 返回";
    }

    private InventoryCanvasView.CharacterProfileData BuildCharacterProfileData()
    {
        PlayerCombat playerCombat = PlayerCombat.LocalInstance;
        GameStateHub gameStateHub = GameStateHub.Instance;

        string healthLine = playerCombat != null && playerCombat.Health != null
            ? $"生命值 {playerCombat.Health.CurrentHealth}/{playerCombat.Health.MaxHealth}"
            : "生命值 未知";
        string combatLine = playerCombat != null
            ? $"当前武器 {(playerCombat.EquippedWeapon != null ? playerCombat.EquippedWeapon.WeaponName : "徒手")}    近战伤害 {playerCombat.AttackDamage}    攻击范围 {playerCombat.AttackRange:0.0}    攻击间隔 {playerCombat.AttackCooldown:0.00}s"
            : "近战数据 暂无";
        string objectiveLine = gameStateHub != null && !string.IsNullOrWhiteSpace(gameStateHub.CurrentObjective)
            ? gameStateHub.CurrentObjective
            : "暂无当前目标，先继续探索这层空间。";
        string stateLine = CombatEncounterTrigger.ActiveEncounter != null ? "战斗中" : "探索中";
        string lastCompleteLine = gameStateHub != null && !string.IsNullOrWhiteSpace(gameStateHub.LastCompletedObjective)
            ? gameStateHub.LastCompletedObjective
            : "暂无已完成记录";
        string chapterCompleteLine = gameStateHub != null && gameStateHub.GetChapterFlag("chapter01_complete") == "true" ? "是" : "否";
        string explorationNotes =
            $"当前状态：{stateLine}\n" +
            $"已收集物件：{_items.Count} 件\n" +
            $"章节完成：{chapterCompleteLine}\n" +
            $"最近完成：{lastCompleteLine}";

        return new InventoryCanvasView.CharacterProfileData(
            "伊尔萨恩",
            "灰烬中的见证者",
            healthLine,
            combatLine,
            objectiveLine,
            explorationNotes);
    }

    private static InventoryCanvasView.PanelTab? GetDirectTabSelection()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                return InventoryCanvasView.PanelTab.Character;
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                return InventoryCanvasView.PanelTab.Inventory;
            }
        }

        return null;
    }

    private static int GetTabSwitchDirection()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                return -1;
            }

            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftShoulder.wasPressedThisFrame)
            {
                return -1;
            }

            if (Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame)
            {
                return 1;
            }
        }

        return 0;
    }

    private static int GetNavigationDirection()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                return -1;
            }

            if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame)
            {
                return -1;
            }

            if (Gamepad.current.dpad.down.wasPressedThisFrame)
            {
                return 1;
            }
        }

        return 0;
    }

    private void TryEquipSelectedWeapon()
    {
        if (!CanEquipSelectedWeapon())
        {
            return;
        }

        OnWeaponEquipRequested?.Invoke(_items[_selectedIndex].Weapon);
    }

    private bool CanEquipSelectedWeapon()
    {
        return _activeTab == InventoryCanvasView.PanelTab.Inventory
            && _items.Count > 0
            && _selectedIndex >= 0
            && _selectedIndex < _items.Count
            && _items[_selectedIndex].Weapon != null;
    }

    private static bool ShouldEquipSelectedItemThisFrame()
    {
        if (Keyboard.current != null
            && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            return true;
        }

        return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
    }
}
