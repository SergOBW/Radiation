using System;
using UnityEngine;
using VContainer;

public enum DosimeterMode { Search, Measurement }
public enum SensorSlot { A, B }

[DisallowMultipleComponent]
public sealed class DosimeterCore : MonoBehaviour
{
    [Header("Сенсоры (заполни оба в инспекторе)")]
    [SerializeField] private DosimeterSensor sensorA;
    [SerializeField] private DosimeterSensor sensorB;
    [SerializeField] private SensorSlot activeSlot = SensorSlot.A;

    [Header("Режим")]
    [SerializeField] private DosimeterMode mode = DosimeterMode.Search;

    [Header("Измерение")]
    [Tooltip("Процент погрешности для режима 'Измерение'")]
    [SerializeField] private float measurementErrorPercent = 10f; // ← 3.1%

    [Header("StateHub (сигналы)")]
    [Inject] private BoolStateHub _stateHub;
    [SerializeField] private string keyModeSearch = "Dosimeter.Mode.Search";
    [SerializeField] private string keyModeMeasurement = "Dosimeter.Mode.Measurement";
    [SerializeField] private string keySensorA = "Dosimeter.Sensor.A";
    [SerializeField] private string keySensorB = "Dosimeter.Sensor.B";

    public DosimeterMode Mode => mode;
    public SensorSlot ActiveSlot => activeSlot;
    public DosimeterSensor CurrentSensor => (activeSlot == SensorSlot.A) ? sensorA : sensorB;
    public float MeasurementErrorPercent => measurementErrorPercent;

    public float CurrentMicroSvPerHour { get; private set; }
    public float PeakMicroSvPerHour { get; private set; }

    public float LastMeanMicroSvPerHour { get; private set; }
    public float LastSigmaMicroSvPerHour { get; private set; }

    public event Action<DosimeterMode> ModeChanged;
    public event Action<SensorSlot> SensorChanged;
    public event Action<float, float> ValuesUpdated;
    public event Action<float, float> MeasurementValueUpdated;

    private void OnEnable()
    {
        PushModeToStateHub(mode);
        PushSensorToStateHub(activeSlot);
        SyncSensorsActiveState();
        LastMeanMicroSvPerHour = 0f;
        LastSigmaMicroSvPerHour = 0f;
    }

    public void SetMode(DosimeterMode newMode, bool resetValues = false)
    {
        if (mode == newMode && !resetValues) return;

        mode = newMode;
        if (resetValues) ResetAll();

        ModeChanged?.Invoke(mode);
        PushModeToStateHub(mode);
    }

    public void SetActiveSensor(SensorSlot slot)
    {
        if (activeSlot == slot) return;

        activeSlot = slot;
        ResetAll();
        SyncSensorsActiveState();

        SensorChanged?.Invoke(activeSlot);
        PushSensorToStateHub(activeSlot);
    }

    public void ToggleSensor()
    {
        SetActiveSensor(activeSlot == SensorSlot.A ? SensorSlot.B : SensorSlot.A);
    }

    public void ResetAll()
    {
        PeakMicroSvPerHour = 0f;
        CurrentMicroSvPerHour = 0f;
        LastMeanMicroSvPerHour = 0f;
        LastSigmaMicroSvPerHour = 0f;

        ValuesUpdated?.Invoke(CurrentMicroSvPerHour, PeakMicroSvPerHour);
        MeasurementValueUpdated?.Invoke(LastMeanMicroSvPerHour, LastSigmaMicroSvPerHour);
    }

    private void Update()
    {
        var s = CurrentSensor;
        float x = (s != null) ? Mathf.Max(0f, s.CurrentDoseRateMicroSvPerHour) : 0f;
        CurrentMicroSvPerHour = x;

        if (mode == DosimeterMode.Search)
        {
            if (x > PeakMicroSvPerHour) PeakMicroSvPerHour = x;
            ValuesUpdated?.Invoke(CurrentMicroSvPerHour, PeakMicroSvPerHour);
        }
        else
        {
            LastMeanMicroSvPerHour = x;
            LastSigmaMicroSvPerHour = x * Mathf.Abs(measurementErrorPercent) * 0.01f;
            MeasurementValueUpdated?.Invoke(LastMeanMicroSvPerHour, LastSigmaMicroSvPerHour);
        }
    }

    private void SyncSensorsActiveState()
    {
        if (activeSlot == SensorSlot.A)
        {
            ApplySensorState(sensorA, true);
            ApplySensorState(sensorB, false);
        }
        else
        {
            ApplySensorState(sensorA, false);
            ApplySensorState(sensorB, true);
        }
    }

    private static void ApplySensorState(DosimeterSensor s, bool active)
    {
        if (s == null) return;
        s.IsWorking = active;
        var go = s.gameObject;
        if (go && go.activeSelf != active) go.SetActive(active);
    }

    private void PushModeToStateHub(DosimeterMode m)
    {
        if (_stateHub == null) return;

        try
        {
            if (m == DosimeterMode.Search)
            {
                _stateHub.SetTrue(keyModeSearch);
                _stateHub.SetFalse(keyModeMeasurement);
            }
            else
            {
                _stateHub.SetTrue(keyModeMeasurement);
                _stateHub.SetFalse(keyModeSearch);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DosimeterCore] StateHub mode error: {e.Message}");
        }
    }

    private void PushSensorToStateHub(SensorSlot s)
    {
        if (_stateHub == null) return;

        try
        {
            if (s == SensorSlot.A)
            {
                _stateHub.SetTrue(keySensorA);
                _stateHub.SetFalse(keySensorB);
            }
            else
            {
                _stateHub.SetTrue(keySensorB);
                _stateHub.SetFalse(keySensorA);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DosimeterCore] StateHub sensor error: {e.Message}");
        }
    }
}
