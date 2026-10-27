using System.Collections;
using TMPro;
using UnityEngine;

public class HudController : MonoBehaviour
{
    public static HudController Instance { get; private set; }

    [SerializeField] private TMP_Text hintText;
    [SerializeField] private CardInfoPanel cardInfoPanel;
    [SerializeField] private TMP_Text defenseTimerText;

    [Header("Blind")]
    [SerializeField] private CanvasGroup blindOverlay;
    [SerializeField] private float blindFadeDuration = 0.3f;

    private Coroutine blindRoutine;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        HideDefenseTimer();

        if (blindOverlay != null)
        {
            blindOverlay.alpha = 0f;
            blindOverlay.blocksRaycasts = false;
            blindOverlay.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // =========================================================
    // HINT
    // =========================================================

    public void SetHint(
        string text)
    {
        if (hintText == null)
            return;

        bool show =
            !string.IsNullOrEmpty(text);

        hintText.gameObject.SetActive(
            show);

        hintText.text =
            show
                ? text
                : string.Empty;
    }

    // =========================================================
    // CARD INFO
    // =========================================================

    public void ShowCardInfo(
        CardData data)
    {
        if (cardInfoPanel == null)
        {
            Debug.LogError(
                "HudController: CardInfoPanel is not assigned.",
                this);

            return;
        }

        cardInfoPanel.Show(data);
    }

    public void HideCardInfo()
    {
        if (cardInfoPanel == null)
            return;

        cardInfoPanel.Hide();
    }

    // =========================================================
    // DEFENSE TIMER
    // =========================================================

    public void ShowDefenseTimer(
        float time)
    {
        if (defenseTimerText == null)
            return;

        defenseTimerText.gameObject.SetActive(
            true);

        SetDefenseTimer(time);
    }

    public void SetDefenseTimer(
        float time)
    {
        if (defenseTimerText == null)
            return;

        time =
            Mathf.Max(
                0f,
                time);

        defenseTimerText.text =
            Mathf.CeilToInt(
                time).ToString();
    }

    public void HideDefenseTimer()
    {
        if (defenseTimerText == null)
            return;

        defenseTimerText.text =
            string.Empty;

        defenseTimerText.gameObject.SetActive(
            false);
    }

    // =========================================================
    // BLIND
    // =========================================================

    public void ShowBlind(
        float duration)
    {
        if (blindOverlay == null)
            return;

        if (blindRoutine != null)
            StopCoroutine(blindRoutine);

        blindRoutine =
            StartCoroutine(
                BlindRoutine(duration));
    }

    private IEnumerator BlindRoutine(
        float duration)
    {
        blindOverlay.gameObject.SetActive(true);

        yield return FadeBlind(
            blindOverlay.alpha,
            1f);

        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                duration -
                blindFadeDuration * 2f));

        yield return FadeBlind(
            blindOverlay.alpha,
            0f);

        blindOverlay.gameObject.SetActive(false);

        blindRoutine = null;
    }

    private IEnumerator FadeBlind(
        float from,
        float to)
    {
        float time = 0f;

        while (time < blindFadeDuration)
        {
            time += Time.deltaTime;

            blindOverlay.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    time / blindFadeDuration);

            yield return null;
        }

        blindOverlay.alpha = to;
    }
}