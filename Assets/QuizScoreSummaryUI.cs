using System.Text;
using TMPro;
using UnityEngine;

public sealed class QuizSummaryView : MonoBehaviour
{
    [SerializeField] private GameObject summaryRoot;
    [SerializeField] private TMP_Text summaryText;

    private void Start()
    {
        TrySubscribe();
        Redraw();
    }

    private void OnDisable()
    {
        TryUnsubscribe();
    }

    private void TrySubscribe()
    {
        if (QuizScoreHandler.Instance == null) return;
        QuizScoreHandler.Instance.Changed += OnChanged;
    }

    private void TryUnsubscribe()
    {
        if (QuizScoreHandler.Instance == null) return;
        QuizScoreHandler.Instance.Changed -= OnChanged;
    }

    private void OnChanged()
    {
        Redraw();
    }

    private void Redraw()
    {
        if (QuizScoreHandler.Instance == null) return;
        if (summaryRoot == null || summaryText == null) return;

        bool ready = QuizScoreHandler.Instance.IsAllRegisteredCompleted();
        if (!ready)
        {
            summaryRoot.SetActive(false);
            return;
        }

        var list = QuizScoreHandler.Instance.GetRegisteredSnapshot();
        var sb = new StringBuilder();
        sb.AppendLine("<b>Результаты квизов</b>");
        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            sb.AppendLine("• " + s.Title + ": " + s.Correct + "/" + s.TotalQuestions);
        }
        int totalCorrect = QuizScoreHandler.Instance.GetTotalCorrect();
        int totalQuestions = QuizScoreHandler.Instance.GetTotalQuestions();
        float overall = QuizScoreHandler.Instance.GetOverallPercent();
        sb.AppendLine();
        sb.AppendLine("ИТОГО: " + totalCorrect + "/" + totalQuestions + " (" + overall.ToString("0.#") + "%)");

        summaryText.text = sb.ToString();
        summaryRoot.SetActive(true);
    }
}