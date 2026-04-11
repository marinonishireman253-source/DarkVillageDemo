using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1500)]
public sealed class UiPreviewController : MonoBehaviour
{
    private enum PreviewPreset
    {
        Exploration = 0,
        Dialogue = 1,
        Combat = 2,
        Modal = 3,
        FullStack = 4
    }

    private static readonly IReadOnlyList<DialogueChoice> PreviewChoices = new[]
    {
        DialogueChoice.CreatePreview("推开祭台后的暗门。", "open-door"),
        DialogueChoice.CreatePreview("询问墙上的熏黑圣像。", "inspect-icon"),
        DialogueChoice.CreatePreview("转身离开这间屋子。", "leave-room")
    };

    [SerializeField] private bool startInPreviewMode;
    [SerializeField] private PreviewPreset defaultPreset = PreviewPreset.Exploration;
    [SerializeField] private Vector2 previewMarkerPosition = new Vector2(260f, 128f);

    private PreviewPreset _currentPreset;
    private bool _isPreviewActive;
    private Texture2D _portraitTexture;

    private void Start()
    {
        _currentPreset = defaultPreset;
        if (startInPreviewMode)
        {
            _isPreviewActive = true;
            Debug.Log("[UiPreview] 预览模式已开启。按 ` 切换预览，1 探索，2 对话，3 战斗，4 模态，5 全套叠加，0 或 Esc 退出。");
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        HandleHotkeys();

        if (!_isPreviewActive)
        {
            return;
        }

        ApplyCurrentPreset();
    }

    private void HandleHotkeys()
    {
        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            _isPreviewActive = !_isPreviewActive;
            if (_isPreviewActive)
            {
                Debug.Log("[UiPreview] 预览模式开启。1 探索，2 对话，3 战斗，4 模态，5 全套叠加，0 或 Esc 退出。");
            }
            else
            {
                ClearPreview();
                Debug.Log("[UiPreview] 预览模式关闭。");
            }
        }

        if (!_isPreviewActive)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            _isPreviewActive = false;
            ClearPreview();
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            _currentPreset = PreviewPreset.Exploration;
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            _currentPreset = PreviewPreset.Dialogue;
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            _currentPreset = PreviewPreset.Combat;
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            _currentPreset = PreviewPreset.Modal;
        }
        else if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            _currentPreset = PreviewPreset.FullStack;
        }
    }

    private void ApplyCurrentPreset()
    {
        if (!UiBootstrap.TryGetHudView(out HudCanvasView hudView)
            || !UiBootstrap.TryGetDialogueView(out DialogueCanvasView dialogueView)
            || !UiBootstrap.TryGetModalView(out ModalCanvasView modalView))
        {
            return;
        }

        switch (_currentPreset)
        {
            case PreviewPreset.Exploration:
                ApplyExplorationPreview(hudView, dialogueView, modalView);
                break;
            case PreviewPreset.Dialogue:
                ApplyDialoguePreview(hudView, dialogueView, modalView, false);
                break;
            case PreviewPreset.Combat:
                ApplyCombatPreview(hudView, dialogueView, modalView);
                break;
            case PreviewPreset.Modal:
                ApplyModalPreview(hudView, dialogueView, modalView);
                break;
            case PreviewPreset.FullStack:
                ApplyDialoguePreview(hudView, dialogueView, modalView, true);
                break;
        }
    }

    private void ApplyExplorationPreview(HudCanvasView hudView, DialogueCanvasView dialogueView, ModalCanvasView modalView)
    {
        dialogueView.HideDialogue();
        dialogueView.HideChoices();
        dialogueView.HidePortrait();
        modalView.HideModal();

        hudView.SetQuestPanel(true, "当前目标", "前往旧木屋二层，调查被钉死的祈祷室。");
        hudView.SetInteractionPrompt(true, "破损供桌", "查看供桌上的烛泪与灰痕", "E");
        hudView.SetWorldMarkerPreview(true, "祈祷室", previewMarkerPosition);
        hudView.HideCompletionBanner();
        hudView.HideCombatPanel();
    }

    private void ApplyDialoguePreview(HudCanvasView hudView, DialogueCanvasView dialogueView, ModalCanvasView modalView, bool keepHud)
    {
        modalView.HideModal();
        if (keepHud)
        {
            ApplyOverlayHudPreview(hudView);
        }
        else
        {
            hudView.HideQuestPanel();
            hudView.HideInteractionPrompt();
            hudView.HideWorldMarker();
            hudView.HideCompletionBanner();
            hudView.HideCombatPanel();
        }

        dialogueView.ShowDialogue(
            "艾格尼丝修女",
            "蜡泪已经沿着黄铜圣像的背面凝住了。别出声，听门板后面那阵像祈祷一样的抓挠。",
            "数字键切预览    ` 关闭预览");
        dialogueView.ShowPortrait(GetPreviewPortrait(), "艾格尼丝修女", new Color(0.13f, 0.14f, 0.17f, 0.96f));
        dialogueView.ShowChoices(PreviewChoices, 1);
    }

    private void ApplyCombatPreview(HudCanvasView hudView, DialogueCanvasView dialogueView, ModalCanvasView modalView)
    {
        dialogueView.HideDialogue();
        dialogueView.HideChoices();
        dialogueView.HidePortrait();
        modalView.HideModal();

        hudView.HideQuestPanel();
        hudView.HideInteractionPrompt();
        hudView.HideCompletionBanner();
        hudView.SetWorldMarkerPreview(true, "邪影", new Vector2(340f, 154f));
        hudView.SetCombatPanel(
            true,
            "伊尔萨恩  HP 17/24",
            "攻击键: Space / J / 鼠标左键",
            "战斗: 梁柱间的访客",
            "束光访客  HP 31/40",
            true);
    }

    private void ApplyModalPreview(HudCanvasView hudView, DialogueCanvasView dialogueView, ModalCanvasView modalView)
    {
        dialogueView.HideDialogue();
        dialogueView.HideChoices();
        dialogueView.HidePortrait();

        hudView.HideQuestPanel();
        hudView.HideInteractionPrompt();
        hudView.HideWorldMarker();
        hudView.HideCompletionBanner();
        hudView.HideCombatPanel();

        modalView.ShowChapterComplete(
            "祈祷室已开启",
            "你从发黑的门框后取下了黄铜铭牌。它并不温暖，却像刚从掌心里拿出来一样带着微弱余温。",
            "1 探索  2 对话  3 战斗  5 叠加预览",
            "收下铭牌",
            "离开此处",
            null,
            null);
    }

    private void ApplyOverlayHudPreview(HudCanvasView hudView)
    {
        hudView.SetQuestPanel(true, "当前目标", "记录屋内供桌与祈祷室的纹样差异。");
        hudView.SetInteractionPrompt(true, "侧门木栓", "查看门栓上的灰烬与指痕", "E");
        hudView.SetWorldMarkerPreview(true, "木栓", new Vector2(310f, 140f));
        hudView.HideCompletionBanner();
        hudView.HideCombatPanel();
    }

    private void ClearPreview()
    {
        if (UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            hudView.HideQuestPanel();
            hudView.HideInteractionPrompt();
            hudView.HideWorldMarker();
            hudView.HideCompletionBanner();
            hudView.HideCombatPanel();
        }

        if (UiBootstrap.TryGetDialogueView(out DialogueCanvasView dialogueView))
        {
            dialogueView.HideDialogue();
            dialogueView.HideChoices();
            dialogueView.HidePortrait();
        }

        if (UiBootstrap.TryGetModalView(out ModalCanvasView modalView))
        {
            modalView.HideModal();
        }
    }

    private Texture2D GetPreviewPortrait()
    {
        if (_portraitTexture != null)
        {
            return _portraitTexture;
        }

        _portraitTexture = Resources.Load<Texture2D>("Characters/Sagiri/front_panel_debug");
        if (_portraitTexture == null)
        {
            _portraitTexture = Resources.Load<Texture2D>("Characters/Sagiri/main_figure_debug");
        }
        if (_portraitTexture == null)
        {
            _portraitTexture = Resources.Load<Texture2D>("Characters/Sagiri/sagiri_front");
        }

        return _portraitTexture;
    }
}
