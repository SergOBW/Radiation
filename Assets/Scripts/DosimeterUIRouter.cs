using UnityEngine;

[DisallowMultipleComponent]
public sealed class DosimeterUIRouter : MonoBehaviour
{
    [SerializeField] private DosimeterCore core;
    [SerializeField] private GameObject gammaUiRoot;
    [SerializeField] private GameObject betaUiRoot;

    private void OnEnable()
    {
        if (!core) return;
        core.SensorChanged += OnSensorChanged;
        core.ModeChanged += OnModeChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (!core) return;
        core.SensorChanged -= OnSensorChanged;
        core.ModeChanged -= OnModeChanged;
    }

    private void OnSensorChanged(SensorSlot _) => Refresh();
    private void OnModeChanged(DosimeterMode _) => Refresh();

    private void Refresh()
    {
        if (!core) return;
        var sensor = core.CurrentSensor;
        bool isBeta = sensor && (sensor.sensitivity & RadiationChannel.Beta) != 0;

        if (gammaUiRoot) gammaUiRoot.SetActive(!isBeta);
        if (betaUiRoot)  betaUiRoot.SetActive(isBeta);
    }
}
