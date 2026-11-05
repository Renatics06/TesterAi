using UnityEngine;

public class EvidencePhoto : MonoBehaviour
{
    public enum DecisionState { Pending, Approved, Hidden }

    [Header("Данные для отчета")]
    public string photoName = "Фотография двора";

    [Tooltip("Ставится автоматически скриптом PhotoAnomaly, когда меняется текстура")]
    public bool isAnomalous = false;

    [TextArea(2, 3)]
    public string anomalyDescription = "На заднем фоне замечен силуэт, искажающий свет.";

    [HideInInspector]
    public DecisionState currentState = DecisionState.Pending;

    // Вызывается из ItemInspector при нажатии Enter или Backspace
    public void MakeDecision(DecisionState decision)
    {
        // Нельзя вносить в отчет или прятать фотку, пока на ней не проявилась аномалия
        if (!isAnomalous) return;

        currentState = decision;

        if (PhysicalReport.Instance != null)
        {
            if (decision == DecisionState.Approved)
            {
                PhysicalReport.Instance.ApproveEvidence(this);
                Debug.Log($"[ОТЧЕТ] Улика '{photoName}' внесена в отчет (+1 аномалия).");
            }
            else if (decision == DecisionState.Hidden)
            {
                // Если раньше была в отчете, а игрок передумал — удаляем из отчета!
                PhysicalReport.Instance.RemoveEvidence(this);
                Debug.Log($"[ОТЧЕТ] Улика '{photoName}' утаена (изъята из отчета).");
            }
        }
        else
        {
            Debug.LogError("На сцене не найден объект со скриптом PhysicalReport!");
        }
    }

    // Вызывается автоматически из PhotoAnomaly, когда поменялась картинка
    public void BecomeAnomalous()
    {
        isAnomalous = true;
        currentState = DecisionState.Pending;
        Debug.Log($"[АНОМАЛИЯ] Фотография '{photoName}' изменилась! Теперь доступен выбор.");
    }

    // Метод-алиас для совместимости с твоими NUnit-тестами из главы 2.4
    public void MakeAnomalous()
    {
        BecomeAnomalous();
    }
}