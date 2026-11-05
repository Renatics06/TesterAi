using UnityEngine;
using UnityEngine.InputSystem;

public class WindowPauseMenu : MonoBehaviour
{
    public enum ElementRole
    {
        WindowTrigger, // Вешается на само окно
        ExitButton     // Вешается на 3D-текст / кнопку EXIT на окне
    }

    [Header("Роль объекта")]
    public ElementRole role = ElementRole.WindowTrigger;

    [Header("Настройки (заполнять только на самом Окне)")]
    [Tooltip("Пустышка перед окном, куда прилетит камера при паузе")]
    public Transform windowCameraView;

    [Tooltip("Объект с надписью ПАУЗА и кнопкой EXIT (будет включаться при клике на окно)")]
    public GameObject pauseMenuVisuals;

    public static bool IsGamePaused { get; private set; } = false;
    private bool _isAtWindow = false;

    private void Start()
    {
        if (role == ElementRole.WindowTrigger && pauseMenuVisuals != null)
        {
            pauseMenuVisuals.SetActive(false);
        }
    }

    private void Update()
    {
        // Если мы в режиме паузы у окна и нажали ПКМ или Esc — выходим из паузы
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
        else if (role == ElementRole.ExitButton)
        {
            QuitGame();
        }
    }

    private void EnterPause(CameraLook cameraController)
    {
        if (_isAtWindow) return;

        _isAtWindow = true;
        IsGamePaused = true;

        if (pauseMenuVisuals != null)
            pauseMenuVisuals.SetActive(true);

        if (cameraController != null && windowCameraView != null)
            cameraController.StartMoveToNode(windowCameraView);

        Debug.Log("[ПАУЗА] Игра на паузе у окна. ПКМ/Esc — вернуться за стол.");
    }

    public void ResumeGame()
    {
        _isAtWindow = false;
        IsGamePaused = false;

        if (pauseMenuVisuals != null)
            pauseMenuVisuals.SetActive(false);

        Debug.Log("[ПАУЗА] Возврат в игру.");
    }

    private void QuitGame()
    {
        Debug.Log("[ВЫХОД] Закрытие игры...");
        Application.Quit();

#if UNITY_EDITOR
        // Чтобы выход работал прямо при тесте в самом редакторе Unity
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}