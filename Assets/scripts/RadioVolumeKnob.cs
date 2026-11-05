using UnityEngine;

public class RadioVolumeKnob : MonoBehaviour
{
    public enum KnobType
    {
        Music,        // Крутилка 1: Только музыка
        MasterVolume  // Крутилка 2: Общая громкость всей игры
    }

    public enum RotationAxis { X, Y, Z }

    [Header("Назначение крутилки")]
    public KnobType knobType = KnobType.Music;

    [Tooltip("Перетащи сюда объект с AudioSource музыки (нужно только для крутилки Music)")]
    public AudioSource musicSource;

    [Header("Настройки вращения цилиндра")]
    [Tooltip("Вокруг какой локальной оси крутить цилиндр (зависит от модели из Blender)")]
    public RotationAxis rotationAxis = RotationAxis.Y;
    public float sensitivity = 0.005f;
    public float minAngle = -135f; // Крайнее левое положение (0% громкости)
    public float maxAngle = 135f;  // Крайнее правое положение (100% громкости)

    [Range(0f, 1f)]
    public float currentVolume = 0.8f;

    private Quaternion _initialLocalRotation;

    private void Start()
    {
        _initialLocalRotation = transform.localRotation;

        // Синхронизируем стартовую громкость
        if (knobType == KnobType.Music && musicSource != null)
        {
            currentVolume = musicSource.volume;
        }
        else if (knobType == KnobType.MasterVolume)
        {
            currentVolume = AudioListener.volume;
        }

        UpdateKnobVisualsAndAudio();
    }

    // Вызывается из ItemInspector, когда игрок зажал ЛКМ и двигает мышь
    public void RotateKnob(float mouseDeltaX)
    {
        currentVolume = Mathf.Clamp01(currentVolume + mouseDeltaX * sensitivity);
        UpdateKnobVisualsAndAudio();
    }

    private void UpdateKnobVisualsAndAudio()
    {
        // 1. Поворачиваем сам цилиндр между minAngle и maxAngle
        float angle = Mathf.Lerp(minAngle, maxAngle, currentVolume);
        Vector3 axis = rotationAxis == RotationAxis.Y ? Vector3.up :
                      (rotationAxis == RotationAxis.Z ? Vector3.forward : Vector3.right);

        transform.localRotation = _initialLocalRotation * Quaternion.AngleAxis(angle, axis);

        // 2. Применяем громкость
        if (knobType == KnobType.Music)
        {
            if (musicSource != null) musicSource.volume = currentVolume;
        }
        else if (knobType == KnobType.MasterVolume)
        {
            AudioListener.volume = currentVolume;
        }
    }

    public string GetKnobLabel()
    {
        int percent = Mathf.RoundToInt(currentVolume * 100f);
        return knobType == KnobType.Music
            ? $"ГРОМКОСТЬ МУЗЫКИ: {percent}%"
            : $"ОБЩАЯ ГРОМКОСТЬ: {percent}%";
    }
}