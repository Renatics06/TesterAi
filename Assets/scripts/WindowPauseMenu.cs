using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class WindowPauseMenu : MonoBehaviour
{
    public enum ElementRole
    {
        WindowTrigger, // Вешается на само окно (раму/створку)
        ExitZone       // Вешается на пустой объект с BoxCollider в проеме открытого окна
    }

    [Header("Роль объекта")]
    public ElementRole role = ElementRole.WindowTrigger;

    [Header("Настройки окна (заполнять только на самом Окне)")]
    [Tooltip("Пустышка перед окном, куда прилетит камера при паузе")]
    public Transform windowCameraView;

    [Tooltip("Объект створки окна, которая будет приоткрываться (может быть само это окно)")]
    public Transform windowSash;

    [Tooltip("На сколько градусов повернется створка при открытии (например, по оси Y или Z)")]
    public Vector3 openRotationOffset = new Vector3(0f, -25f, 0f);

    [Tooltip("Если окно раздвижное (едет, а не крутится) — укажи сдвиг позиции")]
    public Vector3 openPositionOffset = Vector3.zero;

    [Header("Элементы паузы и выхода")]
    [Tooltip("Объект с 3D-текстом ПАУЗА (плавно выплывает из темноты)")]
    public GameObject pauseMenuVisuals;

    [Tooltip("Пустой объект с BoxCollider в щели открытого окна (на нем тоже должен висеть WindowPauseMenu с ролью ExitZone)")]
    public GameObject exitZoneObject;

    [Header("Тайминги анимации")]
    public float openDuration = 1.0f;
    public Vector3 textEmergeOffset = new Vector3(0, -0.1f, 0.2f);

    public static bool IsGamePaused { get; private set; } = false;
    private bool _isAtWindow = false;

    private Quaternion _closedSashRot;
    private Vector3 _closedSashPos;
    private Collider _windowTriggerCollider;

    private TMP_Text[] _menuTexts;
    private Color[] _originalTextColors;
    private Vector3[] _originalTextPositions;
    private Coroutine _animCoroutine;

    // Проверка для системы подсветки: можно ли сейчас навестись на этот объект?
    public bool CanInteract
    {
        get
        {
            if (role == ElementRole.WindowTrigger) return !_isAtWindow;
            if (role == ElementRole.ExitZone) return IsGamePaused;
            return false;
        }
    }

    private void Start()
    {
        if (role == ElementRole.WindowTrigger)
        {
            _windowTriggerCollider = GetComponent<Collider>();

            if (windowSash == null) windowSash = transform;
            _closedSashRot = windowSash.localRotation;
            _closedSashPos = windowSash.localPosition;

            if (pauseMenuVisuals != null)
            {
                _menuTexts = pauseMenuVisuals.GetComponentsInChildren<TMP_Text>(true);
                _originalTextColors = new Color[_menuTexts.Length];
                _originalTextPositions = new Vector3[_menuTexts.Length];

                for (int i = 0; i < _menuTexts.Length; i++)
                {
                    _originalTextColors[i] = _menuTexts[i].color;
                    _originalTextPositions[i] = _menuTexts[i].transform.localPosition;
                }

                SetTextState(0f);
                pauseMenuVisuals.SetActive(false);
            }

            // Прячем зону выхода, пока окно закрыто
            if (exitZoneObject != null)
                exitZoneObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Выход из паузы по ПКМ или Esc
        if (role == ElementRole.WindowTrigger && _isAtWindow && Mouse.current != null)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame ||
               (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                ResumeGame();
            }
        }
    }

    public void OnInteract(CameraLook cameraController)
    {
        if (role == ElementRole.WindowTrigger)
        {
            EnterPause(cameraController);
        }
        else if (role == ElementRole.ExitZone && IsGamePaused)
        {
            QuitGame();
        }
    }

    private void EnterPause(CameraLook cameraController)
    {
        if (_isAtWindow) return;
        _isAtWindow = true;
        IsGamePaused = true;

        // Отключаем коллайдер закрытого окна, чтобы он не перекрывал луч к проему
        if (_windowTriggerCollider != null) _windowTriggerCollider.enabled = false;

        if (pauseMenuVisuals != null) pauseMenuVisuals.SetActive(true);
        if (exitZoneObject != null) exitZoneObject.SetActive(true);

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateWindowAndMenu(1f, false));

        if (cameraController != null && windowCameraView != null)
            cameraController.StartMoveToNode(windowCameraView);
    }

    public void ResumeGame()
    {
        _isAtWindow = false;
        IsGamePaused = false;

        if (exitZoneObject != null) exitZoneObject.SetActive(false);
        if (_windowTriggerCollider != null) _windowTriggerCollider.enabled = true;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateWindowAndMenu(0f, true));
    }

    private IEnumerator AnimateWindowAndMenu(float targetProgress, bool disableAfter)
    {
        float startProgress = 1f - targetProgress;
        float elapsed = 0f;

        Quaternion openRot = _closedSashRot * Quaternion.Euler(openRotationOffset);
        Vector3 openPos = _closedSashPos + openPositionOffset;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float currentProgress = Mathf.Lerp(startProgress, targetProgress, smoothT);

            // 1. Плавно приоткрываем или закрываем створку окна
            if (windowSash != null)
            {
                windowSash.localRotation = Quaternion.Slerp(_closedSashRot, openRot, currentProgress);
                windowSash.localPosition = Vector3.Lerp(_closedSashPos, openPos, currentProgress);
            }

            // 2. Плавно проявляем текст ПАУЗА из темноты
            SetTextState(currentProgress);

            yield return null;
        }

        if (windowSash != null)
        {
            windowSash.localRotation = Quaternion.Slerp(_closedSashRot, openRot, targetProgress);
            windowSash.localPosition = Vector3.Lerp(_closedSashPos, openPos, targetProgress);
        }
        SetTextState(targetProgress);

        if (disableAfter && pauseMenuVisuals != null)
            pauseMenuVisuals.SetActive(false);
    }

    private void SetTextState(float progress)
    {
        if (_menuTexts == null) return;

        Color darkInvisible = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < _menuTexts.Length; i++)
        {
            if (_menuTexts[i] != null)
            {
                _menuTexts[i].color = Color.Lerp(darkInvisible, _originalTextColors[i], progress);
                _menuTexts[i].transform.localPosition = Vector3.Lerp(
                    _originalTextPositions[i] + textEmergeOffset,
                    _originalTextPositions[i],
                    progress);
            }
        }
    }

    private void QuitGame()
    {
        Debug.Log("[ВЫХОД] Выход из игры через окно...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}