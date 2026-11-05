using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PhysicalReport : MonoBehaviour
{
    public static PhysicalReport Instance;

    [Header("Настройки визуала")]
    [Tooltip("Ссылка на компонент текста (TextMeshPro) на модели бланка отчета")]
    public TMP_Text reportTextComponent;

    // Список улик, которые игрок решил внести в отчет
    private List<EvidencePhoto> _approvedEvidence = new List<EvidencePhoto>();

    private void Awake()
    {
        Instance = this;

        // Если забыл перетащить компонент текста в Инспекторе — найдем его сами на объекте или дочерних
        if (reportTextComponent == null)
        {
            reportTextComponent = GetComponentInChildren<TMP_Text>();
        }

        UpdatePaperText();
    }

    // Добавление улики в отчет (или обновление, если уже там)
    public void ApproveEvidence(EvidencePhoto photo)
    {
        if (photo == null) return;

        if (!_approvedEvidence.Contains(photo))
        {
            _approvedEvidence.Add(photo);
        }
        UpdatePaperText();
    }

    // Удаление улики из отчета (когда игрок передумал и нажал Backspace)
    public void RemoveEvidence(EvidencePhoto photo)
    {
        if (photo == null) return;

        if (_approvedEvidence.Contains(photo))
        {
            _approvedEvidence.Remove(photo);
        }
        UpdatePaperText();
    }

    // Полная перерисовка текста на физическом листе бумаги
    public void UpdatePaperText()
    {
        if (reportTextComponent == null)
        {
            Debug.LogWarning("[PhysicalReport] Не назначен reportTextComponent!");
            return;
        }

        string newText = "ОТЧЕТ ОБ ИНЦИДЕНТЕ\n====================\n\n";
        int anomaliesFound = 0;

        if (_approvedEvidence.Count == 0)
        {
            newText += "Зафиксированных аномалий нет.\n\n";
        }
        else
        {
            foreach (var ev in _approvedEvidence)
            {
                if (ev != null && ev.isAnomalous)
                {
                    anomaliesFound++;
                    newText += $"- Улика: {ev.photoName}\n";
                    newText += $"  [!] ВИД АНОМАЛИИ: {ev.anomalyDescription}\n\n";
                }
            }
        }

        newText += $"====================\nИТОГО АНОМАЛИЙ: {anomaliesFound}";

        reportTextComponent.text = newText;
        reportTextComponent.ForceMeshUpdate(); // Принудительно обновляем 3D-меш текста в том же кадре
    }

    // Полезный метод на будущее для ветки с существом из шкафа:
    public int GetApprovedAnomaliesCount() => _approvedEvidence.Count;
}