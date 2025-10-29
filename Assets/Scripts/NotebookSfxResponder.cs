using UnityEngine;

using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class NotebookSfxResponder : MonoBehaviour
{
    [Header("Refs")]
    public Notebook notebook;
    public RadiationSurveyManager survey;   // опционально (для звука «всё сделано»)

    [Header("SFX")]
    public AudioClip onWriteClip;           // короткий «скрип ручки» / «щелчок тетради»
    [Range(0f,1f)] public float onWriteVolume = 0.8f;

    public AudioClip onAllDoneClip;         // фанфарка/дзынь, когда все зоны завершены
    [Range(0f,1f)] public float onAllDoneVolume = 0.9f;

    private AudioSource _src;
    private int _lastCompleted = -1;

    private void Awake()
    {
        _src = gameObject.AddComponent<AudioSource>();
        _src.spatialBlend = 0f;   // UI-звук
        _src.playOnAwake = false;
    }

    private void OnEnable()
    {
        if (notebook != null) notebook.Updated += HandleNotebookUpdated;
        if (survey   != null)    survey.AllCompleted += HandleAllCompleted;
    }

    private void OnDisable()
    {
        if (notebook != null) notebook.Updated -= HandleNotebookUpdated;
        if (survey   != null)    survey.AllCompleted -= HandleAllCompleted;
    }

    private void HandleNotebookUpdated()
    {
        if (onWriteClip) _src.PlayOneShot(onWriteClip, onWriteVolume);
    }

    private void HandleAllCompleted()
    {
        if (onAllDoneClip) _src.PlayOneShot(onAllDoneClip, onAllDoneVolume);
    }
}

