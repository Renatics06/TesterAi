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
    public float nodeRayDistance = 25f;

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

    // Для вращения ручек радиоприемника
    private RadioVolumeKnob _activeKnob = null;
    private bool _wasCanLookBeforeKnob = true;

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

        // 1. Если сейчас крутим ручку радиоприемника
        if (_activeKnob != null)
        {
            HandleKnobRotation();
            return;
        }

        // 2. Осмотр улик или обычное состояние
        if (_isInspecting) HandleInspectState();
        else if (_isReturning) ReturnItemStep();
        else CheckHoverAndClick();
    }

    private void HandleKnobRotation()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            float deltaX = Mouse.current.delta.ReadValue().x;
            _activeKnob.RotateKnob(deltaX);
        }
        else
        {
            // Отпустили ЛКМ — освобождаем ручку
            if (cameraController != null)
                cameraController.canLook = _wasCanLookBeforeKnob;

            _activeKnob = null;
        }
    }

    private void CheckHoverAndClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        Debug.DrawRay(ray.origin, ray.direction * nodeRayDistance, Color.cyan);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, nodeRayDistance))
            {
                Debug.Log($"[Клик] Луч попал в объект: {hit.collider.name} (дистанция: {hit.distance:F1} м)");

                // 1. ПРОВЕРКА НА КРУТИЛКУ РАДИОПРИЕМНИКА (проверяем первой!)
                RadioVolumeKnob knob = hit.collider.GetComponentInParent<RadioVolumeKnob>();
                if (knob != null)
                {
                    _activeKnob = knob;
                    if (cameraController != null)
                    {
                        _wasCanLookBeforeKnob = cameraController.canLook;
                        cameraController.canLook = false; // Блокируем обзор, пока крутим ручку
                    }
                    return;
                }

                // 2. ПРОВЕРКА НА ОКНО ПАУЗЫ ИЛИ КНОПКУ EXIT
                WindowPauseMenu windowMenu = hit.collider.GetComponent<WindowPauseMenu>();
                if (windowMenu == null) windowMenu = hit.collider.GetComponentInParent<WindowPauseMenu>();
                if (windowMenu != null)
                {
                    windowMenu.OnInteract(cameraController);
                    return;
                }

                // 3. ПРОВЕРКА НА ЧЕМОДАН
                SuitcaseCinematic suitcase = hit.collider.GetComponentInParent<SuitcaseCinematic>();
                if (suitcase != null)
                {
                    suitcase.StartCinematic();
                    return;
                }

                // 4. ПРОВЕРКА НА ТОЧКУ ПЕРЕЛЕТА (MovePositionNode)
                MovePositionNode node = hit.collider.GetComponentInParent<MovePositionNode>();
                if (node != null)
                {
                    if (node.targetView == null)
                    {
                        Debug.LogWarning($"На объекте {node.name} висит MovePositionNode, но поле Target View ПУСТОЕ!");
                        return;
                    }

                    if (cameraController != null)
                    {
                        cameraController.StartMoveToNode(node.targetView);
                    }
                    return;
                }

                // 5. ПРОВЕРКА НА ПРЕДМЕТ ДЛЯ ОСМОТРА В РУКАХ
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
                    _panOffset.x = Mathf.Clamp(_panOffset.x, -_currentInspectData.maxPanDistance, _currentInspectData.maxPanDistance);
                    _panOffset.y = Mathf.Clamp(_panOffset.y, -_currentInspectData.maxPanDistance, _currentInspectData.maxPanDistance);
                }
                Vector3 targetPanPos = inspectPoint.position + (Camera.main.transform.right * _panOffset.x) + (Camera.main.transform.up * _panOffset.y);
                _currentItem.position = Vector3.Lerp(_currentItem.position, targetPanPos, Time.deltaTime * moveSpeed);
                break;
        }

        EvidencePhoto photo = _currentItem.GetComponentInChildren<EvidencePhoto>();
        if (photo != null && photo.isAnomalous && Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame && photo.currentState != EvidencePhoto.DecisionState.Approved)
            {
                photo.MakeDecision(EvidencePhoto.DecisionState.Approved);
            }
            else if (Keyboard.current.backspaceKey.wasPressedThisFrame && photo.currentState != EvidencePhoto.DecisionState.Hidden)
            {
                photo.MakeDecision(EvidencePhoto.DecisionState.Hidden);
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            _isInspecting = false;
            _isReturning = true;
        }
    }

    private void ReturnItemStep()
    {
        if (_currentItem == null) { _isReturning = false; return; }

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

    private void OnGUI()
    {
        // Отрисовка громкости при вращении ручки радиоприемника
        if (_activeKnob != null)
        {
            GUIStyle knobStyle = new GUIStyle();
            knobStyle.fontSize = 28;
            knobStyle.normal.textColor = new Color(1f, 0.2f, 0.2f, 0.9f);
            knobStyle.alignment = TextAnchor.LowerCenter;

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height - 20), _activeKnob.GetKnobLabel(), knobStyle);
        }

        if (_isInspecting && Camera.main != null)
        {
            int zoomPercent = Mathf.RoundToInt((defaultFOV / Camera.main.fieldOfView) * 100f);

            GUIStyle style = new GUIStyle();
            style.fontSize = 28;
            style.normal.textColor = new Color(1f, 0.2f, 0.2f, 0.8f);
            style.alignment = TextAnchor.LowerCenter;

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height - 20), $"ZOOM: {zoomPercent}%", style);

            if (_currentItem != null)
            {
                EvidencePhoto photo = _currentItem.GetComponentInChildren<EvidencePhoto>();
                if (photo != null)
                {
                    GUIStyle infoStyle = new GUIStyle();
                    infoStyle.fontSize = 22;
                    infoStyle.alignment = TextAnchor.UpperCenter;

                    if (!photo.isAnomalous)
                    {
                        infoStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.7f);
                        GUI.Label(new Rect(0, 40, Screen.width, 100), "Статус: Обычный снимок (Аномалий не выявлено)", infoStyle);
                    }
                    else if (photo.currentState == EvidencePhoto.DecisionState.Pending)
                    {
                        infoStyle.normal.textColor = Color.yellow;
                        GUI.Label(new Rect(0, 40, Screen.width, 100), "[!] ОБНАРУЖЕНА АНОМАЛИЯ\n[Enter] - Занести в отчет   |   [Backspace] - Утаить улику", infoStyle);
                    }
                    else if (photo.currentState == EvidencePhoto.DecisionState.Approved)
                    {
                        infoStyle.normal.textColor = Color.green;
                        GUI.Label(new Rect(0, 40, Screen.width, 100), "СТАТУС: ЗАФИКСИРОВАНО В ОТЧЕТЕ (+1)\n(Нажмите [Backspace], чтобы передумать и вычеркнуть из отчета)", infoStyle);
                    }
                    else if (photo.currentState == EvidencePhoto.DecisionState.Hidden)
                    {
                        infoStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
                        GUI.Label(new Rect(0, 40, Screen.width, 100), "СТАТУС: УЛИКА УТАЕНА ОТ РУКОВОДСТВА\n(Нажмите [Enter], чтобы передумать и внести в отчет)", infoStyle);
                    }
                }
            }
        }
    }
}