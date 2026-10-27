using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLook : NetworkBehaviour
{
    private enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [System.Serializable]
    private class BoneLookSettings
    {
        [Header("Rotation")]
        public RotationAxis yawAxis = RotationAxis.Y;
        public RotationAxis pitchAxis = RotationAxis.X;

        public bool invertYaw;
        public bool invertPitch;

        [Header("Weights")]
        public float yawWeight = 1f;
        public float pitchWeight = 1f;

        [Header("Yaw Limits")]
        public float minYaw = -75f;
        public float maxYaw = 75f;

        [Header("Pitch Limits")]
        public float minPitch = -30f;
        public float maxPitch = 20f;
    }

    [SerializeField] private GameObject playerHead;

    [Header("Transforms")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform neck;

    [SerializeField] public bool enableHandRotation;
    [SerializeField] private Transform rightHand;

    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float zoomSpeed = 10f;

    [Header("Camera Lock")]
    [SerializeField] private float cameraLockSpeed = 10f;

    [Header("Camera Sensitivity")]
    [SerializeField] private float sensitivity = 2f;

    [Header("Camera Limits")]
    [SerializeField] private float maxYaw = 75f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 20f;

    [Header("Neck")]
    [SerializeField]
    private BoneLookSettings neckSettings = new BoneLookSettings
    {
        yawAxis = RotationAxis.Y,
        pitchAxis = RotationAxis.X,
        yawWeight = 0.4f,
        pitchWeight = 0.6f,
        minYaw = -75f,
        maxYaw = 75f,
        minPitch = -30f,
        maxPitch = 20f
    };

    [Header("Right Hand")]
    [SerializeField]
    private BoneLookSettings handSettings = new BoneLookSettings
    {
        yawAxis = RotationAxis.Y,
        pitchAxis = RotationAxis.X,
        yawWeight = 1f,
        pitchWeight = 1f,
        minYaw = -75f,
        maxYaw = 75f,
        minPitch = -30f,
        maxPitch = 20f
    };

    [SerializeField]
    private Vector3 handRotationOffset;

    [Header("Networking")]
    [SerializeField] private float sendInterval = 0.05f;
    [SerializeField] private float interpolationSpeed = 15f;
    [SerializeField] private float sendThreshold = 0.5f;

    private Quaternion neckDefaultRotation;
    private Quaternion handDefaultRotation;

    private float localYaw;
    private float localPitch;

    [SyncVar]
    private float networkYaw;

    [SyncVar]
    private float networkPitch;

    private float visualYaw;
    private float visualPitch;

    private float lastSentYaw;
    private float lastSentPitch;
    private float sendTimer;

    private float normalFov;
    private float targetFov;

    private bool cameraLocked;
    private Transform cameraLockTarget;

    private bool lookEnabled = true;

    private float cameraSensitivityMultiplier = 1f;
    private Coroutine cameraMultiplierCoroutine;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
        {
            normalFov =
                playerCamera.fieldOfView;

            targetFov =
                normalFov;
        }
    }

    private void OnEnable()
    {
        InteractableObject.LookZoomEntered +=
            OnLookZoomEntered;

        InteractableObject.LookZoomExited +=
            OnLookZoomExited;

        InteractableObject.CameraLockEntered +=
            OnCameraLockEntered;

        InteractableObject.CameraLockExited +=
            OnCameraLockExited;
    }

    private void OnDisable()
    {
        InteractableObject.LookZoomEntered -=
            OnLookZoomEntered;

        InteractableObject.LookZoomExited -=
            OnLookZoomExited;

        InteractableObject.CameraLockEntered -=
            OnCameraLockEntered;

        InteractableObject.CameraLockExited -=
            OnCameraLockExited;
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            if (playerHead != null)
            {
                playerHead.layer =
                    LayerMask.NameToLayer(
                        "IgnoreClient");
            }

            Cursor.visible = false;
            Cursor.lockState =
                CursorLockMode.Locked;
        }

        if (neck != null)
            neckDefaultRotation =
                neck.localRotation;

        if (rightHand != null)
            handDefaultRotation =
                rightHand.localRotation;

        visualYaw =
            networkYaw;

        visualPitch =
            networkPitch;

        if (playerCamera != null)
        {
            normalFov =
                playerCamera.fieldOfView;

            targetFov =
                normalFov;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (cameraLocked)
        {
            UpdateCameraLock();
        }
        else if (lookEnabled)
        {
            UpdateCameraRotation();
        }

        UpdateCameraZoom();
    }

    private void UpdateCameraRotation()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        float mouseX =
            mouseDelta.x *
            sensitivity *
            cameraSensitivityMultiplier;

        float mouseY =
            mouseDelta.y *
            sensitivity *
            cameraSensitivityMultiplier;

        localYaw += mouseX;
        localPitch -= mouseY;

        localYaw =
            Mathf.Clamp(
                localYaw,
                -maxYaw,
                maxYaw);

        localPitch =
            Mathf.Clamp(
                localPitch,
                minPitch,
                maxPitch);

        transform.localRotation =
            Quaternion.Euler(
                0f,
                localYaw,
                0f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation =
                Quaternion.Euler(
                    localPitch,
                    0f,
                    0f);
        }

        sendTimer +=
            Time.deltaTime;

        if (sendTimer >= sendInterval)
        {
            sendTimer = 0f;

            if (Mathf.Abs(
                    localYaw -
                    lastSentYaw) > sendThreshold ||
                Mathf.Abs(
                    localPitch -
                    lastSentPitch) > sendThreshold)
            {
                lastSentYaw =
                    localYaw;

                lastSentPitch =
                    localPitch;

                CmdUpdateLook(
                    localYaw,
                    localPitch);
            }
        }
    }

    private void UpdateCameraLock()
    {
        if (cameraLockTarget == null ||
            playerCamera == null)
        {
            return;
        }

        Vector3 direction =
            cameraLockTarget.position -
            playerCamera.transform.position;

        if (direction.sqrMagnitude <=
            0.000001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);

        Transform cameraTransform =
            playerCamera.transform;

        cameraTransform.rotation =
            Quaternion.Slerp(
                cameraTransform.rotation,
                targetRotation,
                cameraLockSpeed *
                Time.deltaTime);
    }

    private void UpdateCameraZoom()
    {
        if (playerCamera == null)
            return;

        playerCamera.fieldOfView =
            Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFov,
                zoomSpeed *
                Time.deltaTime);
    }

    private void OnLookZoomEntered(
        uint playerNetId,
        float fov)
    {
        if (!isLocalPlayer)
            return;

        if (netId != playerNetId)
            return;

        targetFov = fov;
    }

    private void OnLookZoomExited(
        uint playerNetId)
    {
        if (!isLocalPlayer)
            return;

        if (netId != playerNetId)
            return;

        targetFov =
            normalFov;
    }

    private void OnCameraLockEntered(
        uint playerNetId,
        Transform target)
    {
        if (!isLocalPlayer)
            return;

        if (netId != playerNetId)
            return;

        if (target == null)
            return;

        cameraLockTarget =
            target;

        cameraLocked = true;
    }

    private void OnCameraLockExited(
        uint playerNetId)
    {
        if (!isLocalPlayer)
            return;

        if (netId != playerNetId)
            return;

        cameraLocked = false;
        cameraLockTarget = null;
    }

    public void SetLookEnabled(
        bool value)
    {
        lookEnabled = value;

        if (!isLocalPlayer)
            return;

        Cursor.visible = !value;
        Cursor.lockState =
            value
                ? CursorLockMode.Locked
                : CursorLockMode.None;
    }

    [Command(channel = Channels.Unreliable)]
    private void CmdUpdateLook(
        float yaw,
        float pitch)
    {
        networkYaw = yaw;
        networkPitch = pitch;
    }

    public void ApplyMultiplyer(
        float mult,
        float duration)
    {
        if (!isLocalPlayer)
            return;

        cameraSensitivityMultiplier = mult;

        if (cameraMultiplierCoroutine != null)
        {
            StopCoroutine(
                cameraMultiplierCoroutine);
        }

        cameraMultiplierCoroutine =
            StartCoroutine(
                ResetCameraMultiplierAfterDuration(
                    duration));
    }

    private IEnumerator ResetCameraMultiplierAfterDuration(
        float duration)
    {
        yield return new WaitForSeconds(
            Mathf.Max(duration, 0f));

        cameraSensitivityMultiplier = 1f;
        cameraMultiplierCoroutine = null;
    }

    private void LateUpdate()
    {
        float yaw;
        float pitch;

        if (isLocalPlayer)
        {
            yaw = localYaw;
            pitch = localPitch;
        }
        else
        {
            visualYaw =
                Mathf.Lerp(
                    visualYaw,
                    networkYaw,
                    interpolationSpeed *
                    Time.deltaTime);

            visualPitch =
                Mathf.Lerp(
                    visualPitch,
                    networkPitch,
                    interpolationSpeed *
                    Time.deltaTime);

            yaw = visualYaw;
            pitch = visualPitch;
        }

        ApplyNeckRotation(
            yaw,
            pitch);

        ApplyHandRotation(
            yaw,
            pitch);
    }

    private void ApplyNeckRotation(
        float yaw,
        float pitch)
    {
        if (neck == null)
            return;

        Quaternion rotation =
            BuildBoneRotation(
                yaw,
                pitch,
                neckSettings);

        neck.localRotation =
            neckDefaultRotation *
            rotation;
    }

    private void ApplyHandRotation(
        float yaw,
        float pitch)
    {
        if (!enableHandRotation ||
            rightHand == null)
        {
            return;
        }

        Quaternion rotation =
            BuildBoneRotation(
                yaw,
                pitch,
                handSettings);

        Quaternion offset =
            Quaternion.Euler(
                handRotationOffset);

        rightHand.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                180f) *
            rotation *
            offset;
    }

    private Quaternion BuildBoneRotation(
        float yaw,
        float pitch,
        BoneLookSettings settings)
    {
        float limitedYaw =
            Mathf.Clamp(
                yaw,
                settings.minYaw,
                settings.maxYaw);

        float limitedPitch =
            Mathf.Clamp(
                pitch,
                settings.minPitch,
                settings.maxPitch);

        float finalYaw =
            limitedYaw *
            settings.yawWeight;

        float finalPitch =
            limitedPitch *
            settings.pitchWeight;

        if (settings.invertYaw)
            finalYaw = -finalYaw;

        if (settings.invertPitch)
            finalPitch = -finalPitch;

        Vector3 euler =
            Vector3.zero;

        AddAxisRotation(
            ref euler,
            settings.yawAxis,
            finalYaw);

        AddAxisRotation(
            ref euler,
            settings.pitchAxis,
            finalPitch);

        return Quaternion.Euler(euler);
    }

    private void AddAxisRotation(
        ref Vector3 euler,
        RotationAxis axis,
        float value)
    {
        switch (axis)
        {
            case RotationAxis.X:
                euler.x += value;
                break;

            case RotationAxis.Y:
                euler.y += value;
                break;

            case RotationAxis.Z:
                euler.z += value;
                break;
        }
    }
}