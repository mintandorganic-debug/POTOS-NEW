using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PoisonSwapWindow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform targetRow;
    [SerializeField] private RectTransform ownRow;
    [SerializeField] private SwapCardButton cardTemplate;

    [Header("Controls")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text counterText;

    [Header("Visual")]
    [SerializeField] private Texture cardBack;

    private readonly List<SwapCardButton> targetButtons = new();
    private readonly List<SwapCardButton> ownButtons = new();

    private Player player;
    private PlayerHand hand;

    private bool isOpen;

    public bool IsOpen =>
        isOpen;

    private void Awake()
    {
        if (cardTemplate != null)
            cardTemplate.gameObject.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (hand != null)
            hand.SwapChanged -= Refresh;
    }

    public void Initialize(
        Player player)
    {
        if (hand != null)
            hand.SwapChanged -= Refresh;

        this.player = player;

        hand =
            player != null
                ? player.Hand
                : null;

        if (hand != null)
            hand.SwapChanged += Refresh;

        Refresh();
    }

    private void Update()
    {
        if (!isOpen ||
            hand == null)
        {
            return;
        }

        double remaining =
            hand.SwapEndTime -
            NetworkTime.time;

        if (remaining <= 0 ||
            !hand.IsSwapOpen)
        {
            Close();
            return;
        }

        if (timerText != null)
        {
            timerText.text =
                Mathf.CeilToInt(
                    (float)remaining).ToString();
        }
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    private void Refresh()
    {
        if (hand != null &&
            hand.IsSwapOpen &&
            hand.TryGetSwapTarget(
                out PlayerHand target))
        {
            Open(target);
            return;
        }

        Close();
    }

    private void Open(
        PlayerHand target)
    {
        Clear();

        foreach (Card card in target.GetCards())
        {
            if (card.IsInRoulette)
                continue;

            targetButtons.Add(
                CreateButton(
                    targetRow,
                    card,
                    cardBack));
        }

        foreach (Card card in hand.GetCards())
        {
            if (card.IsInRoulette)
                continue;

            ownButtons.Add(
                CreateButton(
                    ownRow,
                    card,
                    card.Data != null
                        ? card.Data.texture
                        : null));
        }

        isOpen = true;

        SetVisible(true);
        SetLookEnabled(false);
        UpdateControls();
    }

    private void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;

        Clear();
        SetVisible(false);
        SetLookEnabled(true);
    }

    private void Clear()
    {
        foreach (SwapCardButton button in targetButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        foreach (SwapCardButton button in ownButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        targetButtons.Clear();
        ownButtons.Clear();
    }

    private SwapCardButton CreateButton(
        RectTransform row,
        Card card,
        Texture texture)
    {
        SwapCardButton button =
            Instantiate(
                cardTemplate,
                row);

        button.gameObject.SetActive(true);

        button.Setup(
            card.netId,
            texture,
            OnCardClicked);

        return button;
    }

    // =========================================================
    // SELECTION
    // =========================================================

    private void OnCardClicked(
        SwapCardButton button)
    {
        if (button == null)
            return;

        List<SwapCardButton> group =
            ownButtons.Contains(button)
                ? ownButtons
                : targetButtons;

        if (!button.IsSelected &&
            CountSelected(group) >= hand.SwapMaxCards)
        {
            return;
        }

        button.SetSelected(
            !button.IsSelected);

        UpdateControls();
    }

    private int CountSelected(
        List<SwapCardButton> buttons)
    {
        int count = 0;

        foreach (SwapCardButton button in buttons)
        {
            if (button.IsSelected)
                count++;
        }

        return count;
    }

    private uint[] GetSelected(
        List<SwapCardButton> buttons)
    {
        List<uint> ids =
            new List<uint>();

        foreach (SwapCardButton button in buttons)
        {
            if (button.IsSelected)
                ids.Add(button.CardNetId);
        }

        return ids.ToArray();
    }

    private void UpdateControls()
    {
        int own =
            CountSelected(ownButtons);

        int target =
            CountSelected(targetButtons);

        if (counterText != null)
        {
            counterText.text =
                $"{own} / {target}";
        }

        if (confirmButton != null)
        {
            confirmButton.interactable =
                own > 0 &&
                own == target;
        }
    }

    private void Confirm()
    {
        if (!isOpen ||
            hand == null)
        {
            return;
        }

        uint[] own =
            GetSelected(ownButtons);

        uint[] target =
            GetSelected(targetButtons);

        if (own.Length == 0 ||
            own.Length != target.Length)
        {
            return;
        }

        hand.CmdRequestSwap(
            own,
            target);

        Close();
    }

    private void Cancel()
    {
        if (!isOpen ||
            hand == null)
        {
            return;
        }

        hand.CmdCancelSwap();

        Close();
    }

    // =========================================================
    // VISUAL
    // =========================================================

    private void SetVisible(
        bool value)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha =
            value
                ? 1f
                : 0f;

        canvasGroup.interactable = value;
        canvasGroup.blocksRaycasts = value;
    }

    private void SetLookEnabled(
        bool value)
    {
        if (player == null ||
            player.cameraController == null)
        {
            return;
        }

        player.cameraController.SetLookEnabled(value);
    }
}
