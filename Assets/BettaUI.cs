using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BettaUI : MonoBehaviour
{
    [Header("Связь")]
    [SerializeField] private DosimeterCore core;

    [Header("TMP (настрой размеры шрифта в инспекторе)")]
    [SerializeField] private TMP_Text modeText;   // "Режим: ПОИСК (β)" / "Режим: ИЗМЕРЕНИЕ (β)"
    [SerializeField] private TMP_Text line1Big;   // крупное число
    [SerializeField] private TMP_Text line2Err;   // "±14%"
    [SerializeField] private TMP_Text line3Time;  // "17с"
    [SerializeField] private TMP_Text line4Unit;  // "мин⁻¹·см⁻²"

    [Header("Формат")]
    [SerializeField] private float decimals = 0;
    [SerializeField] private string units = "мин⁻¹·см⁻²";
    [Tooltip("Коэффициент пересчёта: (мин⁻¹·см⁻²) на 1 мкЗв/ч")]
    [SerializeField] private float fluxPerMicroSvPerHour = 1.0f; // подстрой под калибровку
    [Tooltip("Показывать фиксированное окно (без реального таймера)")]
    [SerializeField] private int displayWindowSec = 17;

    [Header("Мигание в режиме измерения")]
    [SerializeField] private bool blinkWhileMeasuring = true;
    [SerializeField] private float blinkPeriod = 0.5f;

    private float _blinkT;
    private bool  _blinkOn = true;

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

    private void Update()
    {
        if (!core) return;

        if (core.Mode == DosimeterMode.Measurement && blinkWhileMeasuring && line1Big)
        {
            _blinkT += Time.deltaTime;
            if (_blinkT >= blinkPeriod)
            {
                _blinkT = 0f;
                _blinkOn = !_blinkOn;
                line1Big.enabled = _blinkOn;
            }
        }
        else
        {
            if (line1Big) line1Big.enabled = true;
            _blinkT = 0f; _blinkOn = true;
        }
    }

    private void OnModeChanged(DosimeterMode _)
    {
        ForceRefresh();
    }

    // ==== ПОИСК (β): крупно — ПИК(β), мелко — текущая(β), плюс строки 3/4 ====
    private void OnValuesUpdated(float currentUSvph, float peakUSvph)
    {
        if (!core || core.Mode != DosimeterMode.Search) return;

        float curFlux  = ToFlux(currentUSvph);
        float peakFlux = ToFlux(peakUSvph);

        if (modeText)  modeText.text  = "ПОИСК (β)";
        if (line1Big)  line1Big.text  = peakFlux.ToString("N"+decimals);
        if (line2Err)  line2Err.text  = $"Текущая: {curFlux.ToString("N"+decimals)} {units}";
        if (line3Time) line3Time.text = $"{displayWindowSec}с";
        if (line4Unit) line4Unit.text = units;
    }

    // ==== ИЗМЕРЕНИЕ (β): крупно — текущая(β), мелко — ±X%, плюс строки 3/4 ====
    private void OnMeasurementUpdated(float meanUSvph, float _sigmaIgnored)
    {
        if (!core || core.Mode != DosimeterMode.Measurement) return;

        float meanFlux = ToFlux(meanUSvph);

        if (modeText)  modeText.text  = "Режим: ИЗМЕРЕНИЕ (β)";
        if (line1Big)  line1Big.text  = meanFlux.ToString("N"+decimals);
        if (line2Err)  line2Err.text  = $"±{core.MeasurementErrorPercent:0.##}%";
        if (line3Time) line3Time.text = $"{displayWindowSec}с";
        if (line4Unit) line4Unit.text = units;
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

    private float ToFlux(float microSvPerHour)
    {
        return Mathf.Max(0f, microSvPerHour) * Mathf.Max(0f, fluxPerMicroSvPerHour);
    }
}