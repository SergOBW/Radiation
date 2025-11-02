using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;

public class QuizManager : MonoBehaviour
{
    #if UNITY_EDITOR
    [ContextMenu("DEBUG/Auto-Complete Quiz (Random Yes/No)")]
    public void Debug_AutoCompleteRandom()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[QuizManager] Debug_AutoCompleteRandom доступен только в Play Mode.");
            return;
        }

        if (quizData == null || quizData.questions == null || quizData.questions.Count == 0)
        {
            Debug.LogWarning("[QuizManager] Нет данных квиза.");
            return;
        }

        // убедимся, что квиз стартовал
        if (_currentIndex < 0)
        {
            InitQuiz(); // тихо стартуем, если не начат
        }

        // сброс текущей сессии подсчёта
        _score = 0;

        for (int i = 0; i < quizData.questions.Count; i++)
        {
            var q = quizData.questions[i];
            bool chooseCorrect = UnityEngine.Random.value < 0.5f;

            int pickedIndex = q.correctIndex;
            if (!chooseCorrect)
            {
                // выбираем любой неправильный индекс
                if (q.answers.Count > 1)
                {
                    // найдём индекс != correctIndex
                    int wrong = q.correctIndex;
                    int safety = 0;
                    while (wrong == q.correctIndex && safety < 16)
                    {
                        wrong = UnityEngine.Random.Range(0, q.answers.Count);
                        safety++;
                    }
                    pickedIndex = wrong;
                }
                // если единственный вариант ответа — оставляем правильный
            }

            bool isCorrect = (pickedIndex == q.correctIndex);
            if (isCorrect) _score++;

            // учёт глобальному хендлеру + сигналы
            if (QuizScoreHandler.Instance != null)
                QuizScoreHandler.Instance.RegisterAnswer(quizIdOverride, isCorrect);

            EmitAnswerSignal(isCorrect, i);
        }

        // показать финальный результат + завершить квиз глобально
        ShowResult();

        Debug.Log($"[QuizManager] Автоматически пройдено: {_score}/{quizData.questions.Count}");
    }
#endif
    [Header("Data")]
    [SerializeField] private QuizData quizData;
    [SerializeField] private string quizIdOverride; // можно задать вручную, иначе возьмём quizData.name

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

    private void Start()
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

        // === Старт квиза в глобальном хендлере ===
        if (QuizScoreHandler.Instance != null)
        {
            string quizId = !string.IsNullOrWhiteSpace(quizIdOverride)
                ? quizIdOverride
                : (quizData ? quizData.name : "Quiz");
            int totalQ = quizData ? quizData.questions.Count : 0;
            QuizScoreHandler.Instance.StartQuiz(quizId, titleText.text, totalQ);
        }

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
            if (_selectedIndex < 0) return;

            bool isCorrect = (_selectedIndex == q.correctIndex);
            if (isCorrect) _score++;

            // === учёт ответа ===
            if (QuizScoreHandler.Instance != null)
                QuizScoreHandler.Instance.RegisterAnswer(quizIdOverride, isCorrect);

            EmitAnswerSignal(isCorrect, _currentIndex);

            // ... ваша раскраска и блокировка кнопок ...
            for (int i = 0; i < answerButtons.Count; i++)
            {
                if (!answerButtons[i].gameObject.activeInHierarchy) continue;
                bool btnIsCorrect = (i == q.correctIndex);
                if (btnIsCorrect) SetButtonColor(answerButtons[i], correctColor);
                else
                {
                    if (i == _selectedIndex) SetButtonColor(answerButtons[i], wrongColor);
                    else SetButtonColor(answerButtons[i], normalColor);
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

        // === завершить квиз ===
        if (QuizScoreHandler.Instance != null)
            QuizScoreHandler.Instance.CompleteQuiz(quizIdOverride);

        // ваши сигналы — без изменений
        bool haveSceneHub = _sceneSignalHub != null;
        bool haveScenarioHub = _scenarioSignalHub != null;
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
