using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private QuizData quizData;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private List<Button> answerButtons;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonLabel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color correctColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color wrongColor = new Color(0.9f, 0.2f, 0.2f);

    private int _currentIndex = -1;
    private int _score = 0;
    private bool _questionAnswered = false;

    private void Awake()
    {
        // Навешиваем обработчики на кнопки ответов
        for (int i = 0; i < answerButtons.Count; i++)
        {
            int captured = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(captured));
        }
        nextButton.onClick.AddListener(OnNextClicked);

        InitQuiz();
    }

    private void InitQuiz()
    {
        _score = 0;
        _currentIndex = -1;
        resultPanel.SetActive(false);
        titleText.text = quizData ? quizData.quizTitle : "Тест";
        ShowNextQuestion();
    }

    private void ShowNextQuestion()
    {
        _currentIndex++;
        _questionAnswered = false;

        if (quizData == null || _currentIndex >= quizData.questions.Count)
        {
            ShowResult();
            return;
        }

        foreach (var btn in answerButtons)
        {
            btn.interactable = true;
            SetButtonColor(btn, normalColor);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "";
        }

        var q = quizData.questions[_currentIndex];
        questionText.text = q.questionText;

        for (int i = 0; i < answerButtons.Count; i++)
        {
            var label = answerButtons[i].GetComponentInChildren<TMP_Text>();
            if (!label) continue;

            if (i < q.answers.Count)
            {
                answerButtons[i].gameObject.SetActive(true);
                label.text = q.answers[i];
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        bool lastQuestion = (_currentIndex == quizData.questions.Count - 1);
        nextButtonLabel.text = lastQuestion ? "Завершить" : "Следующий вопрос";
        nextButton.interactable = false;
    }

    private void OnAnswerClicked(int index)
    {
        if (_questionAnswered) return;
        if (quizData == null || _currentIndex >= quizData.questions.Count) return;

        _questionAnswered = true;

        var q = quizData.questions[_currentIndex];
        bool isCorrect = (index == q.correctIndex);
        if (isCorrect) _score++;

        for (int i = 0; i < answerButtons.Count; i++)
        {
            if (!answerButtons[i].gameObject.activeInHierarchy) continue;

            bool btnIsCorrect = (i == q.correctIndex);
            SetButtonColor(answerButtons[i], btnIsCorrect ? correctColor : wrongColor);
            answerButtons[i].interactable = false;
        }

        nextButton.interactable = true;
    }

    private void OnNextClicked()
    {
        if (!_questionAnswered) return;
        ShowNextQuestion();
    }

    private void ShowResult()
    {
        resultPanel.SetActive(true);
        int total = quizData ? quizData.questions.Count : 0;
        resultText.text = $"Ваш результат: {_score} / {total}";
    }

    private void SetButtonColor(Button btn, Color c)
    {
        var target = btn.targetGraphic as Image;
        if (target != null)
        {
            target.color = c;
        }
        else
        {
            var img = btn.GetComponent<Image>();
            if (img) img.color = c;
        }
    }
}
