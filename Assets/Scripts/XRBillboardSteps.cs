using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;

public class XRStepsPanelWithSignals : MonoBehaviour
{
    [System.Serializable]
    public class StepData
    {
        [TextArea] public string Title;
        [TextArea] public string[] Bullets;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonLabel;

    [SerializeField] private Button prevButton;
    [SerializeField] private TMP_Text prevButtonLabel;

    [Header("Авто-показ пунктов")]
    [SerializeField, Min(0.05f)] private float bulletRevealInterval = 1.2f;

    [Header("Шаги")]
    [SerializeField] private StepData[] steps;

    [Header("Сигналы (по одному на шаг)")]
    [Tooltip("Шлётся при показе шага (после смены заголовка и сброса текста). Пусто = авто: Step{N}_Started")]
    [SerializeField] private string[] stepStartSignals;
    [Tooltip("Шлётся при переходе на следующий шаг или при завершении последнего. Пусто = авто: Step{N}_Completed")]
    [SerializeField] private string[] stepCompleteSignals;

    [Header("Прочие сигналы")]
    [SerializeField] private string restartSignal = "Steps_Restarted";

    // DI
    [Inject] private SceneSignalHub _sceneSignalHub;
    [Inject] private ScenarioSignalHub _scenarioSignalHub;

    private int _currentStepIndex = 0;
    private int _revealedCount = 0;
    private Coroutine _revealRoutine;
    private bool _isRevealing = false;
    private bool _sequenceStarted = false;

    private void Reset()
    {
        // Дефолтные шаги (пример)
        steps = new StepData[3];

        steps[0] = new StepData
        {
            Title = "Шаг 1: Измерение МАЭД ГИ на открытой местности",
            Bullets = new[]
            {
                "Выбери 4 контрольные точки, расположенные со всех сторон дома.",
                "Запиши координаты контрольных точек в протокол.",
                "Измеряй на высоте 1 м от поверхности грунта (не менее 3-х раз).",
                "Результаты измерений впиши в протокол.",
                "Рассчитай среднее значение МАЭД, впиши в протокол."
            }
        };

        steps[1] = new StepData
        {
            Title = "Шаг 2: Измерение МАЭД ГИ внутри дома",
            Bullets = new[]
            {
                "Измерь МАЭД ГИ в центре каждого помещения (не менее 3-х раз).",
                "Во время измерений держи детектор на высоте 1 м от уровня пола.",
                "Запиши результаты измерений в протокол.",
                "Рассчитай среднее значение МАЭД, впиши в протокол."
            }
        };

        steps[2] = new StepData
        {
            Title = "Шаг 3: Сплошная гамма-съёмка внутри дома",
            Bullets = new[]
            {
                "Води гамма-детектором вдоль стен и пола на расстоянии не более 30 см.",
                "Запиши максимальные измеренные результаты МАЭД в протокол."
            }
        };

        stepStartSignals = new string[steps.Length];
        stepCompleteSignals = new string[steps.Length];
    }

    private void Awake()
    {
        if (steps == null || steps.Length == 0)
            Reset();

        EnsureSignalArraysSize();

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(OnPrevClicked);
        }
    }

    private void OnEnable()
    {
        ClearUI();
        EmitIfNotEmpty(restartSignal); // новая сессия
    }

    private void ClearUI()
    {
        titleText.text = string.Empty;
        bodyText.text = string.Empty;
        SetNextButtonLabel("Начать");
        SetPrevButtonInteractable(false);
        _currentStepIndex = 0;
        _sequenceStarted = false;
        StopRevealRoutineIfAny();
    }

    private void StartScenario()
    {
        _sequenceStarted = true;
        SetNextButtonLabel("Далее");
        SetPrevButtonInteractable(false); // на первом шаге «Назад» неактивна
        ShowStep(_currentStepIndex, revealInstantly: false);
    }

    private void ShowStep(int index, bool revealInstantly)
    {
        if (index < 0 || index >= steps.Length) return;

        var step = steps[index];
        titleText.text = step.Title;
        bodyText.text = string.Empty;

        _revealedCount = 0;
        StopRevealRoutineIfAny();

        if (revealInstantly)
        {
            RevealAll(step);
            _isRevealing = false;
            UpdateButtonLabelForStepEnd();
        }
        else
        {
            _revealRoutine = StartCoroutine(RevealBulletsRoutine(step));
        }

        // «шаг начат»
        EmitStepStarted(index);

        // Кнопка «Назад» активна, если это не самый первый шаг и сценарий запущен
        SetPrevButtonInteractable(_sequenceStarted && index > 0);
    }

    private IEnumerator RevealBulletsRoutine(StepData step)
    {
        _isRevealing = true;
        int total = step.Bullets?.Length ?? 0;

        for (_revealedCount = 0; _revealedCount < total; _revealedCount++)
        {
            AppendBullet(step.Bullets[_revealedCount]);
            yield return new WaitForSeconds(bulletRevealInterval);
        }

        _isRevealing = false;
        UpdateButtonLabelForStepEnd();
    }

    private void AppendBullet(string text)
    {
        if (string.IsNullOrEmpty(bodyText.text))
            bodyText.text = "• " + text;
        else
            bodyText.text += "\n• " + text;
    }

    // === Кнопка «Далее» ===
    private void OnNextClicked()
    {
        if (!_sequenceStarted)
        {
            StartScenario();
            return;
        }

        if (_isRevealing)
        {
            RevealAllImmediately();
            return;
        }

        bool isLastStep = _currentStepIndex >= steps.Length - 1;

        // Завершаем текущий шаг только при движении вперёд
        EmitStepCompleted(_currentStepIndex);

        if (!isLastStep)
        {
            _currentStepIndex++;
            ShowStep(_currentStepIndex, revealInstantly: false);
            SetNextButtonLabel("Далее");
        }
        else
        {
            // Последний шаг → «Заново»
            _sequenceStarted = false;
            ClearUI();
        }
    }

    // === Кнопка «Назад» ===
    private void OnPrevClicked()
    {
        if (!_sequenceStarted) return; // до старта нельзя
        if (_currentStepIndex <= 0) return;

        // При возврате не шлём «Completed» следующего шага.
        // Просто показываем предыдущий шаг, чтобы его перечитать — целиком.
        _currentStepIndex--;
        ShowStep(_currentStepIndex, revealInstantly: true);

        // На первом шаге «Назад» неактивна
        SetPrevButtonInteractable(_currentStepIndex > 0);

        // На не-последних шагах подпись «Далее»
        SetNextButtonLabel("Далее");
    }

    private void RevealAllImmediately()
    {
        StopRevealRoutineIfAny();
        var step = steps[_currentStepIndex];
        RevealAll(step);
        _isRevealing = false;
        UpdateButtonLabelForStepEnd();
    }

    private void RevealAll(StepData step)
    {
        int total = step.Bullets?.Length ?? 0;
        for (int i = _revealedCount; i < total; i++)
            AppendBullet(step.Bullets[i]);

        _revealedCount = total;
    }

    private void UpdateButtonLabelForStepEnd()
    {
        bool isLast = _currentStepIndex >= steps.Length - 1;
        SetNextButtonLabel(isLast ? "Заново" : "Далее");
    }

    private void SetNextButtonLabel(string text)
    {
        if (nextButtonLabel != null)
            nextButtonLabel.text = text;
    }

    private void SetPrevButtonInteractable(bool interactable)
    {
        if (prevButton != null)
            prevButton.interactable = interactable;

        if (prevButtonLabel != null)
            prevButtonLabel.alpha = interactable ? 1f : 0.5f;
    }

    private void StopRevealRoutineIfAny()
    {
        if (_revealRoutine != null)
        {
            StopCoroutine(_revealRoutine);
            _revealRoutine = null;
        }
        _isRevealing = false;
    }

    // ===== СИГНАЛЫ =====

    private void EnsureSignalArraysSize()
    {
        if (steps == null) return;
        if (stepStartSignals == null || stepStartSignals.Length != steps.Length)
            stepStartSignals = new string[steps.Length];
        if (stepCompleteSignals == null || stepCompleteSignals.Length != steps.Length)
            stepCompleteSignals = new string[steps.Length];
    }

    private void EmitStepStarted(int index)
    {
        string sig = ResolveStartSignal(index);
        EmitIfNotEmpty(sig);
#if UNITY_EDITOR
        Debug.Log($"[XRStepsPanelWithSignals] Step {index + 1} START → '{sig}'");
#endif
    }

    private void EmitStepCompleted(int index)
    {
        string sig = ResolveCompleteSignal(index);
        EmitIfNotEmpty(sig);
#if UNITY_EDITOR
        Debug.Log($"[XRStepsPanelWithSignals] Step {index + 1} COMPLETE → '{sig}'");
#endif
    }

    private string ResolveStartSignal(int index)
    {
        string s = SafeGet(stepStartSignals, index);
        if (!string.IsNullOrWhiteSpace(s)) return s;
        return $"Step{index + 1}_Started";
    }

    private string ResolveCompleteSignal(int index)
    {
        string s = SafeGet(stepCompleteSignals, index);
        if (!string.IsNullOrWhiteSpace(s)) return s;
        return $"Step{index + 1}_Completed";
    }

    private static string SafeGet(string[] arr, int i)
    {
        if (arr == null || i < 0 || i >= arr.Length) return null;
        return arr[i];
    }

    private void EmitIfNotEmpty(string signal)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;

        if (_sceneSignalHub == null || _scenarioSignalHub == null)
        {
            Debug.LogWarning("[XRStepsPanelWithSignals] Signal hubs not injected. Ensure VContainer injects SceneSignalHub & ScenarioSignalHub.");
            return;
        }

        _sceneSignalHub.EmitAll(signal);
        _scenarioSignalHub.Emit(signal);
    }

    // На случай ручной передачи хабов без DI
    public void SetSignalHubs(SceneSignalHub sceneHub, ScenarioSignalHub scenarioHub)
    {
        _sceneSignalHub = sceneHub;
        _scenarioSignalHub = scenarioHub;
    }
}
