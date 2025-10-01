using UnityEngine;
#if UNITY_XR_MANAGEMENT || ENABLE_VR
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
#endif

[DisallowMultipleComponent]
public class GeigerContinuousAudio : MonoBehaviour
{
    [Header("Источник данных")]
    public DosimeterSensor sensor;

    [Header("БАЗОВЫЙ ШУМ (непрерывный)")]
    [Tooltip("Короткий зацикливаемый шум дозиметра без щелчков (seamless loop)")]
    public AudioClip baseLoopClip;
    [Range(0f,1f)] public float baseMaxVolume = 0.6f;
    [Tooltip("Скорость сглаживания громкости, сек")]
    public float volumeSmooth = 0.25f;
    [Tooltip("Скорость сглаживания питча, сек")]
    public float pitchSmooth = 0.25f;
    [Tooltip("К какому уровню (µSv/h) соответствует максимальная громкость baseMaxVolume")]
    public float baseReferenceRate = 100f;
    [Tooltip("Минимальный уровень (µSv/h), ниже которого — полная тишина")]
    public float minAudibleRate = 0.5f;
    [Tooltip("Диапазон изменения питча (1.0 = без изменений)")]
    public Vector2 pitchRange = new Vector2(0.9f, 1.25f);
    [Tooltip("Маппинг громкости по степени: 1=линейно, 0.5=мягче, 2=агрессивнее")]
    public float volumePower = 0.7f;

    [Header("ALARM")]
    public bool alarmEnabled = true;
    [Tooltip("Порог ВКЛ (µSv/h)")]
    public float alarmOnThreshold = 50f;
    [Tooltip("Порог ВЫКЛ (µSv/h) — ниже этого выключаем (гистерезис)")]
    public float alarmOffThreshold = 40f;
    public AudioClip alarmLoopClip;
    [Range(0f,1f)] public float alarmVolume = 0.8f;
    [Tooltip("Когда активен alarm — ослабляем базовый шум")]
    [Range(0f,1f)] public float duckingWhileAlarm = 0.35f;

#if UNITY_XR_MANAGEMENT || ENABLE_VR
    [Header("XR Вибрация (опционально)")]
    public HapticImpulsePlayer haptics;
    [Range(0f,1f)] public float hapticAmplitude = 0.35f;
    public float hapticDuration = 0.08f;
#endif

    [Header("3D настройки")]
    [Range(0f,1f)] public float spatialBlend = 1f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
    public float minDistance = 0.2f;
    public float maxDistance = 6f;
    public float dopplerLevel = 0f;

    // internals
    private AudioSource _baseSrc;
    private AudioSource _alarmSrc;
    private float _volVel;
    private float _pitchVel;
    private float _targetVol;
    private float _targetPitch = 1f;
    private bool  _alarmActive;

    private void Awake()
    {
        if (baseLoopClip)
        {
            _baseSrc = gameObject.AddComponent<AudioSource>();
            Setup3D(_baseSrc);
            _baseSrc.clip = baseLoopClip;
            _baseSrc.loop = true;
            _baseSrc.playOnAwake = false;
            _baseSrc.volume = 0f;
            _baseSrc.pitch = 1f;
        }

        if (alarmLoopClip)
        {
            _alarmSrc = gameObject.AddComponent<AudioSource>();
            Setup3D(_alarmSrc);
            _alarmSrc.clip = alarmLoopClip;
            _alarmSrc.loop = true;
            _alarmSrc.playOnAwake = false;
            _alarmSrc.volume = alarmVolume;
        }
    }

    private void Setup3D(AudioSource src)
    {
        src.spatialBlend = spatialBlend;
        src.rolloffMode  = rolloffMode;
        src.minDistance  = minDistance;
        src.maxDistance  = maxDistance;
        src.dopplerLevel = dopplerLevel;
    }

    private void Update()
    {
        float rate = (sensor != null) ? Mathf.Max(0f, sensor.CurrentDoseRateMicroSvPerHour) : 0f;

        // ==== БАЗОВЫЙ ШУМ ====
        if (_baseSrc)
        {
            // громкость: 0..1 → далее умножим на baseMaxVolume
            float norm = 0f;
            if (rate > minAudibleRate && baseReferenceRate > 0.0001f)
                norm = Mathf.Clamp01(rate / baseReferenceRate);

            // экспоненциальная кривая (приятнее на слух)
            norm = Mathf.Pow(norm, Mathf.Clamp(volumePower, 0.2f, 3f));

            // ducking при активном alarm
            if (_alarmActive) norm *= duckingWhileAlarm;

            _targetVol = norm * baseMaxVolume;
            _baseSrc.volume = Mathf.SmoothDamp(_baseSrc.volume, _targetVol, ref _volVel, volumeSmooth);

            // питч: от 1 к верхней границе при росте уровня
            float t = Mathf.Clamp01(rate / Mathf.Max(1e-4f, baseReferenceRate));
            float targetPitch = Mathf.Lerp(pitchRange.x, pitchRange.y, t);
            _baseSrc.pitch = Mathf.SmoothDamp(_baseSrc.pitch, targetPitch, ref _pitchVel, pitchSmooth);

            // управление воспроизведением
            bool shouldPlay = _targetVol > 0.001f;
            if (shouldPlay && !_baseSrc.isPlaying) _baseSrc.Play();
            if (!shouldPlay && _baseSrc.isPlaying && _baseSrc.volume < 0.02f) _baseSrc.Stop(); // мягкая остановка
        }

        // ==== ALARM с гистерезисом ====
        if (_alarmSrc && alarmEnabled)
        {
            if (!_alarmActive && rate >= alarmOnThreshold)
            {
                _alarmActive = true;
                if (!_alarmSrc.isPlaying) _alarmSrc.Play();
#if UNITY_XR_MANAGEMENT || ENABLE_VR
                if (haptics) { try { haptics.SendHapticImpulse(hapticAmplitude, hapticDuration); } catch { } }
#endif
            }
            else if (_alarmActive && rate <= alarmOffThreshold)
            {
                _alarmActive = false;
                if (_alarmSrc.isPlaying) _alarmSrc.Stop();
            }
        }
        else
        {
            _alarmActive = false;
            if (_alarmSrc && _alarmSrc.isPlaying) _alarmSrc.Stop();
        }
    }
}
