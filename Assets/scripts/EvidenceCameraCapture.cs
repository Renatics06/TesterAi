using UnityEngine;

public class EvidenceCameraCapture : MonoBehaviour
{
    public static EvidenceCameraCapture Instance { get; private set; }

    [Header("Настройки фотоснимка")]
    [Tooltip("Разрешение квадратного снимка для блокнота")]
    public int photoResolution = 512;

    [Tooltip("Длительность вспышки экрана при съёмке (в секундах)")]
    public float flashDuration = 0.25f;

    [Header("Звук затвора (опционально)")]
    public AudioSource shutterAudioSource;
    public AudioClip shutterClip;

    public float CurrentFlashAlpha { get; private set; } = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (CurrentFlashAlpha > 0f)
        {
            CurrentFlashAlpha = Mathf.MoveTowards(CurrentFlashAlpha, 0f, Time.deltaTime / Mathf.Max(flashDuration, 0.05f));
        }
    }

    /// <summary>
    /// Делает чистый квадратный снимок с камеры без OnGUI/HUD.
    /// Подойдёт и для хоткея [F], и для будущей анимации 3D-фотика.
    /// </summary>
    public Texture2D CaptureSquarePhoto(Camera sourceCamera)
    {
        if (sourceCamera == null) sourceCamera = Camera.main;
        if (sourceCamera == null) return null;

        // sRGB важен для URP в Linear Color Space, чтобы фотка не выходила тёмной
        RenderTexture rt = new RenderTexture(photoResolution, photoResolution, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.antiAliasing = 2;

        RenderTexture prevRT = RenderTexture.active;
        RenderTexture prevCamTarget = sourceCamera.targetTexture;
        float prevAspect = sourceCamera.aspect;

        sourceCamera.targetTexture = rt;
        sourceCamera.aspect = 1f; // Квадратный кадр без сплющивания картинки
        sourceCamera.Render();

        RenderTexture.active = rt;
        Texture2D photo = new Texture2D(photoResolution, photoResolution, TextureFormat.RGB24, false);
        photo.ReadPixels(new Rect(0, 0, photoResolution, photoResolution), 0, 0);
        photo.Apply();

        // Возвращаем камеру в исходное состояние
        sourceCamera.targetTexture = prevCamTarget;
        sourceCamera.aspect = prevAspect;
        sourceCamera.ResetAspect();
        RenderTexture.active = prevRT;

        rt.Release();
        Destroy(rt);

        // Вспышка и звук затвора
        CurrentFlashAlpha = 0.85f;
        if (shutterAudioSource != null && shutterClip != null)
            shutterAudioSource.PlayOneShot(shutterClip);

        return photo;
    }
}