using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public sealed class GammaUI : MonoBehaviour
{
    [Header("Связь с ядром")]
    [SerializeField] private DosimeterCore core;

    [Header("TMP (настрой шрифты/размеры в инспекторе)")]
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private TMP_Text valueBig;
    [SerializeField] private TMP_Text auxText;

    [Header("Формат")]
    [SerializeField, Range(0,6)] private int decimals = 2;
    [SerializeField] private string unitRus = "мкЗв/ч";

    private void OnEnable()
    {
        if (!core) return;

        core.ModeChanged += OnModeChanged;
        core.ValuesUpdated += OnValuesUpdated;
        core.MeasurementValueUpdated += OnMeasurementUpdated;

        ForceRefresh();
    }

    private void OnDisable()
    {
        if (!core) return;

        core.ModeChanged -= OnModeChanged;
        core.ValuesUpdated -= OnValuesUpdated;
        core.MeasurementValueUpdated -= OnMeasurementUpdated;
    }

    private void OnModeChanged(DosimeterMode m)
    {
        if (modeText)
            modeText.text = m == DosimeterMode.Search ? "Режим: ПОИСК" : "Режим: ИЗМЕРЕНИЕ";

        ForceRefresh();
    }

    private void OnValuesUpdated(float current, float peak)
    {
        if (!core || core.Mode != DosimeterMode.Search) return;

        if (modeText)  modeText.text = "ПОИСК";
        if (valueBig)  valueBig.text = $"{peak.ToString("N"+decimals)} {unitRus}";
        if (auxText)   auxText.text  = $"Текущая: {current.ToString("N"+decimals)} {unitRus}";
    }

    private void OnMeasurementUpdated(float mean, float sigma )
    {
        if (!core || core.Mode != DosimeterMode.Measurement) return;

        if (modeText)  modeText.text = "ИЗМЕРЕНИЕ";
        if (valueBig)  valueBig.text = $"{mean.ToString("N"+decimals)} {unitRus}";
        if (auxText)   auxText.text  = $"Погрешность: {core.MeasurementErrorPercent:0.##}%";
    }

    private void ForceRefresh()
    {
        if (!core) return;

        if (core.Mode == DosimeterMode.Search)
        {
            OnValuesUpdated(core.CurrentMicroSvPerHour, core.PeakMicroSvPerHour);
        }
        else
        {
            OnMeasurementUpdated(core.LastMeanMicroSvPerHour, core.LastSigmaMicroSvPerHour);
        }
    }
}
