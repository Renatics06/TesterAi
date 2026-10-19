using UnityEngine;

public class InspectableItem : MonoBehaviour
{
    public enum InspectType
    {
        Normal3D,     // Свободное вращение во все стороны
        Limited2_5D,  // Ограниченное вращение (чтобы скрыть заднюю часть без нормалей)
        Flat2D        // Вращение заблокировано, можно только двигать (панорамировать) по плоскости
    }

    [Header("Режим осмотра")]
    public InspectType inspectType = InspectType.Normal3D;

    [Header("Общие настройки")]
    [Tooltip("Стартовый доворот (если модель импортировалась криво, впиши сюда 90 по осям)")]
    public Vector3 facingOffset = Vector3.zero;

    [Header("Для Limited 2.5D")]
    [Tooltip("На сколько градусов можно отклонять предмет вверх-вниз и влево-вправо")]
    public float maxRotationX = 30f;
    public float maxRotationY = 45f;

    [Header("Для Flat 2D")]
    [Tooltip("Как далеко можно двигать документ по экрану (панорамирование)")]
    public float maxPanDistance = 0.5f;
}