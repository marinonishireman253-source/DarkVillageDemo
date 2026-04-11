using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueChoiceUI : MonoBehaviour
{
    private List<DialogueChoice> _currentChoices = new List<DialogueChoice>();
    private int _selectedIndex = -1;
    private bool _isVisible;

    private void OnEnable()
    {
        if (DialogueRunner.Instance != null)
        {
            DialogueRunner.Instance.OnNodeStarted += HandleNodeStarted;
            DialogueRunner.Instance.OnDialogueEnded += HandleDialogueEnded;
        }
    }

    private void OnDisable()
    {
        if (DialogueRunner.Instance != null)
        {
            DialogueRunner.Instance.OnNodeStarted -= HandleNodeStarted;
            DialogueRunner.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }

        _isVisible = false;
        _currentChoices.Clear();
        _selectedIndex = -1;
        SyncCanvasView();
    }

    private void HandleNodeStarted(DialogueNode node)
    {
        if (node != null && node.HasChoices)
        {
            _currentChoices = new List<DialogueChoice>(node.Choices);
            _selectedIndex = 0;
            _isVisible = true;
            SyncCanvasView();
        }
        else
        {
            _isVisible = false;
            _currentChoices.Clear();
            SyncCanvasView();
        }
    }

    private void HandleDialogueEnded()
    {
        _isVisible = false;
        _currentChoices.Clear();
        _selectedIndex = -1;
        SyncCanvasView();
    }

    private void Update()
    {
        if (!_isVisible || _currentChoices.Count == 0)
        {
            return;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            {
                _selectedIndex = (_selectedIndex - 1 + _currentChoices.Count) % _currentChoices.Count;
                SyncCanvasView();
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                _selectedIndex = (_selectedIndex + 1) % _currentChoices.Count;
                SyncCanvasView();
            }
            else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ConfirmChoice();
            }
        }
    }

    private void ConfirmChoice()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _currentChoices.Count)
        {
            return;
        }

        DialogueRunner.Instance?.SelectChoice(_selectedIndex);
    }

    private void SyncCanvasView()
    {
        if (!UiBootstrap.TryGetDialogueView(out DialogueCanvasView dialogueView))
        {
            return;
        }

        if (!_isVisible || _currentChoices.Count == 0)
        {
            dialogueView.HideChoices();
            return;
        }

        dialogueView.ShowChoices(_currentChoices, _selectedIndex);
    }
}
