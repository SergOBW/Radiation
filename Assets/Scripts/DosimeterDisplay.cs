using TMPro;
using UnityEngine;
#if TMP_PRESENT
using TMPro;
#endif

public class DosimeterDisplay : MonoBehaviour
{
    [Header("Связи")]
    public DosimeterSensor sensor;

    [Header("UI (любой из вариантов)")]
    public TMP_Text tmpText;                 // для Canvas/World-Space
    public TMP_Text tmpSecondaryUnit;        // опционально (единицы)

    [Header("Формат")]
    public int decimals = 2;
    public bool autoScaleUnits = true; // µSv/h ↔ mSv/h

    private void LateUpdate()
    {
        if (sensor == null) return;
        float val = Mathf.Max(0f, sensor.CurrentDoseRateMicroSvPerHour);

        string unit = "µSv/h";
        float shown = val;

        if (autoScaleUnits && val >= 1000f)
        {
            unit = "mSv/h";
            shown = val / 1000f;
        }

        string txt = shown.ToString("N" + decimals) + " " + unit;

        if (tmpText) tmpText.text = txt;
        if (tmpSecondaryUnit) tmpSecondaryUnit.text = unit;
    }
}