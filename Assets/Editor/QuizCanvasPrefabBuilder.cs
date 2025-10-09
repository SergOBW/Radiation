#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if UNITY_XR_MANAGEMENT || ENABLE_VR
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

public static class QuizCanvasPrefabBuilder
{
    [MenuItem("Tools/Quiz/Create Quiz Canvas Prefab")]
    public static void CreateQuizCanvasPrefab()
    {
        // --- Создаём корневой Canvas (World Space) ---
        var root = new GameObject("QuizCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        root.AddComponent<GraphicRaycaster>();
        #if UNITY_XR_MANAGEMENT || ENABLE_VR
        root.AddComponent<TrackedDeviceGraphicRaycaster>();
        #endif

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1200, 800);
        root.transform.position = Vector3.zero;

        // Фон
        var bg = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(root.transform, false);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.09f, 0.1f, 0.12f, 0.9f); // тёмный полупрозрачный

        // --- Header ---
        var header = CreatePanel("PanelHeader", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -80), new Vector2(0, 0));
        var title = CreateTMP("Title", header.transform, "Тест", 48, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(20, 10);
        titleRT.offsetMax = new Vector2(-20, -10);

        // --- Вопрос ---
        var qPanel = CreatePanel("PanelQuestion", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -180), new Vector2(0, -90));
        var qText = CreateTMP("QuestionText", qPanel.transform, "Текст вопроса", 36, TextAlignmentOptions.Left);
        var qRT = qText.GetComponent<RectTransform>();
        qRT.anchorMin = new Vector2(0, 0);
        qRT.anchorMax = new Vector2(1, 1);
        qRT.offsetMin = new Vector2(20, 10);
        qRT.offsetMax = new Vector2(-20, -10);

        // --- Ответы (вертикальный список) ---
        var answersPanel = CreatePanel("AnswersGrid", root.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 100), new Vector2(0, -220));
        var layout = answersPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        var fitter = answersPanel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button[] answerButtons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            answerButtons[i] = CreateAnswerButton($"AnswerButton_{i}", answersPanel.transform, $"Ответ {i + 1}");
        }

        // --- BottomBar с кнопкой Next ---
        var bottomBar = CreatePanel("BottomBar", root.transform, new Vector2(0,0), new Vector2(1,0), new Vector2(0,0), new Vector2(0,80));
        var nextBtn = CreateButton("NextButton", bottomBar.transform, "Следующий вопрос");
        var nextRT = nextBtn.GetComponent<RectTransform>();
        nextRT.anchorMin = new Vector2(1, 0.5f);
        nextRT.anchorMax = new Vector2(1, 0.5f);
        nextRT.sizeDelta = new Vector2(360, 60);
        nextRT.anchoredPosition = new Vector2(-200, 0);

        // --- Панель результата (скрыта) ---
        var resultPanel = CreatePanel("ResultPanel", root.transform, new Vector2(0,0), new Vector2(1,1), new Vector2(0,0), new Vector2(0,0));
        var resImg = resultPanel.GetComponent<Image>();
        resImg.color = new Color(0f, 0f, 0f, 0.6f);
        var resultText = CreateTMP("ResultText", resultPanel.transform, "Ваш результат: 0 / 0", 48, TextAlignmentOptions.Center);
        var rrt = resultText.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0, 0);
        rrt.anchorMax = new Vector2(1, 1);
        rrt.offsetMin = new Vector2(20, 20);
        rrt.offsetMax = new Vector2(-20, -20);
        resultPanel.SetActive(false);

        // --- Подключаем QuizManager и сериализованные ссылки ---
        var qm = root.AddComponent<QuizManager>();

        // Пытаемся найти/создать QuizData и присвоить (не обязательно)
        var quizData = AssetDatabase.LoadAssetAtPath<QuizData>("Assets/Quiz/QuizData.asset");
        if (quizData == null)
        {
            AssetDatabase.CreateFolder("Assets", "Quiz");
            AssetDatabase.CreateFolder("Assets/Quiz", "Prefabs");
            quizData = ScriptableObject.CreateInstance<QuizData>();
            AssetDatabase.CreateAsset(quizData, "Assets/Quiz/QuizData.asset");
            AssetDatabase.SaveAssets();
        }

        // Устанавливаем приватные [SerializeField] через SerializedObject
        var so = new SerializedObject(qm);
        so.FindProperty("quizData").objectReferenceValue = quizData;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("questionText").objectReferenceValue = qText;

        var listProp = so.FindProperty("answerButtons");
        listProp.arraySize = answerButtons.Length;
        for (int i = 0; i < answerButtons.Length; i++)
        {
            listProp.GetArrayElementAtIndex(i).objectReferenceValue = answerButtons[i];
        }

        so.FindProperty("nextButton").objectReferenceValue = nextBtn;
        var nextLabel = nextBtn.GetComponentInChildren<TMP_Text>(true);
        so.FindProperty("nextButtonLabel").objectReferenceValue = nextLabel;
        so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        so.FindProperty("resultText").objectReferenceValue = resultText;

        // Цвета по умолчанию
        so.FindProperty("normalColor").colorValue = Color.white;
        so.FindProperty("correctColor").colorValue = new Color(0.2f, 0.8f, 0.2f);
        so.FindProperty("wrongColor").colorValue = new Color(0.9f, 0.2f, 0.2f);

        so.ApplyModifiedPropertiesWithoutUndo();

        // --- Сохраняем как префаб ---
        if (!AssetDatabase.IsValidFolder("Assets/Quiz")) AssetDatabase.CreateFolder("Assets", "Quiz");
        if (!AssetDatabase.IsValidFolder("Assets/Quiz/Prefabs")) AssetDatabase.CreateFolder("Assets/Quiz", "Prefabs");
        string path = "Assets/Quiz/Prefabs/QuizCanvas.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Debug.Log($"QuizCanvas prefab created at: {path}");

        // Чистим сцену (оставим только префаб-ассет)
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Quiz", "Префаб создан:\nAssets/Quiz/Prefabs/QuizCanvas.prefab", "OK");
    }

    // -------- Helpers --------
    private static GameObject CreatePanel(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        var img = go.GetComponent<Image>();
        img.color = new Color(1, 1, 1, 0.06f);
        return go;
    }

    private static TMP_Text CreateTMP(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.9f, 1f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.fadeDuration = 0.05f;
        btn.colors = colors;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 60);

        var txt = CreateTMP("Label", go.transform, label, 32, TextAlignmentOptions.Center);
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16, 6);
        trt.offsetMax = new Vector2(-16, -6);

        return btn;
    }

    private static Button CreateAnswerButton(string name, Transform parent, string label)
    {
        var btn = CreateButton(name, parent, label);

        // НЕ задаём sizeDelta.x = 0
        // rt.sizeDelta = new Vector2(0, 80);  // ← уберите/не ставьте

        var rt = btn.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, 80); // только высота

        var le = btn.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 80;
        le.flexibleWidth = 1; // ← позволит занять всю доступную ширину

        var img = btn.GetComponent<Image>();
        img.color = new Color(0.25f, 0.27f, 0.3f, 1f);

        return btn;
    }
}
#endif
