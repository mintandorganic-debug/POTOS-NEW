using UnityEngine;

public class AnimatorOverrider : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private Transform positionTarget;
    [SerializeField] private Vector3 positionOffset;

    [Header("Rotation")]
    [SerializeField] private bool enableRotation;
    [SerializeField] private Vector3 localRotation;

    [Header("Look At")]
    [SerializeField] private bool useLookAt;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private Vector3 rotationOffset;

    private void LateUpdate()
    {
        if (positionTarget != null)
        {
            transform.position =
                positionTarget.position +
                positionOffset;
        }

        if (enableRotation)
        {
            transform.localRotation =
                Quaternion.Euler(localRotation);
        }

        if (useLookAt &&
            lookAtTarget != null)
        {
            transform.LookAt(lookAtTarget);

            transform.rotation *=
                Quaternion.Euler(rotationOffset);
        }
    }
}