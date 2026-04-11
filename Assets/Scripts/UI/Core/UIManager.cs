using System.Collections;
using UnityEngine;

public sealed class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Views References")]
    [SerializeField] private DialogueCanvasView dialogue;
    [SerializeField] private HudCanvasView hud;
    [SerializeField] private ModalCanvasView modal;
    [SerializeField] private float uiFadeDuration = 0.35f;

    public DialogueCanvasView Dialogue => dialogue;
    public HudCanvasView HUD => hud;
    public ModalCanvasView Modal => modal;

    private Coroutine _hudFadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshBindings();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void BindViews(DialogueCanvasView dialogueView, HudCanvasView hudView, ModalCanvasView modalView)
    {
        dialogue = dialogueView != null ? dialogueView : dialogue;
        hud = hudView != null ? hudView : hud;
        modal = modalView != null ? modalView : modal;
    }

    public void RefreshBindings()
    {
        if (dialogue == null && UiBootstrap.TryGetDialogueView(out DialogueCanvasView dialogueView))
        {
            dialogue = dialogueView;
        }

        if (hud == null && UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            hud = hudView;
        }

        if (modal == null && UiBootstrap.TryGetModalView(out ModalCanvasView modalView))
        {
            modal = modalView;
        }
    }

    public void EnterCinematicMode()
    {
        RefreshBindings();
        if (hud != null)
        {
            FadeView(hud.gameObject, false);
        }
    }

    public void ExitCinematicMode()
    {
        RefreshBindings();
        if (hud != null)
        {
            FadeView(hud.gameObject, true);
        }

        if (dialogue != null)
        {
            dialogue.HideDialogue();
            dialogue.HideChoices();
        }
    }

    public void TriggerDangerState()
    {
        RefreshBindings();
        hud?.SetCombatAlertState(true);
    }

    public void ClearDangerState()
    {
        RefreshBindings();
        hud?.SetCombatAlertState(false);
    }

    private void FadeView(GameObject target, bool visible)
    {
        if (target == null)
        {
            return;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        if (_hudFadeRoutine != null)
        {
            StopCoroutine(_hudFadeRoutine);
        }

        if (visible)
        {
            target.SetActive(true);
        }

        _hudFadeRoutine = StartCoroutine(FadeCanvasGroup(canvasGroup, visible));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.05f, uiFadeDuration);
        float start = canvasGroup.alpha;
        float target = visible ? 1f : 0f;
        float elapsed = 0f;

        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        canvasGroup.alpha = target;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        if (!visible)
        {
            canvasGroup.gameObject.SetActive(false);
        }

        _hudFadeRoutine = null;
    }
}
