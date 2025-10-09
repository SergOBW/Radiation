using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuizData", menuName = "Quiz/Create Quiz Data", order = 0)]
public class QuizData : ScriptableObject
{
    [Serializable]
    public class Question
    {
        [TextArea] public string questionText;
        public List<string> answers = new List<string>();
        [Tooltip("Индекс правильного ответа в списке answers")]
        public int correctIndex = 0;
    }

    public string quizTitle = "Тест";
    public List<Question> questions = new List<Question>();
}