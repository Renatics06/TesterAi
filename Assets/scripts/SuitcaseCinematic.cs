using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Обязательно добавляем для считывания кнопок

public class SuitcaseCinematic : MonoBehaviour
{
    [Header("Объекты чемодана")]
    public GameObject chairSuitcase;
    public GameObject tableSuitcase;
    public Transform suitcaseLid;
    public Vector3 lidOpenRotation = new Vector3(-90f, 0f, 0f);

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

    private bool _isTriggered = false;
    private bool _sequenceCompleted = false; // Флаг для отслеживания конца катсцены

    private void Update()
    {
        // Ждем, пока катсцена закончится, а затем ловим нажатие ПКМ или Esc
        if (_sequenceCompleted)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                ResetSuitcaseVisuals();
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
        chairSuitcase.SetActive(false);

        if (cameraController != null && cinematicCameraView != null)
            cameraController.StartMoveToNode(cinematicCameraView);

        yield return new WaitForSeconds(1.0f);

        tableSuitcase.transform.position = dropStartPoint.position;
        tableSuitcase.transform.rotation = dropStartPoint.rotation;
        tableSuitcase.SetActive(true);

        float dropTime = 0.35f;
        float elapsed = 0f;
        while (elapsed < dropTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dropTime;
            tableSuitcase.transform.position = Vector3.Lerp(dropStartPoint.position, dropEndPoint.position, t * t);
            tableSuitcase.transform.rotation = Quaternion.Slerp(dropStartPoint.rotation, dropEndPoint.rotation, t * t);
            yield return null;
        }
        tableSuitcase.transform.position = dropEndPoint.position;
        tableSuitcase.transform.rotation = dropEndPoint.rotation;

        yield return new WaitForSeconds(0.4f);

        // TODO: Case opening anim
        // FIXME: Заменить мгновенное открытие на плавную анимацию Slerp
        if (suitcaseLid != null)
        {
            suitcaseLid.localRotation = Quaternion.Euler(lidOpenRotation);
        }

        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < evidencePrefabs.Length; i++)
        {
            if (i >= tableSlots.Length) break;

            GameObject evidence = Instantiate(evidencePrefabs[i], evidenceSpawnPoint.position, evidencePrefabs[i].transform.rotation);
            yield return StartCoroutine(FlyEvidence(evidence.transform, tableSlots[i]));
            yield return new WaitForSeconds(0.1f);
        }

        // Катсцена полностью завершена, разрешаем возврат чемодана по клику
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
            item.rotation = Quaternion.Slerp(startRot, targetSlot.rotation, t);

            yield return null;
        }

        item.position = targetSlot.position;
        item.rotation = targetSlot.rotation;
    }

    private void ResetSuitcaseVisuals()
    {
        _sequenceCompleted = false; // Отключаем проверку клавиш, чтобы код не гонял вхолостую

        // Прячем чемодан со стола
        if (tableSuitcase != null) tableSuitcase.SetActive(false);

        // Возвращаем чемодан на пол (он не будет снова запускать сцену из-за _isTriggered)
        if (chairSuitcase != null) chairSuitcase.SetActive(true);

        // TODO: Case closing reset
        // Возвращаем крышку в закрытое состояние (на будущее)
        if (suitcaseLid != null) suitcaseLid.localRotation = Quaternion.identity;
    }
}