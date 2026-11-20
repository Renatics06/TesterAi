using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PhysicalReport : MonoBehaviour
{
    public static PhysicalReport Instance;

    [Header("Ссылки на элементы 3D-блокнота")]
    [Tooltip("Основной текстовый компонент на странице отчета (TextMeshPro)")]
    public TMP_Text notebookTextComponent;

    [Tooltip("UI Image для вывода текстуры-зарисовки текущей страницы")]
    public RawImage sketchDisplayImage;

    [Header("Корпоративный контент (Статичные страницы)")]
    [TextArea(4, 6)]
    public string corporateHeader = "ЕВРОПЕЙСКИЙ ЦЕНТР АНОМАЛЬНЫХ ИССЛЕДОВАНИЙ (ЕЦАИ)\nДЕПАРТАМЕНТ КОНТРОЛЯ УЛИК И СРЕДЫ\n\nВНИМАНИЕ: Все обнаруженные аномалии подлежат строгому учету. Прямой зрительный контакт с незащищенными образцами запрещен. Используйте метод полевой зарисовки.";

    [TextArea(5, 8)]
    public string investigatorNotes = "ЗАПИСКИ ПРЕДШЕСТВЕННИКА (СЛЕДОВАТЕЛЬ Р. ВАН ТЕЛЬ):\n'14 ноября. Они здесь везде. Этот чемодан... нельзя открывать до конца. Тот, что в шкафу, стучит по ночам. Если руководство спросит про пятна на фотографиях — пишите, что это влажность. Делайте только скетчи, не смотрите на них долго...'";

    private List<EvidencePhoto> _notebookPages = new List<EvidencePhoto>();
    private int _currentPageIndex = 0;

    private void Awake()
    {
        Instance = this;
        if (notebookTextComponent == null)
            notebookTextComponent = GetComponentInChildren<TMP_Text>();

        UpdateNotebookText();
    }

    public void AddOrUpdatePage(EvidencePhoto photo)
    {
        if (photo == null) return;

        if (!_notebookPages.Contains(photo))
        {
            _notebookPages.Add(photo);
            photo.currentState = EvidencePhoto.DecisionState.Approved;
            _currentPageIndex = _notebookPages.Count + 1;
        }
        UpdateNotebookText();
    }

    public void DiscardCurrentPage()
    {
        if (_currentPageIndex < 2 || _notebookPages.Count == 0) return;

        int evidenceIndex = _currentPageIndex - 2;
        if (evidenceIndex < _notebookPages.Count)
        {
            EvidencePhoto photo = _notebookPages[evidenceIndex];
            photo.currentState = EvidencePhoto.DecisionState.Discarded;
            _notebookPages.RemoveAt(evidenceIndex);

            _currentPageIndex = Mathf.Clamp(_currentPageIndex - 1, 0, _notebookPages.Count + 1);
            UpdateNotebookText();
            Debug.Log("[БЛОКНОТ] Страница с уликой была вырвана и выброшена.");
        }
    }

    public void NextPage()
    {
        int maxPages = _notebookPages.Count + 1;
        _currentPageIndex = Mathf.Min(_currentPageIndex + 1, maxPages);
        UpdateNotebookText();
    }

    public void PrevPage()
    {
        _currentPageIndex = Mathf.Max(_currentPageIndex - 1, 0);
        UpdateNotebookText();
    }

    public void UpdateNotebookText()
    {
        if (notebookTextComponent == null) return;

        string pageContent = "";

        if (_currentPageIndex == 0)
        {
            pageContent = $"[СТРАНИЦА 1 / ОБЩЕЕ]\n\n{corporateHeader}\n\n--- [Листать далее ->] ---";
            HideSketchUI();
        }
        else if (_currentPageIndex == 1)
        {
            pageContent = $"[СТРАНИЦА 2 / ЖУРНАЛ СЛЕДОВАТЕЛЯ]\n\n{investigatorNotes}\n\n--- [<-- Листать назад / Далее ->] ---";
            HideSketchUI();
        }
        else
        {
            int evidenceIndex = _currentPageIndex - 2;
            if (evidenceIndex < _notebookPages.Count)
            {
                EvidencePhoto ev = _notebookPages[evidenceIndex];

                pageContent = $"[ДЕЛО УЛИКА #{evidenceIndex + 1}: {ev.photoName}]\n";
                pageContent += $" СТАТУС СКЕТЧА: {(ev.isSketched ? "[ЗАРИСОВАНО]" : "[ПУСТО]")}\n\n";

                if (ev.isSketched)
                {
                    pageContent += $"• Форма: {EvidencePhoto.GetFormText(ev.selectedForm)}\n";
                    pageContent += $"• Поверхность: {EvidencePhoto.GetTextureText(ev.selectedTexture)}\n";
                    pageContent += $"• Ядро: {EvidencePhoto.GetCoreText(ev.selectedCore)}\n";
                    pageContent += $"• Поведение: {EvidencePhoto.GetBehaviorText(ev.selectedBehavior)}\n\n";
                    pageContent += "[BACKSPACE] — Вырвать/выбросить страницу из блокнота";
                }

                ShowSketchUI(ev.GetCurrentSketchTexture());
            }
        }

        notebookTextComponent.text = pageContent;
        notebookTextComponent.ForceMeshUpdate();
    }

    private void ShowSketchUI(Texture tex)
    {
        if (sketchDisplayImage != null)
        {
            sketchDisplayImage.gameObject.SetActive(true);
            sketchDisplayImage.texture = tex;
        }
    }

    private void HideSketchUI()
    {
        if (sketchDisplayImage != null)
        {
            sketchDisplayImage.gameObject.SetActive(false);
        }
    }
}