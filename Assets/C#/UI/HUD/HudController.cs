using TMPro;
using UnityEngine;

public class HudController : MonoBehaviour
{
    public static HudController Instance { get; private set; }

    [SerializeField] private TMP_Text hintText;
    [SerializeField] private CardInfoPanel cardInfoPanel;
    [SerializeField] private TMP_Text defenseTimerText;

    private void Awake()
    {
        GameLog.Write(
            "HudController",
            $"Awake hintText={(hintText != null ? hintText.name : "NULL")} cardInfoPanel={(cardInfoPanel != null ? cardInfoPanel.name : "NULL")} defenseTimerText={(defenseTimerText != null ? defenseTimerText.name : "NULL")} existingInstance={(Instance != null ? Instance.name : "NULL")}");

        if (Instance != null &&
            Instance != this)
        {
            GameLog.Write(
                "HudController",
                "Awake duplicate HudController destroyed");

            Destroy(gameObject);
            return;
        }

        Instance = this;

        HideDefenseTimer();
    }

    private void OnDestroy()
    {
        GameLog.Write(
            "HudController",
            $"OnDestroy isCurrentInstance={Instance == this}");

        if (Instance == this)
            Instance = null;
    }

    // =========================================================
    // HINT
    // =========================================================

    public void SetHint(
        string text)
    {
        GameLog.Write(
            "HudController",
            $"SetHint text={(text ?? "NULL")} hintText={(hintText != null ? hintText.name : "NULL")}");

        if (hintText == null)
        {
            GameLog.Write(
                "HudController",
                "SetHint skipped: hintText is not assigned");

            return;
        }

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
        GameLog.Write(
            "HudController",
            $"ShowCardInfo card={(data != null ? data.name : "NULL")} panel={(cardInfoPanel != null ? cardInfoPanel.name : "NULL")}");

        if (cardInfoPanel == null)
        {
            GameLog.Write(
                "HudController",
                "ShowCardInfo failed: CardInfoPanel is not assigned");

            Debug.LogError(
                "HudController: CardInfoPanel is not assigned.",
                this);

            return;
        }

        cardInfoPanel.Show(data);
    }

    public void HideCardInfo()
    {
        GameLog.Write(
            "HudController",
            $"HideCardInfo panel={(cardInfoPanel != null ? cardInfoPanel.name : "NULL")}");

        if (cardInfoPanel == null)
        {
            GameLog.Write(
                "HudController",
                "HideCardInfo skipped: CardInfoPanel is not assigned");

            return;
        }

        cardInfoPanel.Hide();
    }

    // =========================================================
    // DEFENSE TIMER
    // =========================================================

    public void ShowDefenseTimer(
        float time)
    {
        GameLog.Write(
            "HudController",
            $"ShowDefenseTimer time={time} defenseTimerText={(defenseTimerText != null ? defenseTimerText.name : "NULL")}");

        if (defenseTimerText == null)
        {
            GameLog.Write(
                "HudController",
                "ShowDefenseTimer skipped: defenseTimerText is not assigned");

            return;
        }

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
        GameLog.Write(
            "HudController",
            $"HideDefenseTimer defenseTimerText={(defenseTimerText != null ? defenseTimerText.name : "NULL")}");

        if (defenseTimerText == null)
        {
            GameLog.Write(
                "HudController",
                "HideDefenseTimer skipped: defenseTimerText is not assigned");

            return;
        }

        defenseTimerText.text =
            string.Empty;

        defenseTimerText.gameObject.SetActive(
            false);
    }
}
