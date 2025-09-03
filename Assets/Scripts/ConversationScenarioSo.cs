using UnityEngine;

[CreateAssetMenu(menuName = "Conversation/Scenario")]
public sealed class ConversationScenarioSo : ScriptableObject
{
    public ConversationStepSo[] steps;
}