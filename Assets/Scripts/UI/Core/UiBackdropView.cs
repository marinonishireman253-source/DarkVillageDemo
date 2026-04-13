using UnityEngine;
using UnityEngine.UI;

public sealed class UiBackdropView : MonoBehaviour
{
    private UiTheme _theme;
    private UiStateCoordinator _stateCoordinator;

    private Image _topBand;
    private Image _bottomMist;
    private Image _leftVeil;
    private Image _rightVeil;
    private Image _dialogueGlow;
    private Image _modalDimmer;

    private float _targetTopAlpha;
    private float _targetBottomAlpha;
    private float _targetSideAlpha;
    private float _targetDialogueGlowAlpha;
    private float _targetModalAlpha;

    public void Initialize(UiTheme theme, UiStateCoordinator stateCoordinator)
    {
        if (_theme != null)
        {
            return;
        }

        _theme = theme != null ? theme : UiTheme.CreateRuntimeDefault();
        _theme.EnsureRuntimeDefaults();
        _stateCoordinator = stateCoordinator;

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            UiFactory.Stretch(root);
        }

        BuildLayout();
        BindState();
        ApplyTargetsImmediate();
    }

    private void OnDestroy()
    {
        if (_stateCoordinator != null)
        {
            _stateCoordinator.OnModeChanged -= HandleModeChanged;
        }
    }

    private void Update()
    {
        FadeImage(_topBand, _targetTopAlpha);
        FadeImage(_bottomMist, _targetBottomAlpha);
        FadeImage(_leftVeil, _targetSideAlpha);
        FadeImage(_rightVeil, _targetSideAlpha);
        FadeImage(_dialogueGlow, _targetDialogueGlowAlpha);
        FadeImage(_modalDimmer, _targetModalAlpha);
    }

    private void BuildLayout()
    {
        _modalDimmer = CreateBackdropImage("ModalDimmer", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 0f);
        _topBand = CreateBackdropImage("TopBand", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 220f), Vector2.zero, 0.06f);
        _bottomMist = CreateBackdropImage("BottomMist", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 280f), Vector2.zero, 0.08f);
        _leftVeil = CreateBackdropImage("LeftVeil", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(160f, 0f), Vector2.zero, 0.04f);
        _rightVeil = CreateBackdropImage("RightVeil", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(160f, 0f), Vector2.zero, 0.04f);
        _dialogueGlow = CreateBackdropImage("DialogueGlow", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 430f), Vector2.zero, 0f);
    }

    private void BindState()
    {
        if (_stateCoordinator == null)
        {
            _stateCoordinator = UiStateCoordinator.Instance;
        }

        if (_stateCoordinator == null)
        {
            SetTargets(UiStateCoordinator.UiMode.Exploration);
            return;
        }

        _stateCoordinator.OnModeChanged += HandleModeChanged;
        SetTargets(_stateCoordinator.CurrentMode);
    }

    private void HandleModeChanged(UiStateCoordinator.UiMode mode)
    {
        SetTargets(mode);
    }

    private void SetTargets(UiStateCoordinator.UiMode mode)
    {
        switch (mode)
        {
            case UiStateCoordinator.UiMode.Dialogue:
                _targetTopAlpha = 0.14f;
                _targetBottomAlpha = 0.2f;
                _targetSideAlpha = 0.08f;
                _targetDialogueGlowAlpha = 0.18f;
                _targetModalAlpha = 0f;
                break;
            case UiStateCoordinator.UiMode.Combat:
                _targetTopAlpha = 0.1f;
                _targetBottomAlpha = 0.16f;
                _targetSideAlpha = 0.07f;
                _targetDialogueGlowAlpha = 0.05f;
                _targetModalAlpha = 0f;
                break;
            case UiStateCoordinator.UiMode.Inventory:
                _targetTopAlpha = 0.18f;
                _targetBottomAlpha = 0.22f;
                _targetSideAlpha = 0.1f;
                _targetDialogueGlowAlpha = 0.08f;
                _targetModalAlpha = 0.26f;
                break;
            case UiStateCoordinator.UiMode.ChapterComplete:
            case UiStateCoordinator.UiMode.Paused:
                _targetTopAlpha = 0.2f;
                _targetBottomAlpha = 0.26f;
                _targetSideAlpha = 0.12f;
                _targetDialogueGlowAlpha = 0.1f;
                _targetModalAlpha = 0.5f;
                break;
            case UiStateCoordinator.UiMode.InteractionFocus:
                _targetTopAlpha = 0.08f;
                _targetBottomAlpha = 0.12f;
                _targetSideAlpha = 0.05f;
                _targetDialogueGlowAlpha = 0.02f;
                _targetModalAlpha = 0f;
                break;
            default:
                _targetTopAlpha = 0.06f;
                _targetBottomAlpha = 0.08f;
                _targetSideAlpha = 0.04f;
                _targetDialogueGlowAlpha = 0f;
                _targetModalAlpha = 0f;
                break;
        }
    }

    private void ApplyTargetsImmediate()
    {
        SetImageAlpha(_topBand, _targetTopAlpha);
        SetImageAlpha(_bottomMist, _targetBottomAlpha);
        SetImageAlpha(_leftVeil, _targetSideAlpha);
        SetImageAlpha(_rightVeil, _targetSideAlpha);
        SetImageAlpha(_dialogueGlow, _targetDialogueGlowAlpha);
        SetImageAlpha(_modalDimmer, _targetModalAlpha);
    }

    private Image CreateBackdropImage(
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        float initialAlpha)
    {
        Image image = UiFactory.CreateImage(name, transform, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition, _theme.FogShadow);
        image.raycastTarget = false;
        SetImageAlpha(image, initialAlpha);
        return image;
    }

    private void FadeImage(Image image, float targetAlpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        float fadeRate = Mathf.Max(1f, 1f / _theme.FastFadeDuration);
        color.a = Mathf.MoveTowards(color.a, targetAlpha, fadeRate * Time.unscaledDeltaTime);
        image.color = color;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
