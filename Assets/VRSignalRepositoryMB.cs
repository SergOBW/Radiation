using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VContainer;

public sealed class VRSignalRepositoryMB : MonoBehaviour
{
    [Inject] private ConversationOrchestrator _orchestrator;

    [Header("Auto-registered grab objects")]
    [SerializeField] private List<VRSignalOnGrab> grabbers = new();

    private void Awake()
    {
        if (grabbers.Count == 0)
        {
#if UNITY_2023_1_OR_NEWER
            grabbers.AddRange(FindObjectsByType<VRSignalOnGrab>(FindObjectsInactive.Include, FindObjectsSortMode.None));
#else
            grabbers.AddRange(FindObjectsOfType<VRSignalOnGrab>(true));
#endif
        }

        // Регистрируем каждому orchestrator
        foreach (var grab in grabbers)
        {
            if (grab != null)
            {
                grab.Initialize(_orchestrator);
            }
        }

        Debug.Log($"[VRSignalRepository] Registered {grabbers.Count} VR grab emitters.");
    }

    private void OnValidate()
    {
        // автообновление списка в редакторе
#if UNITY_EDITOR
        grabbers.Clear();
        grabbers.AddRange(FindObjectsOfType<VRSignalOnGrab>(true));
#endif
    }
}