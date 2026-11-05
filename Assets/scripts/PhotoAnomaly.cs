using System.Collections;
using UnityEngine;

public class PhotoAnomaly : MonoBehaviour
{
    [Header("Настройки текстур")]
    public Texture normalPhoto;
    public Texture anomalousPhoto;

    [Header("Триггер")]
    [Tooltip("Какое состояние нужно получить, чтобы фотка изменилась?")]
    public string requiredState = "Smoked";

    [Header("Анимация")]
    public float fadeDuration = 2.0f;

    private MeshRenderer _renderer;
    private EvidencePhoto _evidencePhoto;
    private bool _isChanged = false;

    private void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        _evidencePhoto = GetComponent<EvidencePhoto>();
        if (_evidencePhoto == null) _evidencePhoto = GetComponentInParent<EvidencePhoto>();

        // До проявления аномалии фотка строго обычная (выбор заблокирован)
        if (_evidencePhoto != null)
        {
            _evidencePhoto.isAnomalous = false;
            _evidencePhoto.currentState = EvidencePhoto.DecisionState.Pending;
        }

        // Ставим обычную текстуру при старте
        if (_renderer != null && normalPhoto != null)
        {
            Material mat = _renderer.material;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", normalPhoto);
            else mat.mainTexture = normalPhoto;
        }

        // Подписываемся на состояния персонажа
        if (CharacterStateManager.Instance != null)
        {
            CharacterStateManager.Instance.OnStateChanged += CheckCharacterState;
            // Проверяем сразу при спавне (вдруг игрок покурил ДО открытия чемодана)
            CheckCharacterState();
        }
    }

    private void OnDestroy()
    {
        if (CharacterStateManager.Instance != null)
        {
            CharacterStateManager.Instance.OnStateChanged -= CheckCharacterState;
        }
    }

    private void CheckCharacterState()
    {
        if (_isChanged) return;

        if (CharacterStateManager.Instance != null && CharacterStateManager.Instance.HasState(requiredState))
        {
            _isChanged = true;
            StartCoroutine(ChangePhotoRoutine());
        }
    }

    private IEnumerator ChangePhotoRoutine()
    {
        if (_renderer == null) yield break;

        Material mat = _renderer.material;
        string colorProp = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

        Color originalColor = mat.GetColor(colorProp);
        Color darkColor = Color.black;

        float elapsed = 0f;

        // 1. Плавное угасание в темноту
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            mat.SetColor(colorProp, Color.Lerp(originalColor, darkColor, elapsed / fadeDuration));
            yield return null;
        }
        mat.SetColor(colorProp, darkColor);

        // 2. Подмена текстуры на аномальную и разблокировка выбора в EvidencePhoto
        if (anomalousPhoto != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", anomalousPhoto);
            else mat.mainTexture = anomalousPhoto;
        }

        // СООБЩАЕМ УЛИКЕ, ЧТО ОНА СТАЛА АНОМАЛЬНОЙ!
        if (_evidencePhoto != null)
        {
            _evidencePhoto.BecomeAnomalous();
        }

        yield return new WaitForSeconds(0.5f);

        // 3. Плавное проявление новой фотки
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            mat.SetColor(colorProp, Color.Lerp(darkColor, originalColor, elapsed / fadeDuration));
            yield return null;
        }
        mat.SetColor(colorProp, originalColor);
    }
}