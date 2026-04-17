using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryCanvasView : MonoBehaviour
{
    public enum PanelTab
    {
        Character,
        Inventory
    }

    public readonly struct CharacterProfileData
    {
        public CharacterProfileData(
            string characterName,
            string subtitle,
            string healthLine,
            string combatLine,
            string objectiveLine,
            string explorationNotes)
        {
            CharacterName = characterName;
            Subtitle = subtitle;
            HealthLine = healthLine;
            CombatLine = combatLine;
            ObjectiveLine = objectiveLine;
            ExplorationNotes = explorationNotes;
        }

        public string CharacterName { get; }
        public string Subtitle { get; }
        public string HealthLine { get; }
        public string CombatLine { get; }
        public string ObjectiveLine { get; }
        public string ExplorationNotes { get; }
    }

    private const int VisibleRowCount = 8;

    private sealed class RowWidgets
    {
        public RectTransform Root;
        public Image Background;
        public TMP_Text NameText;
        public TMP_Text CategoryText;
        public TMP_Text IndexText;
    }

    private sealed class TabWidgets
    {
        public Image Background;
        public TMP_Text Text;
    }

    private UiTheme _theme;
    private CanvasGroup _canvasGroup;
    private RectTransform _panel;
    private TMP_Text _titleText;
    private TMP_Text _summaryText;
    private TMP_Text _hintText;

    private RectTransform _characterPage;
    private RectTransform _inventoryPage;

    private TabWidgets _characterTab;
    private TabWidgets _inventoryTab;

    private TMP_Text _characterNameText;
    private TMP_Text _characterSubtitleText;
    private TMP_Text _characterHealthText;
    private TMP_Text _characterCombatText;
    private TMP_Text _characterObjectiveText;
    private TMP_Text _characterNotesText;
    private InventorySlotUI _equippedWeaponSlot;

    private TMP_Text _emptyText;
    private TMP_Text _detailTitleText;
    private TMP_Text _detailCategoryText;
    private TMP_Text _detailDescriptionText;
    private readonly List<RowWidgets> _rows = new List<RowWidgets>();

    public void Initialize(UiTheme theme)
    {
        if (_theme != null)
        {
            return;
        }

        _theme = theme != null ? theme : UiTheme.CreateRuntimeDefault();
        _theme.EnsureRuntimeDefaults();

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            UiFactory.Stretch(root);
        }

        BuildLayout();
        HideInventory();
    }

    public void ShowPanel(
        IReadOnlyList<InventoryItemCatalog.ItemViewData> items,
        int selectedIndex,
        InventoryItemCatalog.ItemViewData? selectedItem,
        PanelTab activeTab,
        CharacterProfileData profile,
        string title,
        string summary,
        string hint)
    {
        if (_panel == null)
        {
            return;
        }

        gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        _titleText.text = string.IsNullOrWhiteSpace(title) ? "夜行档案" : title.Trim();
        _summaryText.text = string.IsNullOrWhiteSpace(summary) ? string.Empty : summary.Trim();
        _hintText.text = string.IsNullOrWhiteSpace(hint) ? string.Empty : hint.Trim();

        RefreshTabs(activeTab);
        RefreshCharacterPage(profile);
        RefreshInventoryPage(items, selectedIndex, selectedItem);
    }

    public void HideInventory()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        gameObject.SetActive(false);
    }

    private void BuildLayout()
    {
        _canvasGroup = UiFactory.GetOrAddCanvasGroup(gameObject);

        UiFactory.CreateImage("InventoryDimmer", transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.01f, 0.02f, 0.72f));

        _panel = UiFactory.CreateRect(
            "InventoryPanel",
            transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1520f, 860f),
            Vector2.zero);

        UiFactory.CreateImage("PanelShadow", _panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(34f, 34f), new Vector2(20f, -20f), new Color(0f, 0f, 0f, 0.34f));
        RectTransform inner = UiComponentCatalog.BuildPanelShell(
            _panel,
            new Color(0.03f, 0.04f, 0.06f, 0.95f),
            _theme.ModalFrameSprite,
            new Color(0.08f, 0.1f, 0.14f, 0.97f),
            _theme.ChoicePanelSprite,
            new Vector2(-14f, -14f));
        UiComponentCatalog.CreateAccentLine("LeftAccent", inner, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(4f, -34f), Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.88f));

        TMP_Text eyebrow = UiFactory.CreateText(
            "Eyebrow",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-68f, 24f),
            new Vector2(34f, -24f),
            _theme.DisplayFont,
            UiFontSize.Small,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.Brass);
        eyebrow.gameObject.SetActive(false);

        _titleText = UiFactory.CreateText(
            "Title",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-68f, 42f),
            new Vector2(34f, -56f),
            _theme.DisplayFont,
            UiFontSize.PanelTitle,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        _summaryText = UiFactory.CreateText(
            "Summary",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-68f, 30f),
            new Vector2(34f, -98f),
            _theme.BodyFont,
            UiFontSize.Section,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.SecondaryText);

        RectTransform tabBar = UiFactory.CreateRect(
            "TabBar",
            inner,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(452f, 54f),
            new Vector2(34f, -152f));
        _characterTab = CreateTab(tabBar, "人物", 0f);
        _inventoryTab = CreateTab(tabBar, "背包", 228f);

        RectTransform contentRoot = UiFactory.CreateRect(
            "ContentRoot",
            inner,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);
        contentRoot.offsetMin = new Vector2(34f, 78f);
        contentRoot.offsetMax = new Vector2(-34f, -194f);

        _characterPage = UiFactory.CreateRect("CharacterPage", contentRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        _inventoryPage = UiFactory.CreateRect("InventoryPage", contentRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        BuildCharacterPage(_characterPage);
        BuildInventoryPage(_inventoryPage);

        _hintText = UiFactory.CreateText(
            "Hint",
            inner,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(-68f, 26f),
            new Vector2(34f, 28f),
            _theme.BodyFont,
            UiFontSize.Body,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.76f));
    }

    private void BuildCharacterPage(RectTransform parent)
    {
        RectTransform leftColumn = CreateCard(parent, "ProfileCard", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(360f, 0f), Vector2.zero);

        RectTransform portraitCircle = UiFactory.CreateRect(
            "PortraitCircle",
            leftColumn,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(214f, 214f),
            new Vector2(0f, -84f));
        Image portraitOuter = UiFactory.CreateImage("PortraitOuter", portraitCircle, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.34f));
        UiComponentCatalog.ApplyChrome(portraitOuter, _theme.PortraitFrameSprite, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.34f));
        Image portraitInner = UiFactory.CreateImage("PortraitInner", portraitCircle, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), Vector2.zero, new Color(0.11f, 0.12f, 0.16f, 0.98f));
        UiComponentCatalog.ApplyChrome(portraitInner, _theme.InteractionPromptSprite, new Color(0.11f, 0.12f, 0.16f, 0.98f));
        Image silhouette = UiFactory.CreateImage("Silhouette", portraitCircle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(84f, 118f), new Vector2(0f, 8f), new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.2f));
        silhouette.rectTransform.localScale = new Vector3(1f, 1.15f, 1f);

        _characterNameText = UiFactory.CreateText(
            "CharacterName",
            leftColumn,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-40f, 36f),
            new Vector2(0f, -220f),
            _theme.DisplayFont,
            28,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);

        _characterSubtitleText = UiFactory.CreateText(
            "CharacterSubtitle",
            leftColumn,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-40f, 24f),
            new Vector2(0f, -256f),
            _theme.BodyFont,
            16,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            _theme.Brass);

        RectTransform statsCard = CreateInsetCard(leftColumn, "StatsCard", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-34f, 168f), new Vector2(0f, 24f));
        TMP_Text statsEyebrow = CreateSectionLabel(statsCard, "状态");
        statsEyebrow.characterSpacing = 5f;
        _characterHealthText = CreateInfoLine(statsCard, "CharacterHealth", -58f);
        _characterCombatText = CreateInfoLine(statsCard, "CharacterCombat", -102f);

        RectTransform equipmentCard = CreateInsetCard(leftColumn, "EquipmentCard", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-34f, 148f), new Vector2(0f, 214f));
        TMP_Text equipmentEyebrow = CreateSectionLabel(equipmentCard, "装备");
        equipmentEyebrow.characterSpacing = 5f;

        Image slotBackground = UiFactory.CreateImage(
            "WeaponSlotFrame",
            equipmentCard,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(92f, 92f),
            new Vector2(20f, -82f),
            new Color(1f, 1f, 1f, 0.06f));
        UiComponentCatalog.ApplyChrome(slotBackground, _theme.KeycapBadgeSprite, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.28f));
        Image slotIcon = UiFactory.CreateImage(
            "WeaponSlotIcon",
            slotBackground.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(58f, 58f),
            Vector2.zero,
            new Color(1f, 1f, 1f, 0.08f));

        TMP_Text weaponNameText = UiFactory.CreateText(
            "WeaponName",
            equipmentCard,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-156f, 30f),
            new Vector2(132f, -82f),
            _theme.BodyFont,
            18,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        TMP_Text weaponStatText = UiFactory.CreateText(
            "WeaponStat",
            equipmentCard,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-156f, 24f),
            new Vector2(132f, -118f),
            _theme.BodyFont,
            15,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.SecondaryText);

        _equippedWeaponSlot = equipmentCard.gameObject.AddComponent<InventorySlotUI>();
        _equippedWeaponSlot.Configure(
            slotIcon,
            weaponNameText,
            weaponStatText,
            _theme.PrimaryText,
            new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.08f));

        RectTransform rightColumn = UiFactory.CreateRect("CharacterRightColumn", parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rightColumn.offsetMin = new Vector2(388f, 0f);

        RectTransform objectiveCard = CreateCard(rightColumn, "ObjectiveCard", new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        objectiveCard.offsetMin = new Vector2(0f, 20f);
        TMP_Text objectiveEyebrow = CreateSectionLabel(objectiveCard, "当前目标");
        objectiveEyebrow.characterSpacing = 5f;
        _characterObjectiveText = UiFactory.CreateText(
            "ObjectiveText",
            objectiveCard,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-36f, -72f),
            new Vector2(18f, -60f),
            _theme.BodyFont,
            22,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            _theme.PrimaryText);
        _characterObjectiveText.lineSpacing = 8f;

        RectTransform notesCard = CreateCard(rightColumn, "NotesCard", new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
        notesCard.offsetMax = new Vector2(0f, -20f);
        TMP_Text notesEyebrow = CreateSectionLabel(notesCard, "探索记录");
        notesEyebrow.characterSpacing = 5f;
        _characterNotesText = UiFactory.CreateText(
            "NotesText",
            notesCard,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-36f, -72f),
            new Vector2(18f, -60f),
            _theme.BodyFont,
            18,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            _theme.SecondaryText);
        _characterNotesText.lineSpacing = 6f;
    }

    private void BuildInventoryPage(RectTransform parent)
    {
        RectTransform listCard = CreateCard(parent, "ListCard", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(520f, 0f), Vector2.zero);
        TMP_Text listEyebrow = CreateSectionLabel(listCard, "线索与物件");
        listEyebrow.characterSpacing = 5f;

        RectTransform listHeader = UiFactory.CreateRect(
            "ListHeader",
            listCard,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-36f, 26f),
            new Vector2(0f, -60f));
        UiFactory.CreateText("IndexHeader", listHeader, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(80f, 22f), new Vector2(0f, 0f), _theme.DisplayFont, UiFontSize.Caption, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.52f)).text = "编号";
        UiFactory.CreateText("NameHeader", listHeader, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-220f, 22f), new Vector2(88f, 0f), _theme.DisplayFont, UiFontSize.Caption, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.52f)).text = "名称";
        UiFactory.CreateText("CategoryHeader", listHeader, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(140f, 22f), new Vector2(0f, 0f), _theme.DisplayFont, UiFontSize.Caption, FontStyle.Normal, TextAnchor.MiddleRight, new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.52f)).text = "分类";

        RectTransform rowContainer = UiFactory.CreateRect("RowContainer", listCard, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rowContainer.offsetMin = new Vector2(18f, 18f);
        rowContainer.offsetMax = new Vector2(-18f, -96f);

        VerticalLayoutGroup layout = rowContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < VisibleRowCount; i++)
        {
            _rows.Add(CreateRow(rowContainer, i));
        }

        RectTransform detailCard = CreateCard(parent, "DetailCard", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        detailCard.offsetMin = new Vector2(548f, 0f);
        TMP_Text detailEyebrow = CreateSectionLabel(detailCard, "物件详述");
        detailEyebrow.characterSpacing = 5f;

        _detailTitleText = UiFactory.CreateText(
            "DetailTitle",
            detailCard,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-36f, 34f),
            new Vector2(18f, -58f),
            _theme.DisplayFont,
            28,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        _detailCategoryText = UiFactory.CreateText(
            "DetailCategory",
            detailCard,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-36f, 24f),
            new Vector2(18f, -96f),
            _theme.BodyFont,
            16,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.Brass);

        RectTransform detailBody = CreateInsetCard(detailCard, "DetailBody", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-36f, 440f), new Vector2(0f, 18f));
        _detailDescriptionText = UiFactory.CreateText(
            "DetailDescription",
            detailBody,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-36f, -36f),
            new Vector2(18f, -18f),
            _theme.BodyFont,
            19,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            _theme.SecondaryText);
        _detailDescriptionText.lineSpacing = 8f;

        _emptyText = UiFactory.CreateText(
            "EmptyText",
            detailCard,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-120f, -120f),
            new Vector2(0f, 10f),
            _theme.BodyFont,
            20,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.72f));
        _emptyText.text = "你还没有拾取任何可记录的物件。";
    }

    private void RefreshTabs(PanelTab activeTab)
    {
        bool characterActive = activeTab == PanelTab.Character;
        ApplyTab(_characterTab, characterActive);
        ApplyTab(_inventoryTab, !characterActive);
        _characterPage.gameObject.SetActive(characterActive);
        _inventoryPage.gameObject.SetActive(!characterActive);
    }

    private void RefreshCharacterPage(CharacterProfileData profile)
    {
        _characterNameText.text = string.IsNullOrWhiteSpace(profile.CharacterName) ? "未命名角色" : profile.CharacterName.Trim();
        _characterSubtitleText.text = string.IsNullOrWhiteSpace(profile.Subtitle) ? string.Empty : profile.Subtitle.Trim();
        _characterHealthText.text = string.IsNullOrWhiteSpace(profile.HealthLine) ? "生命数据暂无" : profile.HealthLine.Trim();
        _characterCombatText.text = string.IsNullOrWhiteSpace(profile.CombatLine) ? "战斗数据暂无" : profile.CombatLine.Trim();
        _characterObjectiveText.text = string.IsNullOrWhiteSpace(profile.ObjectiveLine) ? "暂无当前目标。" : profile.ObjectiveLine.Trim();
        _characterNotesText.text = string.IsNullOrWhiteSpace(profile.ExplorationNotes) ? "暂无探索记录。" : profile.ExplorationNotes.Trim();
    }

    private void RefreshInventoryPage(
        IReadOnlyList<InventoryItemCatalog.ItemViewData> items,
        int selectedIndex,
        InventoryItemCatalog.ItemViewData? selectedItem)
    {
        int count = items != null ? items.Count : 0;
        bool hasItems = count > 0;

        _emptyText.gameObject.SetActive(!hasItems);
        _detailTitleText.gameObject.SetActive(hasItems);
        _detailCategoryText.gameObject.SetActive(hasItems);
        _detailDescriptionText.gameObject.SetActive(hasItems);

        int clampedSelection = hasItems ? Mathf.Clamp(selectedIndex, 0, count - 1) : 0;
        int startIndex = Mathf.Clamp(clampedSelection - (VisibleRowCount / 2), 0, Mathf.Max(0, count - VisibleRowCount));

        for (int i = 0; i < _rows.Count; i++)
        {
            int itemIndex = startIndex + i;
            bool shouldShow = hasItems && itemIndex < count;
            RowWidgets row = _rows[i];
            row.Root.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            InventoryItemCatalog.ItemViewData item = items[itemIndex];
            bool isSelected = itemIndex == clampedSelection;
            row.Background.color = isSelected
                ? new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.34f)
                : new Color(1f, 1f, 1f, 0.12f);
            row.IndexText.text = (itemIndex + 1).ToString("00");
            row.NameText.text = item.DisplayName;
            row.CategoryText.text = item.Category;
            row.NameText.color = isSelected ? _theme.PrimaryText : _theme.SecondaryText;
            row.CategoryText.color = isSelected ? _theme.Brass : new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.64f);
        }

        if (!hasItems)
        {
            return;
        }

        InventoryItemCatalog.ItemViewData detailItem = selectedItem.HasValue ? selectedItem.Value : items[clampedSelection];
        _detailTitleText.text = detailItem.DisplayName;
        _detailCategoryText.text = string.IsNullOrWhiteSpace(detailItem.Category) ? "未分类" : detailItem.Category.Trim();
        _detailDescriptionText.text = string.IsNullOrWhiteSpace(detailItem.Description)
            ? "这件物品还没有留下更多记录。"
            : detailItem.Description.Trim();
    }

    private TabWidgets CreateTab(Transform parent, string label, float xPosition)
    {
        Image background = UiFactory.CreateImage(
            label + "Tab",
            parent,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(196f, 52f),
            new Vector2(xPosition, 0f),
            new Color(1f, 1f, 1f, 0.05f));
        UiComponentCatalog.ApplyChrome(background, _theme.PrimaryButtonSprite, new Color(1f, 1f, 1f, 0.05f));
        TMP_Text text = UiFactory.CreateText(
            label + "Label",
            background.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-24f, -10f),
            Vector2.zero,
            _theme.DisplayFont,
            UiFontSize.Section,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.SecondaryText);

        return new TabWidgets
        {
            Background = background,
            Text = text
        };
    }

    private void ApplyTab(TabWidgets tab, bool active)
    {
        tab.Background.color = active
            ? new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.34f)
            : new Color(1f, 1f, 1f, 0.12f);
        tab.Text.color = active ? _theme.PrimaryText : _theme.SecondaryText;
    }

    private RowWidgets CreateRow(Transform parent, int index)
    {
        RectTransform rowRoot = UiFactory.CreateRect(
            "Row_" + index,
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, 64f),
            Vector2.zero);
        LayoutElement layoutElement = rowRoot.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 64f;

        Image background = UiFactory.CreateImage("Background", rowRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.12f));
        UiComponentCatalog.ApplyChrome(background, _theme.ChoicePanelSprite, new Color(1f, 1f, 1f, 0.12f));

        TMP_Text indexText = UiFactory.CreateText(
            "Index",
            rowRoot,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(74f, 22f),
            new Vector2(16f, 0f),
            _theme.DisplayFont,
            UiFontSize.Body,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.Brass);

        TMP_Text nameText = UiFactory.CreateText(
            "Name",
            rowRoot,
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(-220f, 24f),
            new Vector2(90f, 0f),
            _theme.BodyFont,
            UiFontSize.Section,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        TMP_Text categoryText = UiFactory.CreateText(
            "Category",
            rowRoot,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(140f, 22f),
            new Vector2(-16f, 0f),
            _theme.BodyFont,
            UiFontSize.Label,
            FontStyle.Normal,
            TextAnchor.MiddleRight,
            _theme.SecondaryText);

        return new RowWidgets
        {
            Root = rowRoot,
            Background = background,
            NameText = nameText,
            CategoryText = categoryText,
            IndexText = indexText
        };
    }

    private RectTransform CreateCard(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition)
    {
        RectTransform root = UiFactory.CreateRect(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
        RectTransform inner = UiComponentCatalog.BuildPanelShell(
            root,
            new Color(0.04f, 0.05f, 0.07f, 0.92f),
            _theme.ChoicePanelSprite,
            new Color(0.09f, 0.11f, 0.15f, 0.95f),
            _theme.InteractionPromptSprite,
            new Vector2(-10f, -10f));
        UiComponentCatalog.CreateAccentLine("Accent", inner, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, -24f), Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.84f));
        return root;
    }

    private RectTransform CreateInsetCard(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition)
    {
        RectTransform root = UiFactory.CreateRect(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
        RectTransform inner = UiComponentCatalog.BuildPanelShell(
            root,
            new Color(1f, 1f, 1f, 0.08f),
            _theme.InteractionPromptSprite,
            new Color(1f, 1f, 1f, 0.08f),
            _theme.InteractionPromptSprite,
            Vector2.zero);
        UiComponentCatalog.CreateAccentLine("TopLine", inner, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-24f, 1.5f), new Vector2(0f, -1f), new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.42f));
        return root;
    }

    private TMP_Text CreateSectionLabel(Transform parent, string content)
    {
        TMP_Text text = UiFactory.CreateText(
            content + "Label",
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-36f, 24f),
            new Vector2(18f, -20f),
            _theme.DisplayFont,
            13,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.Brass);
        text.text = content;
        return text;
    }

    private TMP_Text CreateInfoLine(Transform parent, string name, float yPosition)
    {
        return UiFactory.CreateText(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-28f, 30f),
            new Vector2(14f, yPosition),
            _theme.BodyFont,
            17,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);
    }

}
