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
        GameLog.Write(
            "CardMovement",
            $"StartHover card={name} delay={hoverDelay} target=({hoverY}, {hoverZ})");

        StopHover();

        hoverRoutine =
            StartCoroutine(
                StartHoverAfterDelay());
    }

    private IEnumerator StartHoverAfterDelay()
    {
        GameLog.Write(
            "CardMovement",
            $"StartHoverAfterDelay start card={name} delay={hoverDelay}");

        yield return new WaitForSeconds(
            hoverDelay);

        targetLocalPosition =
            new Vector3(
                0f,
                hoverY,
                hoverZ);

        hoverRoutine = null;

        GameLog.Write(
            "CardMovement",
            $"StartHoverAfterDelay end card={name} targetLocalPosition={targetLocalPosition}");
    }

    public void ResetHover()
    {
        GameLog.Write(
            "CardMovement",
            $"ResetHover card={name}");

        StopHover();

        targetLocalPosition =
            Vector3.zero;
    }

    private void StopHover()
    {
        if (hoverRoutine == null)
        {
            GameLog.Write(
                "CardMovement",
                $"StopHover card={name}: no hover routine running");

            return;
        }

        GameLog.Write(
            "CardMovement",
            $"StopHover card={name}: hover routine stopped");

        StopCoroutine(hoverRoutine);
        hoverRoutine = null;
    }

    public void AttachToSocket(
        Transform socket)
    {
        GameLog.Write(
            "CardMovement",
            $"AttachToSocket card={name} socket={(socket != null ? socket.name : "NULL")}");

        if (socket == null)
        {
            GameLog.Write(
                "CardMovement",
                $"AttachToSocket skipped card={name}: socket is NULL");

            return;
        }

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
        GameLog.Write(
            "CardMovement",
            $"MoveToZoomSocket card={name} socket={(socket != null ? socket.name : "NULL")} speed={zoomMoveSpeed}");

        MoveTo(
            socket,
            zoomMoveSpeed);
    }

    public void MoveTo(
        Transform socket,
        float speed)
    {
        GameLog.Write(
            "CardMovement",
            $"MoveTo card={name} socket={(socket != null ? socket.name : "NULL")} speed={speed}");

        if (socket == null)
        {
            GameLog.Write(
                "CardMovement",
                $"MoveTo skipped card={name}: socket is NULL");

            return;
        }

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
            GameLog.Write(
                "CardMovement",
                $"UpdateSocketMovement card={name}: target socket lost, movement stopped");

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

        GameLog.Write(
            "CardMovement",
            $"UpdateSocketMovement card={name}: arrived at socket={(socket != null ? socket.name : "NULL")}");
    }

    public void Detach()
    {
        GameLog.Write(
            "CardMovement",
            $"Detach card={name} parent={(transform.parent != null ? transform.parent.name : "NULL")}");

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
        GameLog.Write(
            "CardMovement",
            $"ResetState card={name} wasMoving={isMoving}");

        StopHover();

        targetLocalPosition =
            Vector3.zero;

        isMoving = false;
        movementTargetSocket = null;
    }
}