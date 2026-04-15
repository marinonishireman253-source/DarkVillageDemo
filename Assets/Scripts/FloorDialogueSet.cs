using UnityEngine;

[CreateAssetMenu(fileName = "FloorDialogueSet", menuName = "Ersarn/Floor Dialogue Set")]
public class FloorDialogueSet : ScriptableObject
{
    [SerializeField] private DialogueNode landingMonologueNode;
    [SerializeField] private DialogueNode ruleInscriptionNode;
    [SerializeField] private DialogueNode choiceEchoNode;

    public DialogueNode LandingMonologueNode => landingMonologueNode;
    public DialogueNode RuleInscriptionNode => ruleInscriptionNode;
    public DialogueNode ChoiceEchoNode => choiceEchoNode;
}
