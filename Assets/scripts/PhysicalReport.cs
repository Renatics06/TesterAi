using System.Collections.Generic;
using UnityEngine;

public class PhysicalReport : MonoBehaviour
{
    public static PhysicalReport Instance;

    [Header("Префаб листика и точка спавна в книге")]
    [Tooltip("Перетащи сюда свой префаб страницы со скриптом ReportPage")]
    public ReportPage pagePrefab;

    [Tooltip("Пустышка ВНУТРИ объекта блокнота (с InspectableItem), где будут появляться листы")]
    public Transform pagesAnchor;

    [Tooltip("Доп. множитель размера страницы, если нужно подогнать под размер книги")]
    public float pageScaleMultiplier = 1f;

    [Header("Стартовые страницы (оставь пустыми, если хочешь автосоздание из Page Prefab)")]
    public GameObject customPage1Corporate;
    public GameObject customPage2Notes;

    [Header("Текст для стартовых страниц (если поля выше пустые)")]
    [TextArea(4, 6)]
    public string corporateHeader =
        "ЕВРОПЕЙСКИЙ ЦЕНТР АНОМАЛЬНЫХ ИССЛЕДОВАНИЙ\n" +
        "ПРОТОКОЛ ПОЛЕВОЙ ФИКСАЦИИ\n\n" +
        "Прямой визуальный контакт с образцами запрещен.\n" +
        "Фиксируйте аномалии исключительно методом зарисовки.\n\n" +
        "[D] — Следующая страница";

    [TextArea(5, 8)]
    public string investigatorNotes =
        "МАТЕРИАЛЫ ДЕЛА (ЗАПИСИ ПРЕДШЕСТВЕННИКА):\n" +
        "[Здесь позже будет описание текущего дела и заметки прошлого следователя]\n\n" +
        "[A] / [D] — Листать страницы";

    private List<GameObject> _allPageObjects = new List<GameObject>();
    private List<ReportPage> _evidencePages = new List<ReportPage>();
    private int _currentPageIndex = 0;

    private void Awake()
    {
        Instance = this;
        if (pagesAnchor == null) pagesAnchor = transform;

        InitializeStaticPages();
        ShowOnlyCurrentPage();
    }

    private void InitializeStaticPages()
    {
        _allPageObjects.Clear();

        // СТРАНИЦА 1: Корпоративное приветствие
        if (customPage1Corporate != null)
        {
            _allPageObjects.Add(PrepareCustomPage(customPage1Corporate, "Page_01_Corporate"));
        }
        else if (pagePrefab != null)
        {
            ReportPage p1 = SpawnPageObject("Page_01_Corporate");
            p1.SetupStaticText(corporateHeader);
            _allPageObjects.Add(p1.gameObject);
        }
        else
        {
            Debug.LogError("[PhysicalReport] Не назначен Page Prefab — начальные страницы не созданы!");
        }

        // СТРАНИЦА 2: Записки следователя / описание дела
        if (customPage2Notes != null)
        {
            _allPageObjects.Add(PrepareCustomPage(customPage2Notes, "Page_02_CaseNotes"));
        }
        else if (pagePrefab != null)
        {
            ReportPage p2 = SpawnPageObject("Page_02_CaseNotes");
            p2.SetupStaticText(investigatorNotes);
            _allPageObjects.Add(p2.gameObject);
        }

        Debug.Log($"[БЛОКНОТ] Инициализировано стартовых страниц: {_allPageObjects.Count}");
    }

    private GameObject PrepareCustomPage(GameObject customObj, string fallbackName)
    {
        // Если в поле перетащили префаб из папки Project (не со сцены) — инстанцируем его
        if (!customObj.scene.IsValid())
        {
            GameObject spawned = Instantiate(customObj, pagesAnchor.position, pagesAnchor.rotation);
            spawned.name = fallbackName;
            spawned.transform.localScale = customObj.transform.localScale * pageScaleMultiplier;
            spawned.transform.SetParent(pagesAnchor, true);
            return spawned;
        }

        // Если это объект со сцены — убеждаемся, что он привязан к якорю и полетит за блокнотом
        if (customObj.transform.parent != pagesAnchor)
        {
            customObj.transform.SetParent(pagesAnchor, true);
        }
        return customObj;
    }

    public void AddOrUpdatePage(EvidencePhoto photo)
    {
        if (photo == null) return;

        if (pagePrefab == null)
        {
            Debug.LogError("[PhysicalReport] Не назначен Page Prefab в Инспекторе!");
            return;
        }

        int existingIndex = _evidencePages.FindIndex(p => p.LinkedEvidence == photo);
        if (existingIndex != -1)
        {
            _evidencePages[existingIndex].Refresh(existingIndex + 1);
            _currentPageIndex = 2 + existingIndex;
            ShowOnlyCurrentPage();
            return;
        }

        int newNumber = _evidencePages.Count + 1;
        ReportPage newPage = SpawnPageObject($"Page_Evidence_{newNumber}_{photo.photoName}");
        newPage.SetupEvidence(photo, newNumber);

        photo.currentState = EvidencePhoto.DecisionState.Approved;

        _evidencePages.Add(newPage);
        _allPageObjects.Add(newPage.gameObject);

        _currentPageIndex = _allPageObjects.Count - 1;
        ShowOnlyCurrentPage();
    }

    public void DiscardCurrentPage()
    {
        if (_currentPageIndex < 2) return;

        int evidenceIndex = _currentPageIndex - 2;
        if (evidenceIndex < 0 || evidenceIndex >= _evidencePages.Count) return;

        ReportPage pageToRemove = _evidencePages[evidenceIndex];
        if (pageToRemove.LinkedEvidence != null)
        {
            pageToRemove.LinkedEvidence.currentState = EvidencePhoto.DecisionState.Discarded;
        }

        _evidencePages.RemoveAt(evidenceIndex);
        _allPageObjects.RemoveAt(_currentPageIndex);

        Destroy(pageToRemove.gameObject);
        UpdateNotebookText();

        _currentPageIndex = Mathf.Clamp(_currentPageIndex - 1, 0, _allPageObjects.Count - 1);
        ShowOnlyCurrentPage();
        Debug.Log("[БЛОКНОТ] Лист с уликой вырван и выброшен.");
    }

    public void NextPage()
    {
        if (_allPageObjects.Count == 0) return;
        _currentPageIndex = Mathf.Min(_currentPageIndex + 1, _allPageObjects.Count - 1);
        ShowOnlyCurrentPage();
        Debug.Log($"[БЛОКНОТ] Страница {_currentPageIndex + 1} / {_allPageObjects.Count}");
    }

    public void PrevPage()
    {
        if (_allPageObjects.Count == 0) return;
        _currentPageIndex = Mathf.Max(_currentPageIndex - 1, 0);
        ShowOnlyCurrentPage();
        Debug.Log($"[БЛОКНОТ] Страница {_currentPageIndex + 1} / {_allPageObjects.Count}");
    }

    public void UpdateNotebookText()
    {
        for (int i = 0; i < _evidencePages.Count; i++)
        {
            if (_evidencePages[i] != null)
            {
                _evidencePages[i].Refresh(i + 1);
            }
        }
    }

    private ReportPage SpawnPageObject(string objName)
    {
        // Спавним в мировых координатах якоря, сохраняя оригинальный размер префаба
        ReportPage page = Instantiate(pagePrefab, pagesAnchor.position, pagesAnchor.rotation);
        page.name = objName;
        page.transform.localScale = pagePrefab.transform.localScale * pageScaleMultiplier;

        // Крепим к блокноту с worldPositionStays = true, чтобы scale блокнота не сплющивал лист
        page.transform.SetParent(pagesAnchor, true);
        return page;
    }

    private void ShowOnlyCurrentPage()
    {
        for (int i = 0; i < _allPageObjects.Count; i++)
        {
            if (_allPageObjects[i] != null)
            {
                _allPageObjects[i].SetActive(i == _currentPageIndex);
            }
        }
    }
}