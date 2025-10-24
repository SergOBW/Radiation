using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public sealed class DosimeterUI : MonoBehaviour
{
    [Header("Связь с ядром")]
    [SerializeField] private DosimeterCore core;

    [Header("TMP (настрой шрифты/размеры в инспекторе)")]
    [SerializeField] private TMP_Text modeText;  // маленький заголовок: "Режим: ..."
    [SerializeField] private TMP_Text valueBig;  // КРУПНОЕ число
    [SerializeField] private TMP_Text auxText;   // мелкая подпись под числом

    [Header("Формат")]
    [SerializeField, Range(0,6)] private int decimals = 2;
    [SerializeField] private string unitRus = "мкЗв/ч";

    private void OnEnable()
    {
        if (!core) return;

        core.ModeChanged += OnModeChanged;
        core.ValuesUpdated += OnValuesUpdated;                 // Поиск
        core.MeasurementValueUpdated += OnMeasurementUpdated;  // Измерение

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

        // моментально перерисуем текущие значения
        ForceRefresh();
    }

    // === Режим ПОИСК: КРУПНО — Пик; мелко — Текущая ===
    private void OnValuesUpdated(float current, float peak)
    {
        if (!core || core.Mode != DosimeterMode.Search) return;

        if (modeText)  modeText.text = "ПОИСК";
        if (valueBig)  valueBig.text = $"{peak.ToString("N"+decimals)} {unitRus}";
        if (auxText)   auxText.text  = $"Текущая: {current.ToString("N"+decimals)} {unitRus}";
    }

    // === Режим ИЗМЕРЕНИЕ: КРУПНО — Результат (текущая); мелко — Погрешность % ===
    private void OnMeasurementUpdated(float mean, float sigma /* не показываем, только % */)
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
