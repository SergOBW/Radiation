using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;
using VContainer;

public class QuizManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private QuizData quizData;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private List<Button> answerButtons;
    [SerializeField] private Button actionButton;               // бывш. nextButton
    [SerializeField] private TMP_Text actionButtonLabel;        // бывш. nextButtonLabel
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    [Header("Colors")]
    [SerializeField] private Color normalColor   = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.75f, 0.85f, 1.0f); // подсветка выбранного
    [SerializeField] private Color correctColor  = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color wrongColor    = new Color(0.9f, 0.2f, 0.2f);

    [Inject] private SceneSignalHub _sceneSignalHub;
    [Inject] private ScenarioSignalHub _scenarioSignalHub;

    [SerializeField]private string quizCompleteSignalName = "Quiz.AllQuestionsAnswered";
    [SerializeField]private  string correctAnswerSignalName = "Quiz.CorrectAnswer";
    [SerializeField]private  string wrongAnswerSignalName = "Quiz.WrongAnswer";

    private int _currentIndex = -1;
    private int _score = 0;

    // Новые состояния
    private int  _selectedIndex = -1; // какой вариант пользователь выбрал (до проверки)
    private bool _checked = false;    // проверили ли текущий вопрос (раскрасили ответы)

    private void Awake()
    {
        for (int i = 0; i < answerButtons.Count; i++)
        {
            int captured = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(captured));
        }
        actionButton.onClick.AddListener(OnActionClicked);

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
        _selectedIndex = -1;
        _checked = false;

        if (quizData == null || _currentIndex >= quizData.questions.Count)
        {
            ShowResult();
            return;
        }

        // Сброс кнопок ответов
        foreach (var btn in answerButtons)
        {
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            SetButtonColor(btn, normalColor);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "";
        }

        var q = quizData.questions[_currentIndex];
        questionText.text = q.questionText;

        // Проставляем тексты/скрываем лишние
        for (int i = 0; i < answerButtons.Count; i++)
        {
            var label = answerButtons[i].GetComponentInChildren<TMP_Text>();
            if (!label) continue;

            if (i < q.answers.Count)
            {
                answerButtons[i].gameObject.SetActive(true);
                label.text = q.answers[i];
                SetButtonColor(answerButtons[i], normalColor);
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        // На старте вопроса — кнопка в режиме "Ответить" и выключена до выбора
        actionButtonLabel.text = "Ответить";
        actionButton.interactable = false;
    }

    private void OnAnswerClicked(int index)
    {
        if (_checked) return; // если уже проверили — менять нельзя

        _selectedIndex = index;

        // Подсветим только выбранный, остальные вернём в normalColor
        for (int i = 0; i < answerButtons.Count; i++)
        {
            if (!answerButtons[i].gameObject.activeInHierarchy) continue;
            SetButtonColor(answerButtons[i], i == _selectedIndex ? selectedColor : normalColor);
        }

        // Разрешаем нажать "Ответить"
        actionButton.interactable = true;
        actionButtonLabel.text = "Ответить";
    }

    private void OnActionClicked()
    {
        if (quizData == null || _currentIndex >= quizData.questions.Count) return;

        var q = quizData.questions[_currentIndex];
        bool lastQuestion = (_currentIndex == quizData.questions.Count - 1);

        if (!_checked)
        {
            if (_selectedIndex < 0)
                return;

            bool isCorrect = (_selectedIndex == q.correctIndex);
            if (isCorrect) _score++;

            // Вызов сигналов с передачей индекса вопроса и выбранного индекса
            EmitAnswerSignal(isCorrect, _currentIndex);

            for (int i = 0; i < answerButtons.Count; i++)
            {
                if (!answerButtons[i].gameObject.activeInHierarchy) continue;

                bool btnIsCorrect = (i == q.correctIndex);
                if (btnIsCorrect)
                {
                    SetButtonColor(answerButtons[i], correctColor);
                }
                else
                {
                    if (i == _selectedIndex)
                        SetButtonColor(answerButtons[i], wrongColor);
                    else
                        SetButtonColor(answerButtons[i], normalColor);
                }

                answerButtons[i].interactable = false;
            }

            _checked = true;

            actionButtonLabel.text = lastQuestion ? "Завершить" : "Следующий вопрос";
            actionButton.interactable = true;
        }
        else
        {
            ShowNextQuestion();
        }
    }

    private void EmitAnswerSignal(bool isCorrect, int questionIndex)
    {
        // Тут можно формировать объект или строку с данными, если нужно
        var signalName = isCorrect ? correctAnswerSignalName : wrongAnswerSignalName;

        bool haveSceneHub = _sceneSignalHub != null;
        bool haveScenarioHub = _scenarioSignalHub != null;

        if (!haveSceneHub && !haveScenarioHub)
        {
            Debug.LogWarning("[QuizManager] No signal hubs injected.");
            return;
        }

        string signal = $"{signalName}:{questionIndex}";

        if (haveSceneHub) _sceneSignalHub.EmitAll(signal);
        if (haveScenarioHub) _scenarioSignalHub.Emit(signal);

        Debug.Log($"Signal emitted: {signalName} with payload: {signal}");
    }

    private void ShowResult()
    {
        resultPanel.SetActive(true);
        int total = quizData ? quizData.questions.Count : 0;
        resultText.text = $"Ваш результат: {_score} / {total}";
        actionButton.interactable = false;
        actionButtonLabel.text = "Готово";

        bool haveSceneHub = _sceneSignalHub != null;
        bool haveScenarioHub = _scenarioSignalHub != null;

        if (!haveSceneHub && !haveScenarioHub)
        {
            Debug.LogWarning("[QuizManager] No signal hubs injected.");
            return;
        }

        if (haveSceneHub) _sceneSignalHub.EmitAll(quizCompleteSignalName);
        if (haveScenarioHub) _scenarioSignalHub.Emit(quizCompleteSignalName);

        Debug.Log($"Signal emitted: {quizCompleteSignalName}");
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
