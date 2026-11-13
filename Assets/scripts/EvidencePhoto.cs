using UnityEngine;

public class EvidencePhoto : MonoBehaviour
{
    public enum DecisionState
    {
        Pending,
        Approved,
        Hidden,
        Discarded
    }

    public enum CaptureType
    {
        Sketch, // Для аномалий (берётся нарисованный скетч из префаба)
        Photo   // Для НЕаномальных улик (делается реальный снимок камерой)
    }

    [Header("Данные улики")]
    public string photoName = "Объект №1";

    [Tooltip("Sketch — зарисовка [Space] для аномалий; Photo — фотоснимок [F] для обычных улик")]
    public CaptureType captureType = CaptureType.Sketch;

    [Tooltip("Сними галочку для обычных улик без аномалий (например, кружка с кровью)")]
    public bool hasRealAnomaly = true;

    [Tooltip("Если включено — зарисовать можно только после проявления аномалии")]
    public bool requireAnomalyBeforeSketch = true;

    [Tooltip("Если на предмете висит ProceduralSurfaceTrace — требовать сначала кликнуть по следу перед фото")]
    public bool requireTraceClickBeforePhoto = true;

    [Tooltip("Ставится автоматически скриптом PhotoAnomaly или УФ/лунным триггером")]
    public bool isAnomalous = false;

    [Header("Истинный ID категории (entity / uv_trace / normal)")]
    public string trueCategoryId = "entity";

    [Header("Графика для блокнота (Зарисовка / Заглушка)")]
    public Texture placeholderSketch;
    public Texture realSketch;

    [HideInInspector]
    public DecisionState currentState = DecisionState.Pending;

    [HideInInspector]
    public bool isSketched = false;

    // Сюда кладётся живой снимок, сделанный через EvidenceCameraCapture
    private Texture2D _runtimePhoto;
    private ProceduralSurfaceTrace _surfaceTrace;

    private void Awake()
    {
        _surfaceTrace = GetComponent<ProceduralSurfaceTrace>();
        if (_surfaceTrace == null) _surfaceTrace = GetComponentInChildren<ProceduralSurfaceTrace>();
    }

    private void OnDestroy()
    {
        if (_runtimePhoto != null) Destroy(_runtimePhoto);
    }

    // Можно ли сейчас сделать зарисовку [Space]
    public bool CanSketch =>
        captureType == CaptureType.Sketch &&
        (!hasRealAnomaly || !requireAnomalyBeforeSketch || isAnomalous);

    // Можно ли сейчас сделать реальное фото [F]
    public bool CanTakePhoto
    {
        get
        {
            if (captureType != CaptureType.Photo) return false;
            if (requireTraceClickBeforePhoto && _surfaceTrace != null)
                return _surfaceTrace.HasAnyDiscoveredTrace();
            return true;
        }
    }

    // [Space] — зарисовка аномалии из префаба
    public void MakeSketch()
    {
        if (!CanSketch || isSketched) return;

        isSketched = true;
        Debug.Log($"[ЗАРИСОВКА] '{photoName}' добавлена в банк блокнота.");

        if (PhysicalReport.Instance != null)
            PhysicalReport.Instance.RegisterSketch(this);
    }

    // [F] — сохранение реального скриншота камеры в банк блокнота (можно переснимать!)
    public void ApplyCapturedPhoto(Texture2D capturedPhoto)
    {
        if (capturedPhoto == null) return;

        if (_runtimePhoto != null) Destroy(_runtimePhoto);
        _runtimePhoto = capturedPhoto;
        isSketched = true;

        Debug.Log($"[ФОТО] Снимок '{photoName}' сохранён в банк блокнота.");

        if (PhysicalReport.Instance != null)
            PhysicalReport.Instance.RegisterSketch(this);
    }

    public Texture GetCurrentSketchTexture()
    {
        if (captureType == CaptureType.Photo)
            return _runtimePhoto != null ? (Texture)_runtimePhoto : placeholderSketch;

        if (!isSketched || realSketch == null) return placeholderSketch;
        return realSketch;
    }

    public void BecomeAnomalous()
    {
        isAnomalous = true;
        currentState = DecisionState.Pending;
        Debug.Log($"[АНОМАЛИЯ] На '{photoName}' проявились аномальные признаки.");
    }

    public void MakeAnomalous()
    {
        BecomeAnomalous();
    }
}