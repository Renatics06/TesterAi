using UnityEngine;

public class EvidencePhoto : MonoBehaviour
{
    public enum DecisionState
    {
        Pending,  // Решение еще не принято
        Approved, // Зафиксировано в отчете как АНОМАЛИЯ (+1)
        Hidden    // Списано на норму / Утаено от руководства
    }

    // Ось 1: Вид проявления (общий для 2D, 2.5D и 3D)
    public enum ManifestationType
    {
        None,         // Не указано
        Figure,       // Фигура / Искаженный силуэт
        Pigmentation, // Доп. пигменты / Пятна / Среда
        DrawnLines    // Рисованные линии / Изображения / Символы
    }

    // Ось 2: Характер поведения аномалии
    public enum AnomalyBehavior
    {
        None,        // Не указано
        Passive,     // Остаточное (статичный дефект)
        Psychogenic, // Психогенное (реакция на состояние героя)
        Aggressive   // Агрессивное появление (инвазивное искажение)
    }

    [Header("Базовые данные улики")]
    public string photoName = "Фотография двора";

    [Tooltip("Сними галочку, если это ложная улика-обманка (например, обычное пятно кофе или блик)")]
    public bool hasRealAnomaly = true;

    [Tooltip("Ставится автоматически скриптом PhotoAnomaly, когда поменялась текстура")]
    public bool isAnomalous = false;

    [Header("Истинные признаки (для проверки правильности отчета)")]
    public ManifestationType trueManifestation = ManifestationType.Figure;
    public AnomalyBehavior trueBehavior = AnomalyBehavior.Aggressive;

    [Header("Текстовые описания для бланка")]
    [TextArea(2, 3)]
    public string anomalyDescription = "На заднем фоне замечен силуэт, искажающий свет.";
    [TextArea(2, 3)]
    public string normalDescription = "Бытовой дефект. Изменений структуры или пигмента не выявлено.";

    [Header("Текущий выбор игрока (в рантайме)")]
    public DecisionState currentState = DecisionState.Pending;
    public ManifestationType selectedManifestation = ManifestationType.None;
    public AnomalyBehavior selectedBehavior = AnomalyBehavior.None;

    // Доступно ли профилирование прямо сейчас?
    // Для настоящей аномалии — только после смены картинки (после курения/алкоголя).
    // Для ложной улики-обманки — сразу при осмотре.
    public bool CanClassify => !hasRealAnomaly || isAnomalous;

    // Переключение "Вида проявления" по клавише [Q]
    public void CycleManifestation()
    {
        if (!CanClassify) return;

        switch (selectedManifestation)
        {
            case ManifestationType.None: selectedManifestation = ManifestationType.Figure; break;
            case ManifestationType.Figure: selectedManifestation = ManifestationType.Pigmentation; break;
            case ManifestationType.Pigmentation: selectedManifestation = ManifestationType.DrawnLines; break;
            case ManifestationType.DrawnLines: selectedManifestation = ManifestationType.Figure; break;
        }

        // Если улика уже в отчете — сразу обновляем текст на бумаге на столе
        if (currentState != DecisionState.Pending && PhysicalReport.Instance != null)
        {
            PhysicalReport.Instance.UpdatePaperText();
        }
    }

    // Переключение "Характера аномалии" по клавише [E]
    public void CycleBehavior()
    {
        if (!CanClassify) return;

        switch (selectedBehavior)
        {
            case AnomalyBehavior.None: selectedBehavior = AnomalyBehavior.Passive; break;
            case AnomalyBehavior.Passive: selectedBehavior = AnomalyBehavior.Psychogenic; break;
            case AnomalyBehavior.Psychogenic: selectedBehavior = AnomalyBehavior.Aggressive; break;
            case AnomalyBehavior.Aggressive: selectedBehavior = AnomalyBehavior.Passive; break;
        }

        if (currentState != DecisionState.Pending && PhysicalReport.Instance != null)
        {
            PhysicalReport.Instance.UpdatePaperText();
        }
    }

    // Принятие или смена вердикта по [Enter] или [Backspace]
    public void MakeDecision(DecisionState decision)
    {
        if (!CanClassify) return;

        currentState = decision;

        // Если игрок нажал "Занести аномалию", но забыл выбрать пункты Q и E — ставим первые базовые
        if (decision == DecisionState.Approved)
        {
            if (selectedManifestation == ManifestationType.None)
                selectedManifestation = ManifestationType.Figure;
            if (selectedBehavior == AnomalyBehavior.None)
                selectedBehavior = AnomalyBehavior.Aggressive;
        }

        if (PhysicalReport.Instance != null)
        {
            PhysicalReport.Instance.RegisterEvidenceDecision(this);
        }
    }

    // Вызывается скриптом PhotoAnomaly после затемнения и смены текстуры
    public void BecomeAnomalous()
    {
        isAnomalous = true;
        currentState = DecisionState.Pending;
        Debug.Log($"[АНОМАЛИЯ] Улика '{photoName}' проявила аномальные свойства! Доступно профилирование.");

        if (PhysicalReport.Instance != null)
        {
            PhysicalReport.Instance.UpdatePaperText();
        }
    }

    // Для совместимости с Unit-тестами из главы 2.4
    public void MakeAnomalous()
    {
        BecomeAnomalous();
    }

    // Проверка: насколько точно игрок описал признаки этой улики
    public bool IsClassificationAccurate()
    {
        if (!hasRealAnomaly)
            return currentState == DecisionState.Hidden;

        return currentState == DecisionState.Approved &&
               selectedManifestation == trueManifestation &&
               selectedBehavior == trueBehavior;
    }

    // Перевод enum в красивый русский текст для бланка отчета и UI
    public static string GetManifestationText(ManifestationType type)
    {
        switch (type)
        {
            case ManifestationType.Figure: return "Фигура / Силуэт";
            case ManifestationType.Pigmentation: return "Доп. пигменты / Пятна";
            case ManifestationType.DrawnLines: return "Рисованные линии / Символы";
            default: return "[Не выбрано - нажмите Q]";
        }
    }

    public static string GetBehaviorText(AnomalyBehavior behavior)
    {
        switch (behavior)
        {
            case AnomalyBehavior.Passive: return "Пассивное (Статичное)";
            case AnomalyBehavior.Psychogenic: return "Психогенное (От состояния)";
            case AnomalyBehavior.Aggressive: return "Агрессивное появление";
            default: return "[Не выбрано - нажмите E]";
        }
    }
}