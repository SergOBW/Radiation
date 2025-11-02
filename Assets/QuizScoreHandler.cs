using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class QuizScoreHandler : MonoBehaviour
{
    public static QuizScoreHandler Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private string debugPrefix = "[QuizScoreHandler] ";

    [Serializable]
    public sealed class QuizScore
    {
        public string QuizId;
        public string Title;
        public int TotalQuestions;
        public int Correct;
        public DateTime TimestampUtc;
    }

    private sealed class Session
    {
        public string QuizId;
        public string Title;
        public int Total;
        public int Correct;
        public DateTime StartedUtc;
    }

    private sealed class RegistryItem
    {
        public string QuizId;
        public string Title;
        public int TotalQuestions;
    }

    public event Action Changed;

    private readonly Dictionary<string, Session> _active = new Dictionary<string, Session>();
    private readonly Dictionary<string, QuizScore> _scoresByQuiz = new Dictionary<string, QuizScore>();
    private readonly Dictionary<string, RegistryItem> _registry = new Dictionary<string, RegistryItem>();
    private readonly List<string> _registryOrder = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Log("Awake");
    }

    public void RegisterQuiz(string quizId, string title, int totalQuestions)
    {
        if (string.IsNullOrWhiteSpace(quizId)) return;
        if (!_registry.ContainsKey(quizId))
        {
            var item = new RegistryItem { QuizId = quizId, Title = GetSafeTitle(title), TotalQuestions = Mathf.Max(0, totalQuestions) };
            _registry.Add(quizId, item);
            _registryOrder.Add(quizId);
        }
        else
        {
            var item = _registry[quizId];
            item.Title = GetSafeTitle(title);
            item.TotalQuestions = Mathf.Max(0, totalQuestions);
        }
        Log("RegisterQuiz[" + quizId + "]");
        Changed?.Invoke();
    }

    public void StartQuiz(string quizId, string title, int totalQuestions)
    {
        if (string.IsNullOrWhiteSpace(quizId)) return;
        RegisterQuiz(quizId, title, totalQuestions);
        var session = new Session();
        session.QuizId = quizId;
        session.Title = _registry[quizId].Title;
        session.Total = _registry[quizId].TotalQuestions;
        session.Correct = 0;
        session.StartedUtc = DateTime.UtcNow;
        _active[quizId] = session;
        Log("StartQuiz[" + quizId + "]");
        Changed?.Invoke();
    }

    public void RegisterAnswer(string quizId, bool isCorrect)
    {
        if (!_active.ContainsKey(quizId)) return;
        if (isCorrect) _active[quizId].Correct++;
        Changed?.Invoke();
    }

    public void CompleteQuiz(string quizId)
    {
        if (!_active.ContainsKey(quizId)) return;
        var s = _active[quizId];
        var score = new QuizScore();
        score.QuizId = s.QuizId;
        score.Title = s.Title;
        score.TotalQuestions = s.Total;
        score.Correct = Mathf.Clamp(s.Correct, 0, s.Total);
        score.TimestampUtc = DateTime.UtcNow;
        _scoresByQuiz[quizId] = score;
        _active.Remove(quizId);
        Log("CompleteQuiz[" + quizId + "]: " + score.Correct + "/" + score.TotalQuestions);
        Changed?.Invoke();
    }

    public bool IsAllRegisteredCompleted()
    {
        if (_registry.Count == 0) return false;
        for (int i = 0; i < _registryOrder.Count; i++)
        {
            var id = _registryOrder[i];
            if (!_scoresByQuiz.ContainsKey(id)) return false;
        }
        return true;
    }

    public IReadOnlyList<QuizScore> GetRegisteredSnapshot()
    {
        var list = new List<QuizScore>(_registryOrder.Count);
        for (int i = 0; i < _registryOrder.Count; i++)
        {
            var id = _registryOrder[i];
            if (_scoresByQuiz.TryGetValue(id, out var sc))
            {
                list.Add(sc);
            }
            else
            {
                var reg = _registry[id];
                var stub = new QuizScore();
                stub.QuizId = reg.QuizId;
                stub.Title = reg.Title;
                stub.TotalQuestions = reg.TotalQuestions;
                stub.Correct = 0;
                stub.TimestampUtc = DateTime.MinValue;
                list.Add(stub);
            }
        }
        return list;
    }

    public int GetTotalCorrect()
    {
        int sum = 0;
        for (int i = 0; i < _registryOrder.Count; i++)
        {
            var id = _registryOrder[i];
            if (_scoresByQuiz.TryGetValue(id, out var sc)) sum += sc.Correct;
        }
        return sum;
    }

    public int GetTotalQuestions()
    {
        int sum = 0;
        for (int i = 0; i < _registryOrder.Count; i++)
        {
            var id = _registryOrder[i];
            sum += _registry[id].TotalQuestions;
        }
        return sum;
    }

    public float GetOverallPercent()
    {
        int total = GetTotalQuestions();
        if (total <= 0) return 0f;
        return (float)GetTotalCorrect() / total * 100f;
    }

    public void ClearAll()
    {
        _active.Clear();
        _scoresByQuiz.Clear();
        _registry.Clear();
        _registryOrder.Clear();
        Changed?.Invoke();
    }

    public void DumpState(string tag)
    {
        if (!debugLogs) return;
        var sb = new StringBuilder();
        sb.AppendLine(debugPrefix + "=== " + tag + " ===");
        sb.AppendLine("Active: " + _active.Count);
        foreach (var kv in _active)
        {
            var s = kv.Value;
            sb.AppendLine("A " + s.QuizId + " " + s.Title + " " + s.Correct + "/" + s.Total);
        }
        sb.AppendLine("Registered: " + _registry.Count);
        for (int i = 0; i < _registryOrder.Count; i++)
        {
            var id = _registryOrder[i];
            var r = _registry[id];
            sb.AppendLine("R " + r.QuizId + " " + r.Title + " " + r.TotalQuestions);
        }
        sb.AppendLine("Saved: " + _scoresByQuiz.Count);
        foreach (var kv in _scoresByQuiz)
        {
            var sc = kv.Value;
            sb.AppendLine("S " + sc.QuizId + " " + sc.Title + " " + sc.Correct + "/" + sc.TotalQuestions);
        }
        Debug.Log(sb.ToString());
    }

    private string GetSafeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Quiz";
        return title;
    }

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log(debugPrefix + msg);
    }
}
