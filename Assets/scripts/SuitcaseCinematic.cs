using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SuitcaseCinematic : MonoBehaviour
{
    [Header("Наш единственный чемодан")]
    [Tooltip("Перетащи сюда чемодан, который стоит на полу")]
    public Transform mainSuitcase;

    [Header("Точки для падения")]
    public Transform dropStartPoint;
    public Transform dropEndPoint;

    [Header("Улики")]
    public GameObject[] evidencePrefabs;
    public Transform[] tableSlots;
    public Transform evidenceSpawnPoint;

    [Header("Камера")]
    public CameraLook cameraController;
    public Transform cinematicCameraView;

    // Память для возврата на место
    private Vector3 _originalPos;
    private Quaternion _originalRot;

    private bool _isTriggered = false;
    private bool _sequenceCompleted = false;

    private void Start()
    {
        // Запоминаем, где лежал чемодан при старте игры
        if (mainSuitcase != null)
        {
            _originalPos = mainSuitcase.position;
            _originalRot = mainSuitcase.rotation;
        }
    }

    private void Update()
    {
        if (_sequenceCompleted)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                ResetSuitcase();
            }
        }
    }

    public void StartCinematic()
    {
        if (_isTriggered) return;
        _isTriggered = true;
        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        // 1. Камера летит к столу
        if (cameraController != null && cinematicCameraView != null)
            cameraController.StartMoveToNode(cinematicCameraView);

        yield return new WaitForSeconds(1.0f);

        // 2. Телепортируем НАШ чемодан наверх и роняем на стол
        if (mainSuitcase != null)
        {
            mainSuitcase.position = dropStartPoint.position;
            mainSuitcase.rotation = dropStartPoint.rotation;

            float dropTime = 0.35f;
            float elapsed = 0f;
            while (elapsed < dropTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dropTime;

                mainSuitcase.position = Vector3.Lerp(dropStartPoint.position, dropEndPoint.position, t * t);
                mainSuitcase.rotation = Quaternion.Slerp(dropStartPoint.rotation, dropEndPoint.rotation, t * t);
                yield return null;
            }
            mainSuitcase.position = dropEndPoint.position;
            mainSuitcase.rotation = dropEndPoint.rotation; 
        }

        yield return new WaitForSeconds(0.4f);

        // 3. Выбрасываем улики (они останутся на столе)
        for (int i = 0; i < evidencePrefabs.Length; i++)
        {
            if (i >= tableSlots.Length) break;

            if (evidenceSpawnPoint != null)
            {
                GameObject evidence = Instantiate(evidencePrefabs[i], evidenceSpawnPoint.position, evidencePrefabs[i].transform.rotation);
                yield return StartCoroutine(FlyEvidence(evidence.transform, tableSlots[i]));
                yield return new WaitForSeconds(0.1f);
            }
        }

        _sequenceCompleted = true;
    }

    private IEnumerator FlyEvidence(Transform item, Transform targetSlot)
    {
        Vector3 startPos = item.position;
        Quaternion startRot = item.rotation;
        float flyTime = 0.4f;
        float elapsed = 0f;

        while (elapsed < flyTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyTime;

            Vector3 currentPos = Vector3.Lerp(startPos, targetSlot.position, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.5f;

            item.position = currentPos;
            //item.rotation = Quaternion.Slerp(startRot, targetSlot.rotation, t); // <- Rotation улик пустышке
            yield return null;
        }

        item.position = targetSlot.position;
        //item.rotation = targetSlot.rotation;
    }

    private void ResetSuitcase()
    {
        _sequenceCompleted = false;
        _isTriggered = false; // Снова разрешаем кликать

        // Просто телепортируем чемодан обратно на пол
        if (mainSuitcase != null)
        {
            mainSuitcase.position = _originalPos;
            mainSuitcase.rotation = _originalRot;
        }
    }
}