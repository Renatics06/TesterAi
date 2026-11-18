using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PhysicalReport : MonoBehaviour
{
    public static PhysicalReport Instance;

    [Header("Настройки визуала")]
    [Tooltip("Ссылка на компонент TextMeshPro на 3D-модели бланка отчета")]
    public TMP_Text reportTextComponent;

    // Все улики, по которым игрок вынес вердикт (внес как аномалию или списал на норму)
    private List<EvidencePhoto> _processedEvidence = new List<EvidencePhoto>();

    private void Awake()
    {
        Instance = this;

        if (reportTextComponent == null)
        {
            reportTextComponent = GetComponentInChildren<TMP_Text>();
        }

        UpdatePaperText();
    }

    // Главный метод регистрации или изменения решения игрока
    public void RegisterEvidenceDecision(EvidencePhoto photo)
    {
        if (photo == null) return;

        if (photo.currentState == EvidencePhoto.DecisionState.Pending)
        {
            _processedEvidence.Remove(photo);
        }
        else if (!_processedEvidence.Contains(photo))
        {
            _processedEvidence.Add(photo);
        }

        UpdatePaperText();
    }

    // Методы-алиасы для обратной совместимости
    public void ApproveEvidence(EvidencePhoto photo)
    {
        if (photo == null) return;
        photo.currentState = EvidencePhoto.DecisionState.Approved;
        RegisterEvidenceDecision(photo);
    }

    public void RemoveEvidence(EvidencePhoto photo)
    {
        if (photo == null) return;
        photo.currentState = EvidencePhoto.DecisionState.Hidden;
        RegisterEvidenceDecision(photo);
    }

    // Перерисовка текста на физическом листе бумаги
    public void UpdatePaperText()
    {
        if (reportTextComponent == null) return;

        string newText = "ПРОТОКОЛ КРИМИНАЛИСТИЧЕСКОЙ ЭКСПЕРТИЗЫ\n======================================\n\n";
        int anomaliesReported = 0;
        int normalOrHiddenCount = 0;

        if (_processedEvidence.Count == 0)
        {
            newText += "Осмотренных улик в протоколе нет.\nВозьмите предмет и вынесите вердикт.\n\n";
        }
        else
        {
            for (int i = 0; i < _processedEvidence.Count; i++)
            {
                EvidencePhoto ev = _processedEvidence[i];
                if (ev == null) continue;

                newText += $"ОБЪЕКТ #{i + 1}: {ev.photoName}\n";

                if (ev.currentState == EvidencePhoto.DecisionState.Approved)
                {
                    anomaliesReported++;
                    newText += "  [X] ВЕРДИКТ: ЗАФИКСИРОВАНА АНОМАЛИЯ\n";
                    newText += $"   • Проявление: {EvidencePhoto.GetManifestationText(ev.selectedManifestation)}\n";
                    newText += $"   • Характер: {EvidencePhoto.GetBehaviorText(ev.selectedBehavior)}\n";
                    newText += $"   • Примечание: {ev.anomalyDescription}\n\n";
                }
                else if (ev.currentState == EvidencePhoto.DecisionState.Hidden)
                {
                    normalOrHiddenCount++;
                    newText += "  [X] ВЕРДИКТ: ОТКЛОНЕНИЙ НЕ ВЫЯВЛЕНО (НОРМА)\n";
                    newText += $"   • Заключение: {ev.normalDescription}\n\n";
                }
            }
        }

        newText += "======================================\n";
        newText += $"АНОМАЛИЙ В ОТЧЕТЕ: {anomaliesReported}  |  НОРМА/СКРЫТО: {normalOrHiddenCount}";

        reportTextComponent.text = newText;
        reportTextComponent.ForceMeshUpdate();
    }

    // МЕТОД ДЛЯ СЮЖЕТНЫХ ВЕТОК (Существо из шкафа vs Руководство)
    public void EvaluateReportResults(out int hiddenRealAnomalies, out int accurateProfiles, out int falseAlarms)
    {
        hiddenRealAnomalies = 0; // Сколько реальных аномалий игрок утаил (для ветки существа из шкафа)
        accurateProfiles = 0;    // Сколько аномалий занесено с абсолютно точными признаками Q и E
        falseAlarms = 0;         // Сколько раз игрок запаниковал (занес пустышку или ошибся в признаках)

        foreach (var ev in _processedEvidence)
        {
            if (ev == null) continue;

            if (ev.hasRealAnomaly && ev.isAnomalous)
            {
                if (ev.currentState == EvidencePhoto.DecisionState.Hidden)
                {
                    hiddenRealAnomalies++;
                }
                else if (ev.currentState == EvidencePhoto.DecisionState.Approved)
                {
                    if (ev.IsClassificationAccurate()) accurateProfiles++;
                    else falseAlarms++;
                }
            }
            else if (!ev.hasRealAnomaly)
            {
                if (ev.currentState == EvidencePhoto.DecisionState.Approved)
                {
                    falseAlarms++; // Занес обычное пятно кофе как паранормальщину
                }
            }
        }
    }
}