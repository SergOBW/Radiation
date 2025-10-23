using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using VContainer;

public sealed class SignalDebugEmitter : MonoBehaviour
{
    [SerializeField] private string signal = "RadiometerAssembled";

    [Tooltip("Если включено — Emit вызывается при старте Play Mode (для автопроверки).")]
    [SerializeField] private bool emitOnStart = false;

    [Inject] private SceneSignalHub _sceneSignalHub;
    [Inject] private ScenarioSignalHub _scenarioSignalHub;
    private void Start()
    {
        if (emitOnStart)
            EmitSignal();
    }

    public void EmitSignal()
    {
        if (_sceneSignalHub == null)
        {
            Debug.LogWarning("[SignalDebugEmitter] SignalHub не внедрён! Используй DI или SetSignalHub().");
            return;
        }

        _sceneSignalHub.EmitAll(signal);
        _scenarioSignalHub.Emit(signal);
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(SignalDebugEmitter))]
public class SignalDebugEmitterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var emitter = (SignalDebugEmitter)target;
        GUILayout.Space(10);

        if (Application.isPlaying)
        {
            if (GUILayout.Button("📡 Emit Signal Now", GUILayout.Height(32)))
            {
                emitter.EmitSignal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Кнопка работает только в Play Mode.", MessageType.Info);
        }
    }
}
#endif