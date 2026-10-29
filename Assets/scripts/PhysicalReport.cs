using UnityEngine;
using TMPro; // Библиотека для работы с нормальным текстом

public class PhysicalReport : MonoBehaviour
{
    // Синглтон, чтобы любая улика могла быстро найти отчет на столе
    public static PhysicalReport Instance;

    [Header("Настройки визуала")]
    [Tooltip("Ссылка на компонент TextMeshPro, который висит на модели бумаги")]
    public TextMeshPro reportTextComponent;

    private string _currentText = "ОТЧЕТ ОБ ИНЦИДЕНТЕ\n====================\n\n";
    private int _anomaliesFound = 0;

    private void Awake()
    {
        Instance = this;
        UpdatePaperText();
    }

    // Этот метод будут вызывать сами фотки, когда игрок их осматривает
    public void LogEvidence(string evidenceName, bool isAnomalous, string details)
    {
        _currentText += $"- Предмет: {evidenceName}\n";

        if (isAnomalous)
        {
            _anomaliesFound++;
            _currentText += $"  [!] ВЫЯВЛЕНА АНОМАЛИЯ: {details}\n\n";
        }
        else
        {
            _currentText += $"  Статус: Норма. Без отклонений.\n\n";
        }

        UpdatePaperText();
    }

    private void UpdatePaperText()
    {
        if (reportTextComponent != null)
        {
            reportTextComponent.text = _currentText + $"ИТОГО АНОМАЛИЙ: {_anomaliesFound}";
        }
    }
}