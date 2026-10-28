using System.Collections;
using UnityEngine;

public class TrailFade : MonoBehaviour
{
    [SerializeField] private TrailRenderer[] trails;

    [Header("Stop Detection")] [SerializeField]
    private float stopSpeed = 0.05f;

    [SerializeField] private float stopDelay = 0.1f;

    [Header("Fade")] [SerializeField] private float fadeDuration = 0.5f;

    private Vector3 lastPosition;
    private float stopTimer;

    private Gradient[] originalGradients;
    private float[] originalTimes;

    private Coroutine fadeCoroutine;
    private bool isFading;

    private bool wasMoving;
    
    void Awake()
    {
        lastPosition = transform.position;

        originalGradients = new Gradient[trails.Length];
        originalTimes = new float[trails.Length];

        for (int i = 0; i < trails.Length; i++)
        {
            originalGradients[i] = trails[i].colorGradient;
            originalTimes[i] = trails[i].time;
        }
    }

    void LateUpdate()
    {
        if (isFading) return;

        float speed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;

        lastPosition = transform.position;

        if (fadeCoroutine != null)
            return;

        if (speed > stopSpeed)
        {
            wasMoving = true;
            stopTimer = 0f;
            return;
        }

        if (!wasMoving)
            return;
        
        stopTimer += Time.deltaTime;

        if (stopTimer >= stopDelay)
        {

            wasMoving = false;
            stopTimer = 0f;

            fadeCoroutine = StartCoroutine(Fade());
        }
        
    }

    private void StartFade()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(Fade());
    }

    private IEnumerator Fade()
    {
        isFading = true;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = 1f - t;

            for (int i = 0; i < trails.Length; i++) SetTrailAlpha(i, alpha);

            yield return null;
        }

        for (int i = 0; i < trails.Length; i++)
        {
            SetTrailAlpha(i, 0f);
        }
        
        fadeCoroutine = null;
    }

    private void SetTrailAlpha(int index, float multiplier)
    {
        if (trails[index] == null) return;

        Gradient gradient = trails[index].colorGradient;
        GradientAlphaKey[] alphaKeys = gradient.alphaKeys;

        GradientAlphaKey[] originalAlphaKeys =
            originalGradients[index].alphaKeys;

        for (int i = 0; i < alphaKeys.Length; i++)
        {
            alphaKeys[i].alpha =
                originalAlphaKeys[i].alpha * multiplier;
        }

        gradient.SetKeys(gradient.colorKeys, alphaKeys);

        trails[index].colorGradient = gradient;
    }
}