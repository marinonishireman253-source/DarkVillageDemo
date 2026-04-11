using UnityEngine;
using UnityEngine.UI;

public sealed class UiBackdropView : MonoBehaviour
{
    private UiTheme _theme;
    private UiStateCoordinator _stateCoordinator;

    private Image _topBand;
    private Image _bottomBand;
    private Image _leftBand;
    private Image _rightBand;
    private Image _dialogueHaze;
    private Image _modalDimmer;

    private float _targetTopAlpha;
    private float _targetBottomAlpha;
    private float _targetSideAlpha;
    private float _targetDialogueHazeAlpha;
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
        if (_topBand == null)
        {
            return;
        }

        FadeImage(_topBand, _targetTopAlpha);
        FadeImage(_bottomBand, _targetBottomAlpha);
        FadeImage(_leftBand, _targetSideAlpha);
        FadeImage(_rightBand, _targetSideAlpha);
        FadeImage(_dialogueHaze, _targetDialogueHazeAlpha);
        FadeImage(_modalDimmer, _targetModalAlpha);
    }

    private void BuildLayout()
    {
        _modalDimmer = CreateBackdropImage("ModalDimmer", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 0f);
        _topBand = CreateBackdropImage("TopBand", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 140f), Vector2.zero, 0.1f);
        _bottomBand = CreateBackdropImage("BottomBand", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 200f), Vector2.zero, 0.07f);
        _leftBand = CreateBackdropImage("LeftBand", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(84f, 0f), Vector2.zero, 0.04f);
        _rightBand = CreateBackdropImage("RightBand", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(84f, 0f), Vector2.zero, 0.04f);
        _dialogueHaze = CreateBackdropImage("DialogueHaze", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 360f), Vector2.zero, 0f);
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
                _targetTopAlpha = 0.17f;
                _targetBottomAlpha = 0.2f;
                _targetSideAlpha = 0.07f;
                _targetDialogueHazeAlpha = 0.14f;
                _targetModalAlpha = 0f;
                break;
            case UiStateCoordinator.UiMode.Combat:
                _targetTopAlpha = 0.13f;
                _targetBottomAlpha = 0.12f;
                _targetSideAlpha = 0.06f;
                _targetDialogueHazeAlpha = 0.05f;
                _targetModalAlpha = 0f;
                break;
            case UiStateCoordinator.UiMode.ChapterComplete:
                _targetTopAlpha = 0.2f;
                _targetBottomAlpha = 0.24f;
                _targetSideAlpha = 0.1f;
                _targetDialogueHazeAlpha = 0.12f;
                _targetModalAlpha = 0.46f;
                break;
            case UiStateCoordinator.UiMode.InteractionFocus:
                _targetTopAlpha = 0.1f;
                _targetBottomAlpha = 0.09f;
                _targetSideAlpha = 0.04f;
                _targetDialogueHazeAlpha = 0.02f;
                _targetModalAlpha = 0f;
                break;
            default:
                _targetTopAlpha = 0.1f;
                _targetBottomAlpha = 0.07f;
                _targetSideAlpha = 0.035f;
                _targetDialogueHazeAlpha = 0f;
                _targetModalAlpha = 0f;
                break;
        }
    }

    private void ApplyTargetsImmediate()
    {
        SetImageAlpha(_topBand, _targetTopAlpha);
        SetImageAlpha(_bottomBand, _targetBottomAlpha);
        SetImageAlpha(_leftBand, _targetSideAlpha);
        SetImageAlpha(_rightBand, _targetSideAlpha);
        SetImageAlpha(_dialogueHaze, _targetDialogueHazeAlpha);
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
