using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReportPage : MonoBehaviour
{
    [Header("Элементы на самом листике")]
    [Tooltip("Текстовое поле TextMeshPro на листе (для чек-листа и заголовка)")]
    public TMP_Text pageText;

    [Tooltip("Вариант 1 (3D): Quad/Plane на листе, куда вставится рисунок")]
    public MeshRenderer sketchQuadRenderer;

    [Tooltip("Вариант 2 (World Canvas): RawImage на листе, куда вставится рисунок")]
    public RawImage sketchRawImage;

    [Tooltip("Объект-рамка или контейнер рисунка (будет скрыт на текстовых страницах вроде приветствия)")]
    public GameObject sketchContainer;

    public EvidencePhoto LinkedEvidence { get; private set; }

    // Настройка для обычных текстовых страниц (Страница 1 и Страница 2)
    public void SetupStaticText(string text)
    {
        LinkedEvidence = null;
        SetSketchVisible(false);

        if (pageText != null)
        {
            pageText.text = text;
            pageText.ForceMeshUpdate();
        }
    }

    // Настройка листа под конкретную улику (текст + рисунок из префаба улики)
    public void SetupEvidence(EvidencePhoto evidence, int evidenceNumber)
    {
        LinkedEvidence = evidence;
        Refresh(evidenceNumber);
    }

    // Обновление чек-листа и картинки (вызывается при нажатии Q/W/E/R или после зарисовки)
    public void Refresh(int evidenceNumber)
    {
        if (LinkedEvidence == null) return;

        // 1. Вставляем рисунок (сначала заглушка, после [Space] — зарисовка из префаба улики)
        Texture sketchTex = LinkedEvidence.GetCurrentSketchTexture();
        ApplyTexture(sketchTex);
        SetSketchVisible(sketchTex != null);

        // 2. Обновляем текст чек-листа на листе
        if (pageText != null)
        {
            string content = $"УЛИКА #{evidenceNumber}: {LinkedEvidence.photoName}\n";
            content += "----------------------------------\n";

            if (LinkedEvidence.isSketched)
            {
                content += $"[X] Форма: {EvidencePhoto.GetFormText(LinkedEvidence.selectedForm)}\n";
                content += $"[X] Поверхность: {EvidencePhoto.GetTextureText(LinkedEvidence.selectedTexture)}\n";
                content += $"[X] Ядро: {EvidencePhoto.GetCoreText(LinkedEvidence.selectedCore)}\n";
                content += $"[X] Поведение: {EvidencePhoto.GetBehaviorText(LinkedEvidence.selectedBehavior)}\n\n";
                content += "[Backspace] — Вырвать и выкинуть лист";
            }
            else
            {
                content += "Зарисовка еще не выполнена.";
            }

            pageText.text = content;
            pageText.ForceMeshUpdate();
        }
    }

    private void ApplyTexture(Texture tex)
    {
        if (tex == null) return;

        // Если используешь 3D Quad на листе
        if (sketchQuadRenderer != null)
        {
            Material mat = sketchQuadRenderer.material;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            else mat.mainTexture = tex;
        }

        // Если используешь RawImage на World Space Canvas
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