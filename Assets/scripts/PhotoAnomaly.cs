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
    public float fadeDuration = 2.0f; // Время затемнения (в секундах)

    private MeshRenderer _renderer;
    private bool _isChanged = false;

    private void Start()
    {
        _renderer = GetComponent<MeshRenderer>();

        // Ставим обычную фотку при старте
        if (_renderer != null && normalPhoto != null)
        {
            _renderer.material.mainTexture = normalPhoto;
        }

        // Подписываем фотку на новости от персонажа
        if (CharacterStateManager.Instance != null)
        {
            CharacterStateManager.Instance.OnStateChanged += CheckCharacterState;
        }
    }

    private void OnDestroy()
    {
        // Обязательно отписываемся при уничтожении объекта, чтобы не было ошибок памяти
        if (CharacterStateManager.Instance != null)
        {
            CharacterStateManager.Instance.OnStateChanged -= CheckCharacterState;
        }
    }

    private void CheckCharacterState()
    {
        if (_isChanged) return; // Если уже превратилась, больше не трогаем

        // Если у игрока появилось нужное состояние — запускаем магию
        if (CharacterStateManager.Instance.HasState(requiredState))
        {
            _isChanged = true;
            StartCoroutine(ChangePhotoRoutine());
        }
    }

    private IEnumerator ChangePhotoRoutine()
    {
        if (_renderer == null) yield break;

        Material mat = _renderer.material;

        // В URP цвет называется _BaseColor, в стандарте _Color. Проверяем, что есть.
        string colorProp = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

        Color originalColor = mat.GetColor(colorProp); // Исходный цвет (обычно белый)
        Color darkColor = Color.black;                 // Черный

        float elapsed = 0f;

        // 1. Плавное угасание в темноту
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            mat.SetColor(colorProp, Color.Lerp(originalColor, darkColor, elapsed / fadeDuration));
            yield return null;
        }
        mat.SetColor(colorProp, darkColor);

        // 2. Тихая подмена текстуры, пока фото черное
        if (anomalousPhoto != null)
        {
            // В URP текстура называется _BaseMap, в стандарте _MainTex
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", anomalousPhoto);
            else mat.mainTexture = anomalousPhoto;
        }

        yield return new WaitForSeconds(0.5f); // Пауза в темноте

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