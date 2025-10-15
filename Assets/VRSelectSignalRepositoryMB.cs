using System.Collections.Generic;
using UnityEngine;
using VContainer;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class VRSelectSignalRepositoryMB : MonoBehaviour
{
    [Inject] private ConversationOrchestrator _orchestrator;

#if ENABLE_INPUT_SYSTEM
    [Header("XRI Input Actions")]
    [SerializeField] private InputActionReference leftSelect;
    [SerializeField] private InputActionReference rightSelect;
#endif

    [Header("Auto-registered select objects")]
    [SerializeField] private List<VRSignalOnSelectActionWhileSelected> selectors = new();

    private void Awake()
    {
        if (selectors.Count == 0)
        {
#if UNITY_2023_1_OR_NEWER
            selectors.AddRange(FindObjectsByType<VRSignalOnSelectActionWhileSelected>(FindObjectsInactive.Include, FindObjectsSortMode.None));
#else
            selectors.AddRange(FindObjectsOfType<VRSignalOnSelectActionWhileSelected>(true));
#endif
        }

        foreach (var s in selectors)
        {
            if (s == null) continue;

            s.SetOrchestrator(_orchestrator);

#if ENABLE_INPUT_SYSTEM
            if (leftSelect != null || rightSelect != null)
                s.ConfigureInputActions(leftSelect, rightSelect);
#endif
        }

        Debug.Log($"[VRSelectSignalRepositoryMB] Registered {selectors.Count} VR select emitters.");
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        selectors.Clear();
        selectors.AddRange(FindObjectsOfType<VRSignalOnSelectActionWhileSelected>(true));
#endif
    }
}