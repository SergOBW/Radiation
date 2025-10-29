using UnityEngine;
using System.Collections.Generic;
using VContainer;

public class SelectedPointsSignalEmitter : MonoBehaviour
{
    [SerializeField]
    private RadiationSurveyManager surveyManager;

    [SerializeField]
    private List<string> pointsToWatch = new List<string>();

    [SerializeField]
    private string finalSignalName = "RadiationSurvey.AllPointsCompleted";

    [Inject] private SceneSignalHub _sceneSignalHub;
    [Inject] private ScenarioSignalHub _scenarioSignalHub;

    // Множество уже завершенных точек
    private HashSet<string> completedPoints = new HashSet<string>();

    private void OnEnable()
    {
        if (surveyManager != null)
        {
            surveyManager.PointCompleted += OnPointCompleted;
        }
    }

    private void OnDisable()
    {
        if (surveyManager != null)
        {
            surveyManager.PointCompleted -= OnPointCompleted;
        }
    }

    private void OnPointCompleted(string pointName)
    {
        if (!pointsToWatch.Contains(pointName)) return;

        completedPoints.Add(pointName);

        if (completedPoints.Count == pointsToWatch.Count)
        {
            bool haveSceneHub = _sceneSignalHub != null;
            bool haveScenarioHub = _scenarioSignalHub != null;

            if (!haveSceneHub && !haveScenarioHub)
            {
                Debug.LogWarning("[SelectedPointsSignalEmitter] No signal hubs injected.");
                return;
            }

            if (haveSceneHub) _sceneSignalHub.EmitAll(finalSignalName);
            if (haveScenarioHub) _scenarioSignalHub.Emit(finalSignalName);

            Debug.Log($"Signal emitted: {finalSignalName}");
        }
    }
}