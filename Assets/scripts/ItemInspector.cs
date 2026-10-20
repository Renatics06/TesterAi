using UnityEngine;
using UnityEngine.InputSystem;

public class ItemInspector : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform inspectPoint;
    public CameraLook cameraController;

    [Header("Параметры")]
    public float moveSpeed = 10f;
    public float rotateSpeed = 0.2f;
    public float panSpeed = 0.0015f;

    [Header("Параметры дистанции")]
    [Tooltip("Дистанция для взятия улик со стола")]
    public float rayDistance = 4f;
    [Tooltip("Дистанция для перелета камеры к объектам в комнате (окно, шкаф, радио)")]
    public float nodeRayDistance = 25f; // <-- бьет через всю комнату

    [Header("Настройки Зума")]
    public float defaultFOV = 60f;
    public float maxZoomFOV = 20f;
    public float zoomSmoothness = 0.1f;

    private InspectableItem _currentInspectData;
    private Transform _currentItem;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Transform _originalParent;

    private bool _isKinematicState;
    private Collider _itemCollider;
    private Rigidbody _itemRigidbody;

    private bool _isInspecting = false;
    private bool _isReturning = false;

    // Переменные для вычислений 2.5D и 2D
    private float _rotX, _rotY;
    private Vector2 _panOffset;
    private float _currentFOV;

    private void Start()
    {
        _currentFOV = defaultFOV;
        if (Camera.main != null) Camera.main.fieldOfView = defaultFOV;

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraLook>();
    }

    private void Update()
    {
        if (Mouse.current == null || Camera.main == null) return;

        if (_isInspecting) HandleInspectState();
        else if (_isReturning) ReturnItemStep();
        else CheckHoverAndClick();
    }

    private void CheckHoverAndClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // Рисуем длинный синий луч в окне Scene, чтобы видеть, куда метит курсор
        Debug.DrawRay(ray.origin, ray.direction * nodeRayDistance, Color.cyan);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Стреляем на максимальную дистанцию комнаты (nodeRayDistance)
            if (Physics.Raycast(ray, out RaycastHit hit, nodeRayDistance))
            {
                Debug.Log($"[Клик] Луч попал в объект: {hit.collider.name} (дистанция: {hit.distance:F1} м)");

                SuitcaseCinematic suitcase = hit.collider.GetComponentInParent<SuitcaseCinematic>();
                if (suitcase != null)
                {
                    suitcase.StartCinematic();
                    return; // Обязательно return, чтобы прервать дальнейшие проверки!
                }

                // 1. Ищем скрипт MovePositionNode на самом коллайдере ИЛИ на любом его родителе выше по иерархии
                MovePositionNode node = hit.collider.GetComponentInParent<MovePositionNode>();
                if (node != null)
                {
                    if (node.targetView == null)
                    {
                        Debug.LogWarning($"На объекте {node.name} висит MovePositionNode, но поле Target View ПУСТОЕ! Перетащи туда пустышку.");
                        return;
                    }

                    if (cameraController != null)
                    {
                        Debug.Log($"Перелетаем к точке: {node.targetView.name}");
                        cameraController.StartMoveToNode(node.targetView);
                    }
                    else
                    {
                        Debug.LogError("Не найден скрипт CameraLook! Убедись, что он висит на объекте игрока/камеры.");
                    }
                    return;
                }

                // 2. Если это не точка перелета, проверяем, дотягиваемся ли мы до предмета для осмотра (rayDistance)
                if (hit.distance <= rayDistance)
                {
                    InspectableItem item = hit.collider.GetComponentInParent<InspectableItem>();
                    if (item != null)
                    {
                        StartInspect(item, hit.collider);
                    }
                }
            }
        }
    }

    private InspectableItem GetInspectableItem(Transform hitTransform)
    {
        Transform curr = hitTransform;
        while (curr != null)
        {
            if (curr.TryGetComponent(out InspectableItem item)) return item;
            curr = curr.parent;
        }
        return null;
    }

    private void StartInspect(InspectableItem inspectData, Collider hitCol)
    {
        _currentInspectData = inspectData;
        _currentItem = inspectData.transform;
        _itemCollider = hitCol;

        _originalPosition = _currentItem.position;
        _originalRotation = _currentItem.rotation;
        _originalParent = _currentItem.parent;

        if (_currentItem.TryGetComponent(out _itemRigidbody))
        {
            _isKinematicState = _itemRigidbody.isKinematic;
            _itemRigidbody.isKinematic = true;
        }

        if (_itemCollider != null) _itemCollider.enabled = false;
        if (cameraController != null) cameraController.canLook = false;

        _rotX = 0f; _rotY = 0f;
        _panOffset = Vector2.zero;
        _currentFOV = defaultFOV;

        _isInspecting = true;
        _isReturning = false;
    }

    private void HandleInspectState()
    {
        if (_currentItem == null) return;

        // ЗУМ КОЛЕСИКОМ
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _currentFOV -= (scroll / 3f) * 5f;
            _currentFOV = Mathf.Clamp(_currentFOV, maxZoomFOV, defaultFOV);
        }
        Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, _currentFOV, Time.deltaTime * 10f);

        bool isMouseDragging = Mouse.current.leftButton.isPressed;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        Quaternion baseRotation = Camera.main.transform.rotation * Quaternion.Euler(_currentInspectData.facingOffset);

        // ВЫБОР РЕЖИМА ОСМОТРА ИЗ КОМПОНЕНТА
        switch (_currentInspectData.inspectType)
        {
            case InspectableItem.InspectType.Normal3D:
                _currentItem.position = Vector3.Lerp(_currentItem.position, inspectPoint.position, Time.deltaTime * moveSpeed);
                if (isMouseDragging)
                {
                    _currentItem.Rotate(Camera.main.transform.up, -mouseDelta.x * rotateSpeed, Space.World);
                    _currentItem.Rotate(Camera.main.transform.right, mouseDelta.y * rotateSpeed, Space.World);
                }
                break;

            case InspectableItem.InspectType.Limited2_5D:
                _currentItem.position = Vector3.Lerp(_currentItem.position, inspectPoint.position, Time.deltaTime * moveSpeed);
                if (isMouseDragging)
                {
                    _rotX += mouseDelta.y * rotateSpeed * 2f;
                    _rotY -= mouseDelta.x * rotateSpeed * 2f;
                    // Блокируем вращение, чтобы не показать заднюю часть (где нет нормалей)
                    _rotX = Mathf.Clamp(_rotX, -_currentInspectData.maxRotationX, _currentInspectData.maxRotationX);
                    _rotY = Mathf.Clamp(_rotY, -_currentInspectData.maxRotationY, _currentInspectData.maxRotationY);
                }
                _currentItem.rotation = Quaternion.Slerp(_currentItem.rotation, baseRotation * Quaternion.Euler(_rotX, _rotY, 0), Time.deltaTime * moveSpeed);
                break;

            case InspectableItem.InspectType.Flat2D:
                _currentItem.rotation = Quaternion.Slerp(_currentItem.rotation, baseRotation, Time.deltaTime * moveSpeed);
                if (isMouseDragging)
                {
                    _panOffset.x += mouseDelta.x * panSpeed;
                    _panOffset.y += mouseDelta.y * panSpeed;
                    // Ограничиваем сдвиг (панорамирование)
                    _panOffset.x = Mathf.Clamp(_panOffset.x, -_currentInspectData.maxPanDistance, _currentInspectData.maxPanDistance);
                    _panOffset.y = Mathf.Clamp(_panOffset.y, -_currentInspectData.maxPanDistance, _currentInspectData.maxPanDistance);
                }
                Vector3 targetPanPos = inspectPoint.position + (Camera.main.transform.right * _panOffset.x) + (Camera.main.transform.up * _panOffset.y);
                _currentItem.position = Vector3.Lerp(_currentItem.position, targetPanPos, Time.deltaTime * moveSpeed);
                break;
        }

        // Выход на ПКМ или Esc
        if (Mouse.current.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            _isInspecting = false;
            _isReturning = true;
        }
    }

    private void ReturnItemStep()
    {
        if (_currentItem == null) { _isReturning = false; return; }

        // Сброс зума
        Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, defaultFOV, Time.deltaTime * 10f);

        _currentItem.position = Vector3.Lerp(_currentItem.position, _originalPosition, Time.deltaTime * moveSpeed);
        _currentItem.rotation = Quaternion.Slerp(_currentItem.rotation, _originalRotation, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(_currentItem.position, _originalPosition) < 0.01f)
        {
            Camera.main.fieldOfView = defaultFOV;
            _currentItem.position = _originalPosition;
            _currentItem.rotation = _originalRotation;
            _currentItem.SetParent(_originalParent);

            if (_itemCollider != null) _itemCollider.enabled = true;
            if (_itemRigidbody != null) _itemRigidbody.isKinematic = _isKinematicState;
            if (cameraController != null) cameraController.canLook = true;

            _currentItem = null;
            _isReturning = false;
        }
    }

    // ОТРИСОВКА ТЕКСТА ЗУМА
    private void OnGUI()
    {
        if (_isInspecting && Camera.main != null)
        {
            // Считаем проценты: 60 FOV = 100%, 20 FOV = 300%
            int zoomPercent = Mathf.RoundToInt((defaultFOV / Camera.main.fieldOfView) * 100f);

            GUIStyle style = new GUIStyle();
            style.fontSize = 28;
            style.normal.textColor = new Color(1f, 0.2f, 0.2f, 0.8f); // Полупрозрачный красный цвет под твой стиль
            style.alignment = TextAnchor.LowerCenter;

            // Выводим текст в самом низу экрана
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height - 20), $"ZOOM: {zoomPercent}%", style);
        }
    }
}