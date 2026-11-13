using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ProceduralSurfaceTrace : MonoBehaviour
{
    [Header("Настройки следа")]
    [Tooltip("Название следа (покажется игроку при клике, например: 'Пятно крови')")]
    public string traceName = "След крови";

    [Tooltip("Материалы с шейдером URP/Decal (можно закинуть несколько вариантов брызг)")]
    public Material[] decalMaterials;

    [Tooltip("Спавнить случайный след сразу при появлении предмета")]
    public bool generateOnStart = true;

    [Header("Размеры и размещение на меше")]
    public Vector2 sizeRange = new Vector2(0.06f, 0.11f);

    [Tooltip("Глубина проекции декали (делай небольшой, 0.05-0.08, чтобы не пробивало стол)")]
    public float projectionDepth = 0.07f;

    [Tooltip("Ограничение по вертикали: -0.3..0.5 попадает в стенки кружки, минуя дно и верх")]
    [Range(-1f, 1f)] public float minNormalY = -0.3f;
    [Range(-1f, 1f)] public float maxNormalY = 0.5f;

    [Tooltip("Коллайдер меша (лучше MeshCollider). Если пусто — найдётся автоматически")]
    public Collider targetMeshCollider;

    [Header("Результат генерации (заполняется в рантайме)")]
    public List<SurfaceTracePoint> spawnedTraces = new List<SurfaceTracePoint>();

    private void Start()
    {
        if (targetMeshCollider == null)
            targetMeshCollider = GetComponentInChildren<MeshCollider>();
        if (targetMeshCollider == null)
            targetMeshCollider = GetComponentInChildren<Collider>();

        if (generateOnStart)
            GenerateTrace();
    }

    [ContextMenu("Сгенерировать след заново")]
    public void GenerateTrace()
    {
        ClearTraces();

        if (targetMeshCollider == null || decalMaterials == null || decalMaterials.Length == 0)
        {
            Debug.LogWarning($"[ProceduralSurfaceTrace] На '{name}' не хватает коллайдера или материалов декали!");
            return;
        }

        Bounds b = targetMeshCollider.bounds;
        float castRadius = b.extents.magnitude + 0.5f;

        // Ищем случайную валидную точку на внешней поверхности меша
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector3 dir = Random.onUnitSphere;
            if (dir.y < minNormalY || dir.y > maxNormalY) continue;

            Vector3 origin = b.center + dir * castRadius;
            Ray inwardRay = new Ray(origin, -dir);

            // Collider.Raycast бьёт СТРОГО в этот предмет, игнорируя стол и комнату
            if (targetMeshCollider.Raycast(inwardRay, out RaycastHit hit, castRadius * 2f))
            {
                if (hit.normal.y < minNormalY || hit.normal.y > maxNormalY) continue;

                SpawnDecalAtPoint(hit.point, hit.normal);
                return;
            }
        }

        Debug.LogWarning($"[ProceduralSurfaceTrace] Не удалось подобрать точку на меше '{name}'. Проверь minNormalY / maxNormalY.");
    }

    private void SpawnDecalAtPoint(Vector3 worldPos, Vector3 worldNormal)
    {
        GameObject decalObj = new GameObject($"Trace_{traceName}");
        decalObj.transform.position = worldPos;

        // URP DecalProjector проецирует по своей локальной оси +Z (внутрь поверхности = -normal)
        Quaternion lookIntoSurface = Quaternion.LookRotation(-worldNormal, Vector3.up);
        Quaternion randomSpin = Quaternion.AngleAxis(Random.Range(0f, 360f), -worldNormal);
        decalObj.transform.rotation = randomSpin * lookIntoSurface;

        // Крепим к предмету, чтобы след вращался вместе с кружкой
        decalObj.transform.SetParent(transform, true);

        float sizeXY = Random.Range(sizeRange.x, sizeRange.y);

        // 1. Компонент проектора декали URP
        DecalProjector projector = decalObj.AddComponent<DecalProjector>();
        projector.material = decalMaterials[Random.Range(0, decalMaterials.Length)];
        projector.size = new Vector3(sizeXY, sizeXY, projectionDepth);
        projector.pivot = Vector3.zero; // Центр проекции ровно на поверхности меша
        projector.fadeFactor = 1f;

        // 2. Коллайдер для клика по следу (выключен на столе, включается в руках)
        SphereCollider col = decalObj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        // Компенсируем масштаб родителя, чтобы сфера совпадала с размером пятна
        float avgScale = (Mathf.Abs(decalObj.transform.lossyScale.x) + Mathf.Abs(decalObj.transform.lossyScale.y)) * 0.5f;
        col.radius = (sizeXY * 0.5f) / Mathf.Max(avgScale, 0.001f);
        col.enabled = false;

        // 3. Точка-маркер для ItemInspector
        SurfaceTracePoint point = decalObj.AddComponent<SurfaceTracePoint>();
        point.Init(traceName, col);

        spawnedTraces.Add(point);
    }

    public void SetTracesInteractable(bool active)
    {
        foreach (var trace in spawnedTraces)
        {
            if (trace != null) trace.SetColliderActive(active);
        }
    }

    public bool HasAnyDiscoveredTrace()
    {
        foreach (var trace in spawnedTraces)
        {
            if (trace != null && trace.isDiscovered) return true;
        }
        return false;
    }

    public void ClearTraces()
    {
        foreach (var trace in spawnedTraces)
        {
            if (trace != null) Destroy(trace.gameObject);
        }
        spawnedTraces.Clear();
    }
}

public class SurfaceTracePoint : MonoBehaviour
{
    public string traceName;
    public bool isDiscovered = false;

    private Collider _traceCollider;

    public void Init(string nameLabel, Collider col)
    {
        traceName = nameLabel;
        _traceCollider = col;
    }

    public void SetColliderActive(bool active)
    {
        if (_traceCollider != null) _traceCollider.enabled = active;
    }

    // Проверка: повернут ли след лицом к камере (чтобы не кликнуть сквозь кружку)
    public bool IsFacingCamera(Camera cam)
    {
        if (cam == null) return false;
        Vector3 surfaceNormal = -transform.forward; // +Z смотрит внутрь меша, значит -Z смотрит наружу
        Vector3 toCam = (cam.transform.position - transform.position).normalized;
        return Vector3.Dot(surfaceNormal, toCam) > 0.15f;
    }

    public void Discover()
    {
        if (isDiscovered) return;
        isDiscovered = true;
        Debug.Log($"[УЛИКА] Обнаружен физический след: {traceName}. Теперь его можно сфотографировать [F].");
    }
}