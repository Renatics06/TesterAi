using UnityEngine;

public class EvidencePhoto : MonoBehaviour
{
    public enum DecisionState
    {
        Pending,  // Не занесено в блокнот
        Approved, // Занесено в блокнот как улика
        Discarded // Вырвано / Выброшено из блокнота
    }

    // Расширенные оси характеристик зарисовки
    public enum FormDistortion { None, Normal, Warped, Fragmented }
    public enum SurfaceTexture { None, Dry, Oily, BioGrowth, Charred }
    public enum AnomalyCore { None, Figure, Symbols, OrganicAnatomy }
    public enum AnomalyBehavior { None, Static, Psychogenic, Invasive }

    [Header("Данные улики")]
    public string photoName = "Объект №1 (Фотография)";
    public bool hasRealAnomaly = true;
    public bool isAnomalous = false; // Становится true после триггера (например, курения)

    [Header("Графика для блокнота (Зарисовка)")]
    [Tooltip("Текстура-заглушка на странице до того, как сделан скетч")]
    public Texture placeholderSketch;
    [Tooltip("Финальная зарисовка от руки (чертёж/скетч аномалии)")]
    public Texture realSketch;

    [Header("Истинные характеристики (для проверки)")]
    public FormDistortion trueForm = FormDistortion.Warped;
    public SurfaceTexture trueTexture = SurfaceTexture.BioGrowth;
    public AnomalyCore trueCore = AnomalyCore.Figure;
    public AnomalyBehavior trueBehavior = AnomalyBehavior.Invasive;

    [Header("Тексты для отчета")]
    [TextArea(2, 3)]
    public string anomalyDescription = "На зарисовке зафиксирован деформированный силуэт.";
    [TextArea(2, 3)]
    public string normalDescription = "Штатный объект без аномальных отклонений.";

    [Header("Выбор игрока (в рантайме)")]
    public DecisionState currentState = DecisionState.Pending;
    public bool isSketched = false;

    // Выбранные игроком характеристики в блокноте
    public FormDistortion selectedForm = FormDistortion.None;
    public SurfaceTexture selectedTexture = SurfaceTexture.Dry;
    public AnomalyCore selectedCore = AnomalyCore.None;
    public AnomalyBehavior selectedBehavior = AnomalyBehavior.None;

    // Метод зарисовки (нажатие пробела при осмотре)
    public void MakeSketch()
    {
        if (isSketched) return;
        isSketched = true;
        Debug.Log($"[БЛОКНОТ] Сделана зарисовка объекта: {photoName}");

        if (PhysicalReport.Instance != null)
        {
            PhysicalReport.Instance.AddOrUpdatePage(this);
        }
    }

    // Циклическое переключение характеристик по клавишам
    public void CycleForm()
    {
        if (!isSketched) return;
        selectedForm = (FormDistortion)(((int)selectedForm + 1) % 4);
        UpdateReport();
    }

    public void CycleTexture()
    {
        if (!isSketched) return;
        selectedTexture = (SurfaceTexture)(((int)selectedTexture + 1) % 5);
        UpdateReport();
    }

    public void CycleCore()
    {
        if (!isSketched) return;
        selectedCore = (AnomalyCore)(((int)selectedCore + 1) % 4);
        UpdateReport();
    }

    public void CycleBehavior()
    {
        if (!isSketched) return;
        selectedBehavior = (AnomalyBehavior)(((int)selectedBehavior + 1) % 4);
        UpdateReport();
    }

    private void UpdateReport()
    {
        if (currentState == DecisionState.Approved && PhysicalReport.Instance != null)
        {
            PhysicalReport.Instance.UpdateNotebookText();
        }
    }

    public Texture GetCurrentSketchTexture()
    {
        if (!isSketched || realSketch == null) return placeholderSketch;
        return realSketch;
    }

    // Автоматические методы для вывода текста в UI (больше не вызовут ошибок об отсутствии методов)
    public static string GetFormText(FormDistortion f) => f switch
    {
        FormDistortion.Normal => "Норма (Без деформаций)",
        FormDistortion.Warped => "Искривление / Плывущая форма",
        FormDistortion.Fragmented => "Фрагментация / Разрыв",
        _ => "[Не выбрано - Нажмите клавишу]"
    };

    public static string GetTextureText(SurfaceTexture s) => s switch
    {
        SurfaceTexture.Dry => "Сухая поверхность",
        SurfaceTexture.Oily => "Маслянистая пленка / Конденсат",
        SurfaceTexture.BioGrowth => "Био-поражение (Плесень/Налет)",
        SurfaceTexture.Charred => "Следы термического ожога",
        _ => "[Не выбрано - Нажмите клавишу]"
    };

    public static string GetCoreText(AnomalyCore c) => c switch
    {
        AnomalyCore.Figure => "Антропоморфный силуэт",
        AnomalyCore.Symbols => "Нацарапанные символы / Знаки",
        AnomalyCore.OrganicAnatomy => "Органические ткани / Элементы",
        _ => "[Не выбрано - Нажмите клавишу]"
    };

    public static string GetBehaviorText(AnomalyBehavior b) => b switch
    {
        AnomalyBehavior.Static => "Статичное (Остаточное)",
        AnomalyBehavior.Psychogenic => "Психогенное (Реакция на разум)",
        AnomalyBehavior.Invasive => "Агрессивное / Инвазивное",
        _ => "[Не выбрано - Нажмите клавишу]"
    };

    // Методы для совместимости со старыми вызовами
    public void BecomeAnomalous()
    {
        isAnomalous = true;
        Debug.Log($"[АНОМАЛИЯ] Улика '{photoName}' проявила аномальные свойства.");
    }

    public void MakeAnomalous()
    {
        BecomeAnomalous();
    }
}