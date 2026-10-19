using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLook : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform cameraTransform;

    [Header("Edge Panning (Движение по краям)")]
    public float panSpeed = 60f;
    public float edgeThreshold = 30f;
    public float verticalClampAngle = 60f;
    public float horizontalClampAngle = 70f;

    [HideInInspector]
    public bool canLook = true;

    private float _xRotation = 0f;
    private float _yRotation = 0f;

    // Переменные для перелета камеры
    private Vector3 _startPos;
    private Quaternion _startPlayerRot;
    private Quaternion _startCamRot;
    private Transform _currentTargetNode = null;
    private bool _isReturning = false;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Запоминаем базовую позицию стула за столом
        _startPos = transform.position;
        _startPlayerRot = transform.rotation;
        if (cameraTransform != null) _startCamRot = cameraTransform.localRotation;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // ЛОГИКА 1: Перелет камеры к выбранной точке
        if (_currentTargetNode != null)
        {
            MoveToTargetStep();
            return;
        }

        // ЛОГИКА 2: Возврат за стол
        if (_isReturning)
        {
            ReturnToStartStep();
            return;
        }

        // ЛОГИКА 3: Обычный обзор по краям (если не заблокировано)
        if (!canLook) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        float panX = 0f, panY = 0f;

        if (mousePos.x <= edgeThreshold) panX = -1f;
        else if (mousePos.x >= Screen.width - edgeThreshold) panX = 1f;

        if (mousePos.y <= edgeThreshold) panY = -1f;
        else if (mousePos.y >= Screen.height - edgeThreshold) panY = 1f;

        _xRotation -= panY * panSpeed * Time.deltaTime;
        _yRotation += panX * panSpeed * Time.deltaTime;

        _xRotation = Mathf.Clamp(_xRotation, -verticalClampAngle, verticalClampAngle);
        _yRotation = Mathf.Clamp(_yRotation, -horizontalClampAngle, horizontalClampAngle);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        transform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
    }

    // --- МЕТОДЫ ПЕРЕМЕЩЕНИЯ ---
    public void StartMoveToNode(Transform targetView)
    {
        _currentTargetNode = targetView;
        _isReturning = false;
        canLook = false;
    }

    private void MoveToTargetStep()
    {
        transform.position = Vector3.Lerp(transform.position, _currentTargetNode.position, Time.deltaTime * 5f);
        transform.rotation = Quaternion.Slerp(transform.rotation, _currentTargetNode.rotation, Time.deltaTime * 5f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, Quaternion.identity, Time.deltaTime * 5f);

        // Возврат по ПКМ или Esc
        if (Mouse.current.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            _currentTargetNode = null;
            _isReturning = true;
        }
    }

    private void ReturnToStartStep()
    {
        transform.position = Vector3.Lerp(transform.position, _startPos, Time.deltaTime * 5f);
        transform.rotation = Quaternion.Slerp(transform.rotation, _startPlayerRot, Time.deltaTime * 5f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, _startCamRot, Time.deltaTime * 5f);

        if (Vector3.Distance(transform.position, _startPos) < 0.01f)
        {
            _isReturning = false;
            canLook = true;
            // Обновляем углы, чтобы камера не дернулась
            _yRotation = transform.localEulerAngles.y;
            if (_yRotation > 180) _yRotation -= 360;
            if (cameraTransform != null)
            {
                _xRotation = cameraTransform.localEulerAngles.x;
                if (_xRotation > 180) _xRotation -= 360;
            }
        }
    }
}