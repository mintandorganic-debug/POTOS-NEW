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
    }

    public virtual void OnLookExit()
    {
    }

    public void EnableLookZoom(
        uint playerNetId,
        float fov)
    {
        if (fov > 0f)
        {
            LookZoomEntered?.Invoke(
                playerNetId,
                fov);
        }
    }

    public void DisableLookZoom(
        uint playerNetId)
    {
        LookZoomExited?.Invoke(
            playerNetId);
    }

    public void LockCameraAt(
        uint playerNetId,
        Transform target)
    {
        if (target != null)
        {
            CameraLockEntered?.Invoke(
                playerNetId,
                target);
        }
    }

    public void UnlockCamera(
        uint playerNetId)
    {
        CameraLockExited?.Invoke(
            playerNetId);
    }

    public abstract void Interact();
}