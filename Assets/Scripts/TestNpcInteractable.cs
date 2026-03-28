using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TestNpcInteractable : InteractableBase
{
    [System.Serializable]
    private struct DialogueLine
    {
        [TextArea(2, 4)]
        public string text;
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueLine[] dialogueLines =
    {
        new DialogueLine
        {
            text = "夜色压下来以后，村口就没有那么好认了。"
        },
        new DialogueLine
        {
            text = "要是你准备继续往前走，记得沿着灯火最弱的那条路。"
        }
    };

    [Header("Focus Feedback")]
    [SerializeField] private Renderer highlightRenderer;
    [SerializeField] private Color idleColor = new Color(0.78f, 0.8f, 0.82f);
    [SerializeField] private Color focusColor = new Color(0.97f, 0.83f, 0.58f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public DialogueData DialogueData => dialogueData;
    public override string DisplayName => dialogueData != null && !string.IsNullOrWhiteSpace(dialogueData.SpeakerName)
        ? dialogueData.SpeakerName
        : base.DisplayName;

    private MaterialPropertyBlock _propertyBlock;
    private bool _supportsBaseColor;
    private bool _supportsColor;

    private void Reset()
    {
        ApplyPromptDefaults();

        if (highlightRenderer == null)
        {
            highlightRenderer = GetComponentInChildren<Renderer>();
        }

        EnsureCollider();
    }

    private void Awake()
    {
        ApplyPromptDefaults();

        if (highlightRenderer == null)
        {
            highlightRenderer = GetComponentInChildren<Renderer>();
        }

        EnsureCollider();
        CacheColorSupport();
        ApplyColor(idleColor);
    }

    public override void Interact(PlayerMover player)
    {
        if (SimpleDialogueUI.Instance == null || SimpleDialogueUI.IsOpen)
        {
            return;
        }

        if (TryGetComponent(out QuestObjectiveTarget objectiveTarget))
        {
            objectiveTarget.NotifyInteracted();
        }

        if (dialogueData != null)
        {
            SimpleDialogueUI.Instance.Show(dialogueData.SpeakerName, dialogueData.Lines);
            return;
        }

        SimpleDialogueUI.Instance.Show(DisplayName, EnumerateFallbackDialogueLines());
    }

    public override void OnFocusGained(PlayerMover player)
    {
        ApplyColor(focusColor);
    }

    public override void OnFocusLost(PlayerMover player)
    {
        ApplyColor(idleColor);
    }

    private void ApplyPromptDefaults()
    {
        if (dialogueData != null)
        {
            promptText = dialogueData.PromptText;

            if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
            {
                displayName = dialogueData.SpeakerName;
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(promptText) || promptText == "Interact")
        {
            promptText = "交谈";
        }

        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = "守夜人";
        }
    }

    private IEnumerable<string> EnumerateFallbackDialogueLines()
    {
        bool hasLine = false;

        foreach (DialogueLine line in dialogueLines)
        {
            if (string.IsNullOrWhiteSpace(line.text))
            {
                continue;
            }

            hasLine = true;
            yield return line.text.Trim();
        }

        if (!hasLine)
        {
            yield return "...";
        }
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider>() != null)
        {
            return;
        }

        CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        capsuleCollider.center = new Vector3(0f, 1f, 0f);
        capsuleCollider.height = 2f;
        capsuleCollider.radius = 0.35f;
    }

    private void CacheColorSupport()
    {
        if (highlightRenderer == null || highlightRenderer.sharedMaterial == null)
        {
            return;
        }

        _propertyBlock = new MaterialPropertyBlock();
        _supportsBaseColor = highlightRenderer.sharedMaterial.HasProperty(BaseColorId);
        _supportsColor = highlightRenderer.sharedMaterial.HasProperty(ColorId);
    }

    private void ApplyColor(Color color)
    {
        if (highlightRenderer == null || (!_supportsBaseColor && !_supportsColor))
        {
            return;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        _propertyBlock.Clear();

        if (_supportsBaseColor)
        {
            _propertyBlock.SetColor(BaseColorId, color);
        }

        if (_supportsColor)
        {
            _propertyBlock.SetColor(ColorId, color);
        }

        highlightRenderer.SetPropertyBlock(_propertyBlock);
    }
}
