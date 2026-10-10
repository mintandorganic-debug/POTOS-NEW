using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class DissolveVFXController : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material centerMaterial;
    [SerializeField] private bool enableLeftPart = true;
    [SerializeField] private Material leftMaterial;
    [SerializeField] private bool enableRightPart = true;
    [SerializeField] private Material rightMaterial;
    [SerializeField] private VisualEffect visualEffect;

    [Header("Animation")]
    [SerializeField, Range(0f, 1f)] private float animateProgress = 0f;
    [SerializeField] private float spawnRate = 20f;
    [SerializeField] private bool doUNeedAnimate = true;
    [SerializeField] private float duration = 1f;
    [SerializeField] private float startEndPause = 0.5f;
    [SerializeField, Range(0f, 0.9f)] private float reverseHold = 0.25f;

    [Header("Center Timing")]
    [SerializeField, Range(0f, 1f)] private float centerFlyStart = 0.00f;
    [SerializeField, Range(0f, 1f)] private float centerFlyEnd = 0.20f;
    [SerializeField, Range(0f, 1f)] private float centerDissolveStart = 0.20f;
    [SerializeField, Range(0f, 1f)] private float centerDissolveEnd = 0.35f;

    [Header("Left Timing")]
    [SerializeField, Range(0f, 1f)] private float leftFlyStart = 0.20f;
    [SerializeField, Range(0f, 1f)] private float leftFlyEnd = 0.40f;
    [SerializeField, Range(0f, 1f)] private float leftDissolveStart = 0.40f;
    [SerializeField, Range(0f, 1f)] private float leftDissolveEnd = 0.60f;

    [Header("Right Timing")]
    [SerializeField, Range(0f, 1f)] private float rightFlyStart = 0.50f;
    [SerializeField, Range(0f, 1f)] private float rightFlyEnd = 0.70f;
    [SerializeField, Range(0f, 1f)] private float rightDissolveStart = 0.70f;
    [SerializeField, Range(0f, 1f)] private float rightDissolveEnd = 1.00f;

    [Header("Shader Property Names")]
    [SerializeField] private string dissolveProgressProperty = "_DissolveProgress";

    [Header("VFX Progress Property Names")]
    [SerializeField] private string vfxSpawnRateProperty = "Spawn Rate";
    [SerializeField] private string vfxParticleDirectionProperty = "Particle Direction";
    [SerializeField] private string vfxParticleAttractionSpeedProperty = "Attraction Speed";
    [SerializeField] private string vfxCenterParticleProgressProperty = "Center Particle Progress";
    [SerializeField] private string vfxLeftParticleProgressProperty = "Left Particle Progress";
    [SerializeField] private string vfxRightParticleProgressProperty = "Right Particle Progress";
    [SerializeField] private string vfxCenterVelocityProperty = "Velocity Central Part";
    [SerializeField] private string vfxLeftVelocityProperty = "Velocity Left Part";
    [SerializeField] private string vfxRightVelocityProperty = "Velocity Right Part";

    private string vfxForwardDirectionProperty = "Forward Direction";
    private string vfxReverseDirectionProperty = "Reverse Direction";

    private int dissolveProgressID;
    private float previousAnimateProgress;
    private float particleDirection = 1f;

    private Coroutine animationRoutine;

    private void OnEnable()
    {
        CacheIDs();

        previousAnimateProgress = animateProgress;

        ApplyValues();
    }

    private void OnDisable()
    {
        StopManualAnimation();
    }

    private void OnValidate()
    {
        CacheIDs();
        ApplyValues();
    }

    private void CacheIDs()
    {
        dissolveProgressID =
            Shader.PropertyToID(
                dissolveProgressProperty);
    }

    private void Update()
    {
        if (Application.isPlaying &&
            doUNeedAnimate &&
            animationRoutine == null)
        {
            float safeDuration =
                Mathf.Max(duration, 0.0001f);

            float safePause =
                Mathf.Max(startEndPause, 0f);

            float cycleDuration =
                safeDuration * 2f +
                safePause * 2f;

            float cycleTime =
                Time.time % cycleDuration;

            if (cycleTime < safeDuration)
            {
                animateProgress =
                    cycleTime / safeDuration;
            }
            else if (cycleTime <
                     safeDuration + safePause)
            {
                animateProgress = 1f;
            }
            else if (cycleTime <
                     safeDuration * 2f +
                     safePause)
            {
                float reverseTime =
                    cycleTime -
                    safeDuration -
                    safePause;

                animateProgress =
                    1f -
                    reverseTime / safeDuration;
            }
            else
            {
                animateProgress = 0f;
            }
        }

        UpdateParticleDirection();
        ApplyValues();
    }

    // =========================================================
    // MANUAL ANIMATION
    // =========================================================

    [ContextMenu("Play Assemble")]
    public void PlayAssemble()
    {
        StartManualAnimation(
            0f,
            1f,
            1f);
    }

    [ContextMenu("Play Dissolve")]
    public void PlayDissolve()
    {
        StartManualAnimation(
            1f,
            0f,
            -1f);
    }

    private void StartManualAnimation(
        float start,
        float end,
        float direction)
    {
        doUNeedAnimate = false;

        StopManualAnimation();

        animateProgress = start;
        previousAnimateProgress = start;
        particleDirection = direction;

        ApplyValues();

        if (Application.isPlaying)
        {
            animationRoutine =
                StartCoroutine(
                    AnimateOnce(
                        start,
                        end));
        }
        else
        {
            animateProgress = end;
            previousAnimateProgress = end;

            ApplyValues();
        }
    }

    private IEnumerator AnimateOnce(
        float start,
        float end)
    {
        float elapsed = 0f;

        float safeDuration =
            Mathf.Max(
                duration,
                0.0001f);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / safeDuration);

            animateProgress =
                Mathf.Lerp(
                    start,
                    end,
                    t);

            ApplyValues();

            yield return null;
        }

        animateProgress = end;
        previousAnimateProgress = end;

        ApplyValues();

        animationRoutine = null;
    }

    private void StopManualAnimation()
    {
        if (animationRoutine == null)
            return;

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }

    // =========================================================
    // PARTICLE DIRECTION
    // =========================================================

    private void UpdateParticleDirection()
    {
        if (animateProgress >
            previousAnimateProgress)
        {
            particleDirection = 1f;
        }
        else if (animateProgress <
                 previousAnimateProgress)
        {
            particleDirection = -1f;
        }

        previousAnimateProgress =
            animateProgress;
    }

    // =========================================================
    // APPLY
    // =========================================================

    private void ApplyValues()
    {
        float centerParticleProgress =
            GetParticleProgress(
                centerFlyStart,
                centerFlyEnd);

        float leftParticleProgress =
            enableLeftPart
                ? GetParticleProgress(
                    leftFlyStart,
                    leftFlyEnd)
                : 0f;

        float rightParticleProgress =
            enableRightPart
                ? GetParticleProgress(
                    rightFlyStart,
                    rightFlyEnd)
                : 0f;

        float centerVelocityProgress =
            GetVelocityProgress(
                centerParticleProgress);

        float leftVelocityProgress =
            enableLeftPart
                ? GetVelocityProgress(
                    leftParticleProgress)
                : 0f;

        float rightVelocityProgress =
            enableRightPart
                ? GetVelocityProgress(
                    rightParticleProgress)
                : 0f;

        float centerDissolveProgress =
            GetDissolveProgress(
                centerDissolveStart,
                centerDissolveEnd);

        float leftDissolveProgress =
            enableLeftPart
                ? GetDissolveProgress(
                    leftDissolveStart,
                    leftDissolveEnd)
                : 0f;

        float rightDissolveProgress =
            enableRightPart
                ? GetDissolveProgress(
                    rightDissolveStart,
                    rightDissolveEnd)
                : 0f;

        SetMaterialProgress(
            centerMaterial,
            centerDissolveProgress);

        SetMaterialProgress(
            leftMaterial,
            leftDissolveProgress);

        SetMaterialProgress(
            rightMaterial,
            rightDissolveProgress);

        if (visualEffect == null)
            return;

        visualEffect.SetFloat(
            vfxParticleDirectionProperty,
            particleDirection);

        bool isForward =
            particleDirection > 0f;

        bool isReverse =
            particleDirection < 0f;

        visualEffect.SetBool(
            vfxForwardDirectionProperty,
            isForward);

        visualEffect.SetBool(
            vfxReverseDirectionProperty,
            isReverse);

        visualEffect.SetFloat(
            vfxCenterParticleProgressProperty,
            centerParticleProgress);

        visualEffect.SetFloat(
            vfxLeftParticleProgressProperty,
            leftParticleProgress);

        visualEffect.SetFloat(
            vfxRightParticleProgressProperty,
            rightParticleProgress);

        visualEffect.SetFloat(
            vfxCenterVelocityProperty,
            centerVelocityProgress);

        visualEffect.SetFloat(
            vfxLeftVelocityProperty,
            leftVelocityProgress);

        visualEffect.SetFloat(
            vfxRightVelocityProperty,
            rightVelocityProgress);

        visualEffect.SetFloat(
            vfxSpawnRateProperty,
            spawnRate);
    }

    private float GetParticleProgress(
        float start,
        float end)
    {
        float sequenceProgress =
            GetSequenceProgress();

        if (Mathf.Approximately(
                start,
                end))
        {
            return sequenceProgress >= end
                ? 1f
                : 0f;
        }

        return Mathf.Clamp01(
            Mathf.InverseLerp(
                start,
                end,
                sequenceProgress));
    }

    private float GetDissolveProgress(
        float start,
        float end)
    {
        float sequenceProgress =
            GetSequenceProgress();

        float progress;

        if (Mathf.Approximately(
                start,
                end))
        {
            progress =
                sequenceProgress >= end
                    ? 1f
                    : 0f;
        }
        else
        {
            progress =
                Mathf.Clamp01(
                    Mathf.InverseLerp(
                        start,
                        end,
                        sequenceProgress));
        }

        return particleDirection > 0f
            ? progress
            : 1f - progress;
    }

    private float GetSequenceProgress()
    {
        return particleDirection > 0f
            ? animateProgress
            : 1f - animateProgress;
    }

    private float GetVelocityProgress(
        float particleProgress)
    {
        if (particleDirection > 0f)
            return particleProgress;

        if (particleProgress <= reverseHold)
            return 0f;

        return Mathf.Clamp01(
            Mathf.InverseLerp(
                reverseHold,
                1f,
                particleProgress));
    }

    private void SetMaterialProgress(
        Material material,
        float progress)
    {
        if (material == null)
            return;

        material.SetFloat(
            dissolveProgressID,
            progress);
    }
}