using System.Text;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class NotebookUIRenderer : MonoBehaviour
{
    public Notebook notebook;
    public RadiationSurveyManager survey;
    public TMP_Text text;

    [Header("Формат")]
    [Tooltip("Максимум символов в имени зоны (дальше будет …)")]
    public int maxNameChars = 20;

    [Tooltip("Знаков после запятой у значения")]
    public int decimals = 2;

    [Tooltip("Ширина колонок (символов) для имени / статуса / значения")]
    public int colName = 22, colMark = 2, colValue = 10;

    [Tooltip("Ширина моношага для <mspace>; 0.55–0.6em обычно ок")]
    public string monoEm = "0.56em";

    [Header("Символы статуса")]
    public string checkMark = "✓";
    public string crossMark  = "✗";

    private void OnEnable()
    {
        if (notebook != null) notebook.Updated += Refresh;
        if (survey   != null) survey.PointsListChanged += Refresh;

        if (text != null)
        {
            text.enableWordWrapping = false;
            text.richText = true;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (notebook != null) notebook.Updated -= Refresh;
        if (survey   != null) survey.PointsListChanged -= Refresh;
    }

    public void Refresh()
    {
        if (text == null || survey == null || notebook == null) return;

        var sb = new StringBuilder(512);
        sb.Append("<mspace=").Append(monoEm).Append('>');

        foreach (var z in survey.zones)
        {
            if (z == null) continue;

            // имя (обрезка + паддинг)
            string name = string.IsNullOrEmpty(z.pointName) ? "Без имени" : z.pointName.Trim();
            if (name.Length > maxNameChars)
                name = name.Substring(0, Mathf.Max(0, maxNameChars - 1)) + "…";
            name = PadRight(name, colName);

            bool done = notebook.IsCompleted(z.pointName);
            string mark = PadRight(done ? checkMark : crossMark, colMark);

            string val = "—";
            if (done && notebook.TryGetValue(z.pointName, out float v))
                val = v.ToString("N" + Mathf.Clamp(decimals, 0, 6)) + " мкЗв/ч";
            val = PadRight(val, colValue);

            sb.Append(name).Append(mark).Append(val).Append('\n');
        }

        sb.Append("</mspace>");
        text.text = sb.ToString();
    }

    private static string PadRight(string s, int width)
    {
        if (s == null) s = "";
        if (s.Length >= width) return s;
        return s + new string(' ', width - s.Length);
    }
}
