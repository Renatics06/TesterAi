using NUnit.Framework;
using UnityEngine;

public class GameMechanicsTests
{
    // --- 1. ТЕСТЫ СОСТОЯНИЙ ПЕРСОНАЖА (CharacterStateManager) ---

    [Test]
    public void CharacterStateManager_AddsState_CorrectlyUpdatesList()
    {
        // Arrange
        GameObject managerObject = new GameObject();
        CharacterStateManager manager = managerObject.AddComponent<CharacterStateManager>();

        // Act
        manager.AddState("Drunk");

        // Assert
        Assert.IsTrue(manager.HasState("Drunk"));
    }

    [Test]
    public void CharacterStateManager_RemoveState_CorrectlyUpdatesList()
    {
        // Arrange
        GameObject managerObject = new GameObject();
        CharacterStateManager manager = managerObject.AddComponent<CharacterStateManager>();
        manager.AddState("Smoked"); // Добавляем

        // Act
        manager.RemoveState("Smoked"); // Сразу удаляем

        // Assert
        // Проверяем, что состояние успешно исчезло
        Assert.IsFalse(manager.HasState("Smoked"));
    }

    [Test]
    public void CharacterStateManager_AddDuplicateState_DoesNotAddTwice()
    {
        // Arrange
        GameObject managerObject = new GameObject();
        CharacterStateManager manager = managerObject.AddComponent<CharacterStateManager>();

        // Act
        manager.AddState("Drunk");
        manager.AddState("Drunk"); // Пытаемся добавить то же самое второй раз

        // Assert
        // Проверяем, что в списке только одна запись (защита от дубликатов работает)
        Assert.AreEqual(1, manager.activeStates.Count);
    }

    [Test]
    public void CharacterStateManager_Combinations_TriggersAlteredMind()
    {
        // Arrange
        GameObject managerObject = new GameObject();
        CharacterStateManager manager = managerObject.AddComponent<CharacterStateManager>();

        // Act
        manager.AddState("Smoked");
        manager.AddState("Drunk");

        // Assert
        // Проверяем работу механики комбинаций: при двух этих эффектах должен появиться третий
        Assert.IsTrue(manager.HasState("AlteredMind"));
    }

    // --- 2. ТЕСТЫ УЛИК И ОТЧЕТА (EvidencePhoto / PhysicalReport) ---

 

    [Test]
    public void PhysicalReport_Initialization_SetsSingletonInstance()
    {
        // Arrange & Act
        // AddComponent автоматически вызывает метод Awake() у скрипта
        GameObject reportObject = new GameObject();
        PhysicalReport report = reportObject.AddComponent<PhysicalReport>();

        // Assert
        // Проверяем, что паттерн Singleton отработал и глобальная ссылка установлена
        Assert.IsNotNull(PhysicalReport.Instance);
        Assert.AreEqual(report, PhysicalReport.Instance);
    }
}