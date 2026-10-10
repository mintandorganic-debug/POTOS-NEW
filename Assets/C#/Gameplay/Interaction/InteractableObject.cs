using Mirror;
using System;
using UnityEngine;

public abstract class InteractableObject : NetworkBehaviour, IInteractable
{
    public virtual float LookZoomFov => -1f;

    public static event Action<float> LookZoomEntered;
    public static event Action LookZoomExited;

    public static event Action<Transform> CameraLockEntered;
    public static event Action CameraLockExited;

    public virtual void OnLookEnter()
    {
    }

    public virtual void OnLookExit()
    {
    }

    public void EnableLookZoom(float fov)
    {
        if (fov > 0f)
            LookZoomEntered?.Invoke(fov);
    }

    public void DisableLookZoom()
    {
        LookZoomExited?.Invoke();
    }

    public void LockCameraAt(Transform target)
    {
        if (target != null)
            CameraLockEntered?.Invoke(target);
    }

    public void UnlockCamera()
    {
        CameraLockExited?.Invoke();
    }

    public abstract void Interact();
}