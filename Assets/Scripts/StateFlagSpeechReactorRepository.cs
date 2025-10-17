using System.Collections.Generic;
using UnityEngine;
using VContainer;

public sealed class StateFlagSpeechReactorRepository : MonoBehaviour
{
    [Header("Список еакций на сцене")]
    [SerializeField]
    private List<StateFlagSpeechReactorMB> stateFlagSpeechReactor = new();

    [Inject]
    public void Construct(ActorRepository repo, BoolStateHub stateHub)
    {
        foreach (var stateFlagSpeechReactor in stateFlagSpeechReactor)
        {
            stateFlagSpeechReactor.SetRepository(repo);
            stateFlagSpeechReactor.SetStateHub(stateHub);
        }
    }

}
