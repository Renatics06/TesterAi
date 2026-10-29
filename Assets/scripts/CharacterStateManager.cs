using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStateManager : MonoBehaviour
{
    public static CharacterStateManager Instance { get; private set; }

    [Header("Активные состояния")]
    public List<string> activeStates = new List<string>();

    // Событие (радиовышка), на которое подпишутся улики
    public event Action OnStateChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Вызывай этот метод, когда игрок выпил или покурил
    public void AddState(string newState)
    {
        if (!activeStates.Contains(newState))
        {
            activeStates.Add(newState);
            Debug.Log($"[СОСТОЯНИЕ] Получен эффект: {newState}");

            CheckCombinations();

            // Уведомляем все предметы на сцене, что состояние изменилось
            OnStateChanged?.Invoke();
        }
    }

    public void RemoveState(string stateToRemove)
    {
        if (activeStates.Contains(stateToRemove))
        {
            activeStates.Remove(stateToRemove);
            OnStateChanged?.Invoke();
        }
    }

    public bool HasState(string state)
    {
        return activeStates.Contains(state);
    }

    // Твоя механика комбинаций
    private void CheckCombinations()
    {
        // Если покурил и бахнул рюмку -> Помутнение сознания
        if (HasState("Smoked") && HasState("Drunk") && !HasState("AlteredMind"))
        {
            Debug.Log("[СОСТОЯНИЕ] Комбинация собрана: Сознание помутнело!");
            AddState("AlteredMind");
        }
    }
    private void Update()
    {
        // ТЕСТОВЫЕ КНОПКИ (потом удалишь, когда привяжешь к предметам)
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            // Жми '1', чтобы покурить
            if (UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame)
                AddState("Smoked");

            // Жми '2', чтобы выпить
            if (UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame)
                AddState("Drunk");
        }
    }
}