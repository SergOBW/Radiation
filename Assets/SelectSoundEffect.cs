using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class SelectSoundEffect : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private SelectByTriggerOnHover selectByTrigger;
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioClip deselectClip;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (selectByTrigger != null)
            selectByTrigger.OnVirtualSelectionChanged += OnSelectionChanged;
    }

    private void OnDestroy()
    {
        if (selectByTrigger != null)
            selectByTrigger.OnVirtualSelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(SelectByTriggerOnHover sender, bool selected)
    {
        AudioClip clip = selected ? selectClip : deselectClip;
        if (clip == null || _audioSource == null) return;
        _audioSource.PlayOneShot(clip);
    }
}
