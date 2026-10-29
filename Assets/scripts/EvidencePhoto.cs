using UnityEngine;

public class EvidencePhoto : MonoBehaviour
{
    [Header("Данные для отчета")]
    public string photoName = "Фотография двора";

    [Header("Тип улики")]
    public bool isAnomalous = false; // Поставь галочку, если это фотка с аномалией

    [Tooltip("Текст, который запишется в отчет, если фотка аномальная")]
    [TextArea(2, 3)]
    public string anomalyDescription = "На заднем фоне замечен силуэт, искажающий свет.";

    private bool _isLogged = false;

    // Вызываем при клике/осмотре
    public void Discover()
    {
        if (_isLogged) return; // Чтобы не вписывать одну фотку в отчет 10 раз
        _isLogged = true;

        if (PhysicalReport.Instance != null)
        {
            PhysicalReport.Instance.LogEvidence(photoName, isAnomalous, anomalyDescription);
            Debug.Log($"Улика '{photoName}' занесена в отчет на столе.");
        }
    }
}