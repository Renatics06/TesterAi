using System;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalReport : MonoBehaviour
{
    public static PhysicalReport Instance;

    [Header("Префаб листика и точка спавна в книге")]
    [Tooltip("Префаб страницы из папки Project со скриптом ReportPage")]
    public ReportPage pagePrefab;

    [Tooltip("Пустышка со СЦЕНЫ (внутри объекта с InspectableItem), где появляются листы")]
    public Transform pagesAnchor;

    [Tooltip("Доп. множитель размера страницы")]
    public float pageScaleMultiplier = 1f;

    [Header("Категории отчета (можно добавлять новые через '+')")]
    public List<ReportCategoryDefinition> categories = new List<ReportCategoryDefinition>();

    [Header("Стартовые страницы (оставь пустыми для автосоздания из Page Prefab)")]
    public GameObject customPage1Corporate;
    public GameObject customPage2Notes;

    [Header("Текст для стартовых страниц")]
    [TextArea(4, 6)]
    public string corporateHeader =
        "ЕВРОПЕЙСКИЙ ЦЕНТР АНОМАЛЬНЫХ ИССЛЕДОВАНИЙ\n" +
        "ПРОТОКОЛ ПОЛЕВОЙ ФИКСАЦИИ\n\n" +
        "Прямой визуальный контакт с образцами запрещен.\n" +
        "Фиксируйте аномалии методом зарисовки.\n\n" +
        "[D] — Далее | [N] — Новый лист";

    [TextArea(5, 8)]
    public string investigatorNotes =
        "МАТЕРИАЛЫ ДЕЛА (ЗАПИСИ ПРЕДШЕСТВЕННИКА):\n" +
        "[Описание текущего дела и заметки прошлого следователя]\n\n" +
        "[A]/[D] — Листать | [N] — Создать новый лист";

    // Событие для подписки из кода (например, для реакции комнаты/сущности/УФ-ивента)
    public event Action<ReportPage> OnPageConfirmed;

    // Банк зарисовок, которые игрок уже сделал на уликах
    public List<EvidencePhoto> UnlockedSketches { get; private set; } = new List<EvidencePhoto>();

    private List<GameObject> _allPageObjects = new List<GameObject>();
    private List<ReportPage> _builderPages = new List<ReportPage>();
    private int _currentPageIndex = 0;

    // Автозаполнение 3 базовых категорий при добавлении компонента в Unity
    private void Reset()
    {
        PopulateDefaultCategories();
    }

    private void Awake()
    {
        Instance = this;

        if (categories == null || categories.Count == 0)
        {
            PopulateDefaultCategories();
        }

        ValidatePagesAnchor();
        InitializeStaticPages();
        ShowOnlyCurrentPage();
    }

    [ContextMenu("Заполнить 3 базовые категории")]
    public void PopulateDefaultCategories()
    {
        categories = new List<ReportCategoryDefinition>
        {
            // 1. Признак присутствия аномальной сущности
            new ReportCategoryDefinition
            {
                categoryId = "entity",
                categoryTitle = "Признак присутствия сущности",
                fields = new List<ChecklistFieldDefinition>
                {
                    new ChecklistFieldDefinition
                    {
                        fieldName = "Форма",
                        options = new List<string> { "Не выбрано", "Норма", "Искривление", "Фрагментация" }
                    },
                    new ChecklistFieldDefinition
                    {
                        fieldName = "Поверхность",
                        options = new List<string> { "Не выбрано", "Сухая", "Маслянистая пленка", "Плесень / Налет", "Следы ожога" }
                    },
                    new ChecklistFieldDefinition
                    {
                        fieldName = "Ядро",
                        options = new List<string> { "Не выбрано", "Силуэт / Фигура", "Символы / Линии", "Органика" }
                    },
                    new ChecklistFieldDefinition
                    {
                        fieldName = "Поведение",
                        options = new List<string> { "Не выбрано", "Статичное", "Психогенное", "Агрессивное появление" }
                    }
                }
            },

            // 2. Аномальный след (УФ / лунный свет / фиолетовая жидкость)
            new ReportCategoryDefinition
            {
                categoryId = "uv_trace",
                categoryTitle = "Аномальный след",
                fields = new List<ChecklistFieldDefinition>
                {
                    new ChecklistFieldDefinition
                    {
                        fieldName = "Условие проявления",
                        options = new List<string> { "Не выбрано", "Ультрафиолет", "Лунный свет", "УФ + Лунный свет" }
                    },
                    new ChecklistFieldDefinition
                    {
                        fieldName = "Вещество / След",
                        options = new List<string> { "Не выбрано", "Фиолетовая жидкость", "Люминесцентный налет", "Иной остаток" }
                    },
                    new ChecklistFieldDefinition
                    {
                        fieldName = "Реакция следа",
                        options = new List<string> { "Не выбрано", "Пассивный след", "Меняет оттенок", "Активное свечение" }
                    }
                }
            },

            // 3. Улика без следов аномалий (обычная кровь и бытовые следы)
            new ReportCategoryDefinition
            {
                categoryId = "normal",
                categoryTitle = "Без следов аномалий",
                fields = new List<ChecklistFieldDefinition>
                {
                    new ChecklistFieldDefinition
                    {
                        fieldName = "Бытовой след",
                        options = new List<string> { "Не выбрано", "Кровь", "Пятно / Грязь", "Брак печати", "Механическое повреждение" }
                    },
                    new ChecklistFieldDefinition
                    {
                        fieldName = "Заключение",
                        options = new List<string> { "Не выбрано", "Списать как норму", "Требует доп. осмотра" }
                    }
                }
            }
        };
    }

    // Сохранение зарисовки в банк при нажатии [Space] на улике
    public void RegisterSketch(EvidencePhoto photo)
    {
        if (photo == null) return;

        if (!UnlockedSketches.Contains(photo))
        {
            UnlockedSketches.Add(photo);
        }

        // Обновляем открытую страницу, если на ней висело "Нет зарисовок"
        ReportPage active = GetActiveBuilderPage();
        if (active != null) active.RefreshVisuals();
    }

    // [N] — Создать новый пустой лист-билдер в конце блокнота
    public void CreateNewPage()
    {
        if (pagePrefab == null)
        {
            Debug.LogError("[PhysicalReport] Не назначен Page Prefab!");
            return;
        }

        int newNum = _builderPages.Count + 1;
        ReportPage newPage = SpawnPageObject($"Page_Builder_{newNum}");
        newPage.SetupAsBuilder(this, newNum);

        _builderPages.Add(newPage);
        _allPageObjects.Add(newPage.gameObject);

        _currentPageIndex = _allPageObjects.Count - 1;
        ShowOnlyCurrentPage();
        Debug.Log($"[БЛОКНОТ] Создан новый лист дела #{newNum}.");
    }

    // [Backspace] — Вырвать и выкинуть текущий лист
    public void DiscardCurrentPage()
    {
        if (_currentPageIndex < 2) return;

        int builderIndex = _currentPageIndex - 2;
        if (builderIndex < 0 || builderIndex >= _builderPages.Count) return;

        ReportPage pageToRemove = _builderPages[builderIndex];
        _builderPages.RemoveAt(builderIndex);
        _allPageObjects.RemoveAt(_currentPageIndex);

        Destroy(pageToRemove.gameObject);

        for (int i = 0; i < _builderPages.Count; i++)
        {
            if (_builderPages[i] != null)
                _builderPages[i].SetPageNumber(i + 1);
        }

        _currentPageIndex = Mathf.Clamp(_currentPageIndex - 1, 0, _allPageObjects.Count - 1);
        ShowOnlyCurrentPage();
    }

    // Управление активным листом-билдером
    public void MoveBuilderCursor(int delta) => GetActiveBuilderPage()?.MoveCursor(delta);
    public void CycleBuilderValue(int delta) => GetActiveBuilderPage()?.CycleActiveRow(delta);
    public void ToggleSignCurrentPage() => GetActiveBuilderPage()?.ToggleSign();

    public void NotifyPageSigned(ReportPage page)
    {
        OnPageConfirmed?.Invoke(page);
    }

    public ReportPage GetActiveBuilderPage()
    {
        if (_currentPageIndex < 2) return null;
        int idx = _currentPageIndex - 2;
        if (idx < 0 || idx >= _builderPages.Count) return null;
        return _builderPages[idx];
    }

    public void NextPage()
    {
        if (_allPageObjects.Count == 0) return;
        _currentPageIndex = Mathf.Min(_currentPageIndex + 1, _allPageObjects.Count - 1);
        ShowOnlyCurrentPage();
    }

    public void PrevPage()
    {
        if (_allPageObjects.Count == 0) return;
        _currentPageIndex = Mathf.Max(_currentPageIndex - 1, 0);
        ShowOnlyCurrentPage();
    }

    public string GetPageCounterLabel() => $"Страница {_currentPageIndex + 1} / {_allPageObjects.Count}";

    private void ValidatePagesAnchor()
    {
        if (pagesAnchor == null || !pagesAnchor.gameObject.scene.IsValid())
        {
            if (pagesAnchor != null)
                Debug.LogWarning($"[PhysicalReport] В Pages Anchor попал префаб ('{pagesAnchor.name}'), привязываю к '{name}'.");
            pagesAnchor = transform;
        }

        InspectableItem inspectable = GetComponentInParent<InspectableItem>();
        if (inspectable == null) inspectable = GetComponentInChildren<InspectableItem>();

        if (inspectable != null && !pagesAnchor.IsChildOf(inspectable.transform))
        {
            Debug.LogWarning($"[PhysicalReport] '{pagesAnchor.name}' не внутри объекта с InspectableItem ('{inspectable.name}')!");
        }
    }

    private void InitializeStaticPages()
    {
        _allPageObjects.Clear();

        if (customPage1Corporate != null)
            _allPageObjects.Add(PrepareCustomPage(customPage1Corporate, "Page_01_Corporate"));
        else if (pagePrefab != null)
        {
            ReportPage p1 = SpawnPageObject("Page_01_Corporate");
            p1.SetupStaticText(corporateHeader);
            _allPageObjects.Add(p1.gameObject);
        }

        if (customPage2Notes != null)
            _allPageObjects.Add(PrepareCustomPage(customPage2Notes, "Page_02_CaseNotes"));
        else if (pagePrefab != null)
        {
            ReportPage p2 = SpawnPageObject("Page_02_CaseNotes");
            p2.SetupStaticText(investigatorNotes);
            _allPageObjects.Add(p2.gameObject);
        }
    }

    private GameObject PrepareCustomPage(GameObject customObj, string fallbackName)
    {
        ValidatePagesAnchor();

        if (!customObj.scene.IsValid())
        {
            GameObject spawned = Instantiate(customObj, pagesAnchor.position, pagesAnchor.rotation);
            spawned.name = fallbackName;
            spawned.transform.localScale = customObj.transform.localScale * pageScaleMultiplier;
            spawned.transform.SetParent(pagesAnchor, true);
            return spawned;
        }

        if (customObj.transform.parent != pagesAnchor)
            customObj.transform.SetParent(pagesAnchor, true);

        return customObj;
    }

    private ReportPage SpawnPageObject(string objName)
    {
        ValidatePagesAnchor();

        ReportPage page = Instantiate(pagePrefab, pagesAnchor.position, pagesAnchor.rotation);
        page.name = objName;
        page.transform.localScale = pagePrefab.transform.localScale * pageScaleMultiplier;
        page.transform.SetParent(pagesAnchor, true);
        return page;
    }

    private void ShowOnlyCurrentPage()
    {
        for (int i = 0; i < _allPageObjects.Count; i++)
        {
            if (_allPageObjects[i] != null)
                _allPageObjects[i].SetActive(i == _currentPageIndex);
        }
    }
}