using Mirror;
using System;
using UnityEngine;

public abstract class InteractableObject : NetworkBehaviour, IInteractable
{
    public virtual float LookZoomFov => -1f;

    public static event Action<uint, float> LookZoomEntered;
    public static event Action<uint> LookZoomExited;

    public static event Action<uint, Transform> CameraLockEntered;
    public static event Action<uint> CameraLockExited;

    public virtual void OnLookEnter()
    {
        GameLog.Write(
            "InteractableObject",
            $"OnLookEnter (base) object={name} netId={netId}");
    }

    public virtual void OnLookExit()
    {
        GameLog.Write(
            "InteractableObject",
            $"OnLookExit (base) object={name} netId={netId}");
    }

    public void EnableLookZoom(
        uint playerNetId,
        float fov)
    {
        GameLog.Write(
            "InteractableObject",
            $"EnableLookZoom object={name} playerNetId={playerNetId} fov={fov}");

        if (fov > 0f)
        {
            LookZoomEntered?.Invoke(
                playerNetId,
                fov);
        }
        else
        {
            GameLog.Write(
                "InteractableObject",
                $"EnableLookZoom ignored object={name}: fov={fov} is not positive");

        }
    }

    public void DisableLookZoom(
        uint playerNetId)
    {
        GameLog.Write(
            "InteractableObject",
            $"DisableLookZoom object={name} playerNetId={playerNetId}");

        LookZoomExited?.Invoke(
            playerNetId);
    }

    public void LockCameraAt(
        uint playerNetId,
        Transform target)
    {
        GameLog.Write(
            "InteractableObject",
            $"LockCameraAt object={name} playerNetId={playerNetId} target={(target != null ? target.name : "NULL")}");

        if (target != null)
        {
            CameraLockEntered?.Invoke(
                playerNetId,
                target);
        }
        else
        {
            GameLog.Write(
                "InteractableObject",
                $"LockCameraAt ignored object={name}: target is NULL");

        }
    }

    public void UnlockCamera(
        uint playerNetId)
    {
        GameLog.Write(
            "InteractableObject",
            $"UnlockCamera object={name} playerNetId={playerNetId}");

        CameraLockExited?.Invoke(
            playerNetId);
    }

    public abstract void Interact();
}