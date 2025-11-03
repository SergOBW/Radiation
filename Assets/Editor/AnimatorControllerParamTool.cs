#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;

public static class AnimatorControllerParamTool
{
    [MenuItem("Tools/Animator/Ensure Bot Params")]
    public static void EnsureBotParams()
    {
        Object selected = Selection.activeObject;
        AnimatorController controller = selected as AnimatorController;
        if (controller == null)
        {
            Debug.LogError("Выбери AnimatorController в Project и повтори.");
            return;
        }

        EnsureBool(controller, "IsTalking");
        EnsureBool(controller, "IsWalking");
        EnsureBool(controller, "IsIdle");
        EnsureFloat(controller, "Speed");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[AnimatorControllerParamTool] Параметры проверены/добавлены.");
    }

    private static void EnsureBool(AnimatorController c, string name)
    {
        if (!HasParam(c, name))
            c.AddParameter(name, AnimatorControllerParameterType.Bool);
    }

    private static void EnsureFloat(AnimatorController c, string name)
    {
        if (!HasParam(c, name))
            c.AddParameter(name, AnimatorControllerParameterType.Float);
    }

    private static bool HasParam(AnimatorController c, string name)
    {
        for (int i = 0; i < c.parameters.Length; i++)
            if (c.parameters[i].name == name) return true;
        return false;
    }
}
#endif