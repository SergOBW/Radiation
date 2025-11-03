#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterStateController))]
public sealed class CharacterStateControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ctrl = (CharacterStateController)target;
        var bot = ctrl.GetComponent<BotController>();

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Talking ON")) ctrl.StartTalking();
        if (GUILayout.Button("Talking OFF")) ctrl.StopTalking();
        if (GUILayout.Button("Talking TOGGLE")) ctrl.ToggleTalking();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Idle"))
        {
            ctrl.SetTalking(false);
            if (bot != null) bot.DebugSetMoving(false);
        }
        if (GUILayout.Button("Set Walking"))
        {
            ctrl.SetTalking(false);
            if (bot != null) bot.DebugSetMoving(true);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2);
        EditorGUILayout.HelpBox("Idle выключает Talking и движение. Walking включает движение и выключает Talking. Кнопки работают только в Play Mode.", MessageType.Info);
    }
}
#endif