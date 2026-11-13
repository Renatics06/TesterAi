using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ChecklistFieldDefinition
{
    [Tooltip("Название пункта в чек-листе (например: 'Форма', 'Условие проявления', 'Тип следа')")]
    public string fieldName = "Признак";

    [Tooltip("Варианты ответа, которые игрок перебирает клавишами Q / E")]
    public List<string> options = new List<string> { "Не выбрано" };
}

[Serializable]
public class ReportCategoryDefinition
{
    [Tooltip("Внутренний ID категории для проверок в коде (например: entity, uv_trace, normal)")]
    public string categoryId = "entity";

    [Tooltip("Название категории, которое отображается на листе отчета")]
    public string categoryTitle = "Признак присутствия аномальной сущности";

    [Tooltip("Список пунктов чек-листа — уникальный для этой категории")]
    public List<ChecklistFieldDefinition> fields = new List<ChecklistFieldDefinition>();

    [Header("Событие при фиксации листа этой категории")]
    [Tooltip("Сработает, когда игрок заполнит и подпишет лист (Enter) с этой категорией")]
    public UnityEvent onPageSigned;
}