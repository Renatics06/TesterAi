using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class ItemInspector : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform inspectPoint;
    public CameraLook cameraController;

    [Header("Параметры осмотра")]
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

    [Header("Настройки подсветки по коллайдеру")]
    public Color highlightColor = new Color(1f, 0.95f, 0.8f, 0.14f);
    public Color exitZoneHighlightColor = new Color(1f, 0.25f, 0.25f, 0.18f);

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

    private RadioVolumeKnob _activeKnob = null;
    private bool _wasCanLookBeforeKnob = true;

    private GameObject _highlightBoxObj;
    private MeshRenderer _highlightRenderer;
    private bool _isHoveringExitZone = false;

    private float _rotX, _rotY;
    private Vector2 _panOffset;
    private float _currentFOV;

    private void Start()
    {
        _currentFOV = defaultFOV;
        if (Camera.main != null) Camera.main.fieldOfView = defaultFOV;

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraLook>();

        CreateHighlightVolume();
    }

    private void CreateHighlightVolume()
    {
        _highlightBoxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _highlightBoxObj.name = "ColliderHighlightVolume";

        Collider col = _highlightBoxObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        _highlightRenderer = _highlightBoxObj.GetComponent<MeshRenderer>();
        _highlightRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _highlightRenderer.receiveShadows = false;

        Shader transShader = Shader.Find("Sprites/Default");
        if (transShader != null)
        {
            Material mat = new Material(transShader);
            mat.color = highlightColor;
            _highlightRenderer.material = mat;
        }

        _highlightBoxObj.SetActive(false);
    }

    private void Update()
    {
        if (Mouse.current == null || Camera.main == null) return;

        if (_activeKnob != null)
        {
            HideHighlight();
            HandleKnobRotation();
            return;
        }

        if (_isInspecting)
        {
            HideHighlight();
            HandleInspectState();
        }
        else if (_isReturning)
        {
            HideHighlight();
            ReturnItemStep();
        }
        else
        {
            CheckHoverAndClick();
            HandleNotebookControls();
        }
    }

    // Управление страницами блокнота (когда игрок смотрит на стол/блокнот и не держит улику)
    private void HandleNotebookControls()
    {
        if (PhysicalReport.Instance == null || Keyboard.current == null || WindowPauseMenu.IsGamePaused)
            return;

        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            PhysicalReport.Instance.NextPage();
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            PhysicalReport.Instance.PrevPage();
        }
        else if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            PhysicalReport.Instance.DiscardCurrentPage();
        }
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
            if (cameraController != null)
                cameraController.canLook = _wasCanLookBeforeKnob;

            _activeKnob = null;
        }
    }

    private void CheckHoverAndClick()
    {
        _isHoveringExitZone = false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        Debug.DrawRay(ray.origin, ray.direction * nodeRayDistance, Color.cyan);

        if (Physics.Raycast(ray, out RaycastHit hit, nodeRayDistance))
        {
            Collider hitCol = hit.collider;

            // 1. Крутилки радиоприемника
            RadioVolumeKnob knob = hitCol.GetComponentInParent<RadioVolumeKnob>();
            if (knob != null)
            {
                ShowHighlightOnCollider(hitCol, highlightColor);
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _activeKnob = knob;
                    if (cameraController != null)
                    {
                        _wasCanLookBeforeKnob = cameraController.canLook;
                        cameraController.canLook = false;
                    }
                }
                return;
            }

            // 2. Окно паузы и зона выхода в открытом окне
            WindowPauseMenu windowMenu = hitCol.GetComponent<WindowPauseMenu>();
            if (windowMenu == null) windowMenu = hitCol.GetComponentInParent<WindowPauseMenu>();
            if (windowMenu != null && windowMenu.CanInteract)
            {
                bool isExit = windowMenu.role == WindowPauseMenu.ElementRole.ExitZone;
                _isHoveringExitZone = isExit;

                ShowHighlightOnCollider(hitCol, isExit ? exitZoneHighlightColor : highlightColor);

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    windowMenu.OnInteract(cameraController);
                }
                return;
            }

            // 3. Чемодан с уликами
            SuitcaseCinematic suitcase = hitCol.GetComponentInParent<SuitcaseCinematic>();
            if (suitcase != null)
            {
                ShowHighlightOnCollider(hitCol, highlightColor);
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    suitcase.StartCinematic();
                }
                return;
            }

            // 4. Точки перелета камеры (MovePositionNode)
            MovePositionNode node = hitCol.GetComponentInParent<MovePositionNode>();
            if (node != null && node.targetView != null)
            {
                ShowHighlightOnCollider(hitCol, highlightColor);
                if (Mouse.current.leftButton.wasPressedThisFrame && cameraController != null)
                {
                    cameraController.StartMoveToNode(node.targetView);
                }
                return;
            }

            // 5. Предметы на столе для осмотра в руках
            if (hit.distance <= rayDistance)
            {
                InspectableItem item = hitCol.GetComponentInParent<InspectableItem>();
                if (item != null)
                {
                    ShowHighlightOnCollider(hitCol, highlightColor);
                    if (Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        StartInspect(item, hitCol);
                    }
                    return;
                }
            }
        }

        HideHighlight();
    }

    private void ShowHighlightOnCollider(Collider col, Color color)
    {
        if (_highlightBoxObj == null || col == null) return;

        if (!_highlightBoxObj.activeSelf)
            _highlightBoxObj.SetActive(true);

        if (_highlightRenderer != null)
            _highlightRenderer.material.color = color;

        if (col is BoxCollider box)
        {
            _highlightBoxObj.transform.position = box.transform.TransformPoint(box.center);
            _highlightBoxObj.transform.rotation = box.transform.rotation;

            Vector3 lossy = box.transform.lossyScale;
            _highlightBoxObj.transform.localScale = new Vector3(
                Mathf.Abs(box.size.x * lossy.x),
                Mathf.Abs(box.size.y * lossy.y),
                Mathf.Abs(box.size.z * lossy.z)
            ) * 1.02f;
        }
        else
        {
            Bounds b = col.bounds;
            _highlightBoxObj.transform.position = b.center;
            _highlightBoxObj.transform.rotation = Quaternion.identity;
            _highlightBoxObj.transform.localScale = b.size * 1.02f;
        }
    }

    private void HideHighlight()
    {
        _isHoveringExitZone = false;
        if (_highlightBoxObj != null && _highlightBoxObj.activeSelf)
        {
            _highlightBoxObj.SetActive(false);
        }
    }

    private void StartInspect(InspectableItem inspectData, Collider hitCol)
    {
        HideHighlight();

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

        // Если в руках сам блокнот — разрешаем листать и вырывать страницы
        if (_currentItem.GetComponentInChildren<PhysicalReport>() != null)
        {
            HandleNotebookControls();
        }

        // ЛОГИКА ЗАРИСОВКИ И ЧЕК-ЛИСТА УЛИКИ
        EvidencePhoto photo = _currentItem.GetComponentInChildren<EvidencePhoto>();
        if (photo != null && Keyboard.current != null)
        {
            bool canSketch = !photo.hasRealAnomaly || photo.isAnomalous;

            if (canSketch)
            {
                // [Space] — сделать зарисовку и добавить страницу в блокнот
                if (!photo.isSketched && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    photo.MakeSketch();
                }
                // После зарисовки можно заполнять чек-лист (Q / W / E / R)
                else if (photo.isSketched && photo.currentState == EvidencePhoto.DecisionState.Approved)
                {
                    if (Keyboard.current.qKey.wasPressedThisFrame) photo.CycleForm();
                    else if (Keyboard.current.wKey.wasPressedThisFrame) photo.CycleTexture();
                    else if (Keyboard.current.eKey.wasPressedThisFrame) photo.CycleCore();
                    else if (Keyboard.current.rKey.wasPressedThisFrame) photo.CycleBehavior();
                }
            }
        }

        // Выход из осмотра по ПКМ или Esc
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
        if (_isHoveringExitZone)
        {
            GUIStyle exitStyle = new GUIStyle();
            exitStyle.fontSize = 28;
            exitStyle.normal.textColor = new Color(1f, 0.3f, 0.3f, 0.95f);
            exitStyle.alignment = TextAnchor.LowerCenter;

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height - 30), "Вы хотите выйти из игры?", exitStyle);
        }

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

            GUIStyle zoomStyle = new GUIStyle();
            zoomStyle.fontSize = 28;
            zoomStyle.normal.textColor = new Color(1f, 0.2f, 0.2f, 0.8f);
            zoomStyle.alignment = TextAnchor.LowerCenter;

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height - 20), $"ZOOM: {zoomPercent}%", zoomStyle);

            if (_currentItem != null)
            {
                EvidencePhoto photo = _currentItem.GetComponentInChildren<EvidencePhoto>();
                if (photo != null)
                {
                    GUIStyle infoStyle = new GUIStyle();
                    infoStyle.fontSize = 20;
                    infoStyle.alignment = TextAnchor.UpperCenter;

                    bool canSketch = !photo.hasRealAnomaly || photo.isAnomalous;

                    if (!canSketch)
                    {
                        infoStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.75f);
                        GUI.Label(new Rect(0, 30, Screen.width, 80),
                            $"Улика: {photo.photoName}\nВидимых отклонений пока не наблюдается.", infoStyle);
                    }
                    else if (!photo.isSketched)
                    {
                        infoStyle.normal.textColor = Color.yellow;
                        GUI.Label(new Rect(0, 30, Screen.width, 80),
                            $"ОБНАРУЖЕНА УЛИКА: {photo.photoName}\nНажмите [SPACE], чтобы сделать зарисовку в блокнот", infoStyle);
                    }
                    else if (photo.currentState == EvidencePhoto.DecisionState.Discarded)
                    {
                        infoStyle.normal.textColor = new Color(1f, 0.45f, 0.45f);
                        GUI.Label(new Rect(0, 30, Screen.width, 80),
                            $"Улика: {photo.photoName}\nСТАТУС: СТРАНИЦА ВЫРВАНА ИЗ БЛОКНОТА", infoStyle);
                    }
                    else
                    {
                        infoStyle.normal.textColor = Color.green;
                        string line1 = $"[Q] Форма: {EvidencePhoto.GetFormText(photo.selectedForm)}   |   [W] Поверхность: {EvidencePhoto.GetTextureText(photo.selectedTexture)}";
                        string line2 = $"[E] Ядро: {EvidencePhoto.GetCoreText(photo.selectedCore)}   |   [R] Поведение: {EvidencePhoto.GetBehaviorText(photo.selectedBehavior)}";

                        GUI.Label(new Rect(0, 20, Screen.width, 130),
                            $"ЗАРИСОВАНО В БЛОКНОТ: {photo.photoName}\n{line1}\n{line2}", infoStyle);
                    }
                }
            }
        }
    }
}