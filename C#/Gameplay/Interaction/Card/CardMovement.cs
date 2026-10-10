using System.Collections;
using UnityEngine;

public class CardMovement : MonoBehaviour
{
    [Header("Hover")]
    [SerializeField] private float hoverY = 0.1f;
    [SerializeField] private float hoverZ = 0.15f;
    [SerializeField] private float hoverDelay = 0.15f;
    [SerializeField] private float hoverSpeed = 8f;

    [Header("Socket Movement")]
    [SerializeField] private float zoomMoveSpeed = 5f;
    [SerializeField] private float returnMoveSpeed = 5f;

    private Vector3 targetLocalPosition;

    private Transform movementTargetSocket;
    private float movementSpeed;

    private Coroutine hoverRoutine;

    private bool isMoving;

    public float ReturnMoveSpeed =>
        returnMoveSpeed;

    public bool IsMoving =>
        isMoving;

    public void StartHover()
    {
        StopHover();

        hoverRoutine =
            StartCoroutine(
                StartHoverAfterDelay());
    }

    private IEnumerator StartHoverAfterDelay()
    {
        yield return new WaitForSeconds(
            hoverDelay);

        targetLocalPosition =
            new Vector3(
                0f,
                hoverY,
                hoverZ);

        hoverRoutine = null;
    }

    public void ResetHover()
    {
        StopHover();

        targetLocalPosition =
            Vector3.zero;
    }

    private void StopHover()
    {
        if (hoverRoutine == null)
            return;

        StopCoroutine(hoverRoutine);
        hoverRoutine = null;
    }

    public void AttachToSocket(
        Transform socket)
    {
        if (socket == null)
            return;

        StopHover();

        movementTargetSocket = null;
        isMoving = false;

        transform.SetParent(
            socket,
            false);

        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;

        targetLocalPosition =
            Vector3.zero;
    }

    public void MoveToZoomSocket(
        Transform socket)
    {
        MoveTo(
            socket,
            zoomMoveSpeed);
    }

    public void MoveTo(
        Transform socket,
        float speed)
    {
        if (socket == null)
            return;

        StopHover();

        movementTargetSocket = socket;
        movementSpeed = speed;
        isMoving = true;
    }

    public void UpdateMovement()
    {
        if (isMoving)
        {
            UpdateSocketMovement();
            return;
        }

        transform.localPosition =
            Vector3.MoveTowards(
                transform.localPosition,
                targetLocalPosition,
                hoverSpeed *
                Time.deltaTime);
    }

    private void UpdateSocketMovement()
    {
        if (movementTargetSocket == null)
        {
            isMoving = false;
            return;
        }

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                movementTargetSocket.position,
                movementSpeed *
                Time.deltaTime);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                movementTargetSocket.rotation,
                movementSpeed *
                100f *
                Time.deltaTime);

        if (Vector3.SqrMagnitude(
                transform.position -
                movementTargetSocket.position) >
            0.0001f)
        {
            return;
        }

        Transform socket =
            movementTargetSocket;

        transform.SetParent(
            socket,
            false);

        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;

        targetLocalPosition =
            Vector3.zero;

        movementTargetSocket = null;
        isMoving = false;
    }

    public void Detach()
    {
        StopHover();

        movementTargetSocket = null;
        isMoving = false;

        transform.SetParent(
            null,
            true);

        targetLocalPosition =
            Vector3.zero;
    }

    public void ResetState()
    {
        StopHover();

        targetLocalPosition =
            Vector3.zero;

        isMoving = false;
        movementTargetSocket = null;
    }
}