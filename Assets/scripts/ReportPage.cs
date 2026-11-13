using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReportPage : MonoBehaviour
{
    [Header("Элементы на самом листике")]
    public TMP_Text pageText;
    public MeshRenderer sketchQuadRenderer;
    public RawImage sketchRawImage;
    public GameObject sketchContainer;

    [Tooltip("Опционально: текстура пустой рамки, пока игрок не выбрал зарисовку")]
    public Texture emptySketchPlaceholder;

    // Данные, собранные игроком на этом конкретном листе
    public bool IsBuilderPage { get; private set; } = false;
    public bool IsSigned { get; private set; } = false;
    public EvidencePhoto SelectedSketch { get; private set; }
    public int SelectedCategoryIndex { get; private set; } = 0;

    private List<int> _selectedOptionIndices = new List<int>();
    private int _cursorRow = 0; // 0 = Зарисовка, 1 = Категория, 2+ = Пункты выбранной категории
    private int _sketchIndex = -1; // -1 = Не выбрано
    private int _pageNumber = 1;

    private PhysicalReport _report;

    // Для стартовых статических страниц (приветствие и записки)
    public void SetupStaticText(string text)
    {
        IsBuilderPage = false;
        SetSketchVisible(false);

        if (pageText != null)
        {
            pageText.text = text;
            pageText.ForceMeshUpdate();
        }
    }

    // Инициализация нового пустого листа в режиме Билдера
    public void SetupAsBuilder(PhysicalReport report, int pageNumber)
    {
        _report = report;
        _pageNumber = pageNumber;
        IsBuilderPage = true;
        IsSigned = false;
        _cursorRow = 0;
        _sketchIndex = -1;
        SelectedSketch = null;
        SelectedCategoryIndex = 0;

        RebuildFieldSelectionArray();
        RefreshVisuals();
    }

    public void SetPageNumber(int pageNumber)
    {
        _pageNumber = pageNumber;
        if (IsBuilderPage) RefreshVisuals();
    }

    // Движение курсора '>' вверх/вниз (W / S)
    public void MoveCursor(int delta)
    {
        if (!IsBuilderPage || IsSigned) return;

        int totalRows = GetTotalRowsCount();
        if (totalRows <= 0) return;

        _cursorRow = (_cursorRow + delta + totalRows) % totalRows;
        RefreshVisuals();
    }

    // Переключение значения в текущей строке (Q / E)
    public void CycleActiveRow(int delta)
    {
        if (!IsBuilderPage || IsSigned || _report == null) return;

        if (_cursorRow == 0)
        {
            // Строка 0: Выбор зарисовки из банка зарисованных улик
            int count = _report.UnlockedSketches.Count;
            if (count == 0)
            {
                _sketchIndex = -1;
                SelectedSketch = null;
            }
            else
            {
                // Диапазон от -1 (Пусто) до count - 1
                int totalChoices = count + 1;
                int shifted = (_sketchIndex + 1 + delta + totalChoices) % totalChoices;
                _sketchIndex = shifted - 1;
                SelectedSketch = _sketchIndex >= 0 ? _report.UnlockedSketches[_sketchIndex] : null;
            }
        }
        else if (_cursorRow == 1)
        {
            // Строка 1: Выбор категории (1. Сущность, 2. След УФ/Луна, 3. Без аномалий, ...)
            int catCount = _report.categories.Count;
            if (catCount > 0)
            {
                SelectedCategoryIndex = (SelectedCategoryIndex + delta + catCount) % catCount;
                RebuildFieldSelectionArray();
            }
        }
        else
        {
            // Строки 2+: Динамические пункты выбранной категории
            int fieldIndex = _cursorRow - 2;
            ReportCategoryDefinition cat = GetCurrentCategory();
            if (cat != null && fieldIndex >= 0 && fieldIndex < cat.fields.Count)
            {
                int optCount = cat.fields[fieldIndex].options.Count;
                if (optCount > 0)
                {
                    _selectedOptionIndices[fieldIndex] = (_selectedOptionIndices[fieldIndex] + delta + optCount) % optCount;
                }
            }
        }

        RefreshVisuals();
    }

    // Подписать / снять подпись (Enter)
    public void ToggleSign()
    {
        if (!IsBuilderPage) return;

        IsSigned = !IsSigned;

        if (IsSigned)
        {
            ReportCategoryDefinition cat = GetCurrentCategory();
            if (cat != null)
            {
                cat.onPageSigned?.Invoke();
                Debug.Log($"[ОТЧЕТ] Лист #{_pageNumber} подписан. Категория: {cat.categoryTitle} ({cat.categoryId})");
            }
            _report?.NotifyPageSigned(this);
        }
        else
        {
            Debug.Log($"[ОТЧЕТ] Лист #{_pageNumber} возвращен в режим редактирования.");
        }

        RefreshVisuals();
    }

    public ReportCategoryDefinition GetCurrentCategory()
    {
        if (_report == null || _report.categories.Count == 0) return null;
        int idx = Mathf.Clamp(SelectedCategoryIndex, 0, _report.categories.Count - 1);
        return _report.categories[idx];
    }

    public string GetSelectedOptionValue(int fieldIndex)
    {
        ReportCategoryDefinition cat = GetCurrentCategory();
        if (cat == null || fieldIndex < 0 || fieldIndex >= cat.fields.Count) return "";
        if (fieldIndex >= _selectedOptionIndices.Count) return "";

        var options = cat.fields[fieldIndex].options;
        if (options.Count == 0) return "";

        int optIdx = Mathf.Clamp(_selectedOptionIndices[fieldIndex], 0, options.Count - 1);
        return options[optIdx];
    }

    private void RebuildFieldSelectionArray()
    {
        _selectedOptionIndices.Clear();
        ReportCategoryDefinition cat = GetCurrentCategory();
        if (cat != null)
        {
            for (int i = 0; i < cat.fields.Count; i++)
            {
                _selectedOptionIndices.Add(0);
            }
        }
        _cursorRow = Mathf.Clamp(_cursorRow, 0, Mathf.Max(0, GetTotalRowsCount() - 1));
    }

    private int GetTotalRowsCount()
    {
        ReportCategoryDefinition cat = GetCurrentCategory();
        int dynamicFields = cat != null ? cat.fields.Count : 0;
        return 2 + dynamicFields; // Зарисовка + Категория + Поля категории
    }

    public void RefreshVisuals()
    {
        if (!IsBuilderPage) return;

        // 1. Картинка в SketchQuad
        Texture tex = SelectedSketch != null ? SelectedSketch.GetCurrentSketchTexture() : emptySketchPlaceholder;
        if (tex != null)
        {
            ApplyTexture(tex);
            SetSketchVisible(true);
        }
        else
        {
            SetSketchVisible(false);
        }

        // 2. Текст билдера на листе
        if (pageText == null) return;

        string statusTag = IsSigned ? "[ПОДПИСАНО]" : "[ЧЕРНОВИК]";
        string content = $"ЛИСТ ДЕЛА #{_pageNumber}  {statusTag}\n";
        content += "================================\n";

        // Строка 0: Зарисовка
        string mark0 = (!IsSigned && _cursorRow == 0) ? "> " : "  ";
        string sketchLabel;
        if (SelectedSketch != null)
            sketchLabel = SelectedSketch.photoName;
        else if (_report != null && _report.UnlockedSketches.Count == 0)
            sketchLabel = "[Нет зарисовок — Space на улике]";
        else
            sketchLabel = "[Не выбрано (Q/E)]";

        content += $"{mark0}Зарисовка: {sketchLabel}\n";

        // Строка 1: Категория
        ReportCategoryDefinition cat = GetCurrentCategory();
        string mark1 = (!IsSigned && _cursorRow == 1) ? "> " : "  ";
        string catTitle = cat != null ? cat.categoryTitle : "[Нет категорий]";
        content += $"{mark1}Категория: {catTitle}\n";
        content += "--------------------------------\n";

        // Строки 2+: Динамические поля выбранной категории
        if (cat != null)
        {
            for (int i = 0; i < cat.fields.Count; i++)
            {
                int rowIndex = 2 + i;
                string mark = (!IsSigned && _cursorRow == rowIndex) ? "> " : "  ";
                string fieldName = cat.fields[i].fieldName;
                string val = GetSelectedOptionValue(i);
                content += $"{mark}{fieldName}: {val}\n";
            }
        }

        content += "--------------------------------\n";
        if (IsSigned)
            content += "[Enter] Редактировать | [Backspace] Вырвать";
        else
            content += "[W/S] Строка | [Q/E] Выбор | [Enter] Подписать";

        pageText.text = content;
        pageText.ForceMeshUpdate();
    }

    private void ApplyTexture(Texture tex)
    {
        if (sketchQuadRenderer != null)
        {
            Material mat = sketchQuadRenderer.material;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            else mat.mainTexture = tex;
        }

        if (sketchRawImage != null)
        {
            sketchRawImage.texture = tex;
        }
    }

    private void SetSketchVisible(bool visible)
    {
        if (sketchContainer != null) sketchContainer.SetActive(visible);
        if (sketchQuadRenderer != null) sketchQuadRenderer.gameObject.SetActive(visible);
        if (sketchRawImage != null) sketchRawImage.gameObject.SetActive(visible);
    }
}