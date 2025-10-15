using System.Collections.Generic;
using UnityEngine;
using VContainer;

public sealed class VRStateHubRepositoryMB : MonoBehaviour
{
    [Inject] private BoolStateHub _stateHub;

    [Header("Auto-registered components that need StateHub")]
    [SerializeField] private List<MonoBehaviour> stateUsers = new List<MonoBehaviour>();

    private void Awake()
    {
        // Если список пуст — найдём всех кандидатов на сцене и отфильтруем тех, кто реализует IStateHubUser
        if (stateUsers.Count == 0)
        {
#if UNITY_2023_1_OR_NEWER
            var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = FindObjectsOfType<MonoBehaviour>(true);
#endif
            for (int i = 0; i < all.Length; i++)
            {
                var mb = all[i];
                if (mb == null) continue;
                if (mb is VRHoldFlagEmitter) stateUsers.Add(mb);
            }
        }

        // Прокидываем StateHub
        for (int i = 0; i < stateUsers.Count; i++)
        {
            var mb = stateUsers[i];
            if (mb == null) continue;

            var user = mb as VRHoldFlagEmitter;
            if (user != null) user.SetStateHub(_stateHub);
        }

        Debug.Log($"[VRStateHubRepositoryMB] Initialized StateHub for {stateUsers.Count} components.");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Для удобства в редакторе — всегда пересобираем список
        stateUsers.Clear();
        var all = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i];
            if (mb == null) continue;
            if (mb is VRHoldFlagEmitter) stateUsers.Add(mb);
        }
    }
#endif
}