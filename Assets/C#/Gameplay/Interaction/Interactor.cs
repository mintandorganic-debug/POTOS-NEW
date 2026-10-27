using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Interactor : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Player owner;

    [Header("Interaction")]
    [SerializeField] private float maxDistance = 3f;
    [SerializeField] private LayerMask interactionMask = ~0;

    [Header("Hand")]
    [SerializeField] private HandCarousel handCarousel;
    [SerializeField] private float handHideDelay = 0.3f;
    [SerializeField] private Vector3 handZonePadding = new Vector3(0.06f, 0.09f, 0.04f);

    private InteractableObject currentTarget;

    private Button currentButton;

    private EventSystem eventSystem;
    private HudController hudController;
    private PoisonSwapWindow swapWindow;

    private float handHideTimer;
    private bool handSuppressed;
    private bool handPinned;
    private bool lookingAtHand;

    private HandZone handZone;
    private Card highlightedCard;

    private void Awake()
    {
        eventSystem = EventSystem.current;

        if (owner == null)
            owner = GetComponent<Player>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        hudController =
            FindFirstObjectByType<HudController>();

        swapWindow =
            FindFirstObjectByType<PoisonSwapWindow>(
                FindObjectsInactive.Include);

        if (swapWindow != null)
            swapWindow.Initialize(owner);

        if (handCarousel != null)
        {
            handCarousel.Initialize(
                owner != null
                    ? owner.Hand
                    : null);

            handCarousel.CenterChanged -=
                OnHandCenterChanged;

            handCarousel.CenterChanged +=
                OnHandCenterChanged;

            if (owner != null)
            {
                handCarousel.SetAnchors(
                    playerCamera,
                    owner.cardSockets);
            }
        }

        CreateHandZone();
    }

    public override void OnStopLocalPlayer()
    {
        if (handCarousel != null)
        {
            handCarousel.CenterChanged -=
                OnHandCenterChanged;
        }

        base.OnStopLocalPlayer();
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (swapWindow != null &&
            swapWindow.IsOpen)
        {
            if (handCarousel != null &&
                handCarousel.IsOpen)
            {
                HideHand();
            }

            return;
        }

        DetectTarget();
        DetectButton();

        UpdateHand();

        if (IsHandTogglePressed())
        {
            ToggleHand();
            return;
        }

        if (IsCancelPressed())
        {
            CancelSelection();
            return;
        }

        if (!IsInteractPressed())
            return;

        if (currentButton != null)
        {
            ClickButton(currentButton);
            return;
        }

        HandleInteraction();
    }

    // =========================================================
    // INPUT
    // =========================================================

    private bool IsInteractPressed()
    {
        return
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;
    }

    private bool IsHandTogglePressed()
    {
        return
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private bool IsCancelPressed()
    {
        return
            (
                Mouse.current != null &&
                Mouse.current.rightButton.wasPressedThisFrame
            )
            ||
            (
                Keyboard.current != null &&
                (
                    Keyboard.current.qKey.wasPressedThisFrame ||
                    Keyboard.current.escapeKey.wasPressedThisFrame
                )
            );
    }

    private int GetScrollDirection()
    {
        if (Mouse.current != null)
        {
            float scroll =
                Mouse.current.scroll.ReadValue().y;

            if (scroll > 0f)
                return -1;

            if (scroll < 0f)
                return 1;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame)
                return -1;

            if (Keyboard.current.dKey.wasPressedThisFrame)
                return 1;
        }

        return 0;
    }

    // =========================================================
    // TARGET DETECTION
    // =========================================================

    private void DetectTarget()
    {
        InteractableObject newTarget = null;

        Ray ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward);

        bool hand = false;

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxDistance,
                interactionMask))
        {
            newTarget =
                hit.collider
                    .GetComponentInParent<InteractableObject>();

            hand =
                (handZone != null &&
                 hit.collider.gameObject == handZone.gameObject) ||
                TryGetOwnCard(
                    newTarget,
                    out _);
        }

        if (hand)
            newTarget = null;

        if (hand != lookingAtHand)
        {
            lookingAtHand = hand;

            if (lookingAtHand)
                OnHandLooked();
        }

        if (newTarget == currentTarget)
            return;

        currentTarget?.OnLookExit();

        currentTarget = newTarget;

        currentTarget?.OnLookEnter();

        UpdateHint();
    }

    private bool TryGetOwnCard(
        InteractableObject target,
        out Card card)
    {
        card = target as Card;

        if (card == null ||
            card.IsInRoulette)
        {
            card = null;
            return false;
        }

        if (!card.TryGetOwner(
                out Player cardOwner) ||
            cardOwner != owner)
        {
            card = null;
            return false;
        }

        return true;
    }

    // =========================================================
    // HAND
    // =========================================================

    private void CreateHandZone()
    {
        if (owner == null ||
            owner.cardSockets == null ||
            owner.cardSockets.Count == 0 ||
            owner.cardSockets[0] == null)
        {
            return;
        }

        GameObject zoneObject =
            new GameObject("Hand Zone");

        zoneObject.transform.SetParent(
            owner.cardSockets[0].transform.parent,
            false);

        handZone =
            zoneObject.AddComponent<HandZone>();

        handZone.Initialize(
            owner,
            owner.cardSockets,
            handZonePadding);
    }

    private void OnHandLooked()
    {
        handHideTimer = 0f;

        if (handCarousel == null ||
            handSuppressed ||
            handCarousel.IsOpen)
        {
            return;
        }

        handCarousel.Show(null);

        UpdateHighlight();
        UpdateHint();
    }

    private void UpdateHand()
    {
        if (handCarousel == null)
            return;

        if (handCarousel.IsOpen)
        {
            int scroll =
                GetScrollDirection();

            if (scroll != 0)
                handCarousel.Scroll(scroll);
        }

        if (lookingAtHand)
        {
            handHideTimer = 0f;
            return;
        }

        handSuppressed = false;

        if (!handCarousel.IsOpen ||
            handPinned)
        {
            return;
        }

        handHideTimer += Time.deltaTime;

        if (handHideTimer < handHideDelay)
            return;

        handHideTimer = 0f;

        HideHand();
    }

    private void ToggleHand()
    {
        if (handCarousel == null)
            return;

        if (handCarousel.IsOpen)
        {
            handSuppressed = lookingAtHand;

            HideHand();

            return;
        }

        handPinned = true;
        handSuppressed = false;
        handHideTimer = 0f;

        handCarousel.Show(null);

        UpdateHighlight();
        UpdateHint();
    }

    private void HideHand()
    {
        if (handCarousel == null)
            return;

        handPinned = false;

        handCarousel.Hide();

        UpdateHighlight();

        UpdateHint();
    }

    private void OnHandCenterChanged(
        Card card)
    {
        UpdateHighlight();
        UpdateHint();
    }

    private void UpdateHighlight()
    {
        Card card =
            handCarousel != null &&
            handCarousel.IsOpen
                ? handCarousel.CenterCard
                : null;

        if (card == highlightedCard)
            return;

        if (highlightedCard != null)
            highlightedCard.SetHighlight(false);

        highlightedCard = card;

        if (highlightedCard != null)
            highlightedCard.SetHighlight(true);
    }

    // =========================================================
    // HINT
    // =========================================================

    private void UpdateHint()
    {
        if (hudController == null)
            return;

        Card card =
            handCarousel != null &&
            handCarousel.IsOpen
                ? handCarousel.CenterCard
                : currentTarget as Card;

        if (card != null &&
            !card.IsAvailable)
        {
            hudController.SetHint(
                GetUnavailableReasonText(
                    card.Reason));

            return;
        }

        hudController.SetHint(null);
    }

    private string GetUnavailableReasonText(
        Card.UnavailableReason reason)
    {
        return reason switch
        {
            Card.UnavailableReason.SampleReason =>
                "Card is unavailable",

            Card.UnavailableReason.WrongTurnPhase =>
                "Card is unavailable",

            _ =>
                string.Empty
        };
    }

    // =========================================================
    // INTERACTION
    // =========================================================

    private void HandleInteraction()
    {
        if (handCarousel != null &&
            handCarousel.IsOpen)
        {
            Card card =
                handCarousel.CenterCard;

            if (card == null ||
                !card.IsPlayable)
            {
                return;
            }

            card.Interact();

            handSuppressed = true;

            HideHand();

            return;
        }

        if (currentTarget is Card)
            return;

        if (currentTarget != null)
        {
            currentTarget.Interact();
        }
    }

    // =========================================================
    // CANCEL
    // =========================================================

    private void CancelSelection()
    {
        if (handCarousel != null &&
            handCarousel.IsOpen)
        {
            handSuppressed = true;

            HideHand();

            return;
        }

        if (AttackSequence.Instance == null)
            return;

        AttackSequence.Instance.CmdCancelSequence();
    }

    // =========================================================
    // WORLD SPACE UI
    // =========================================================

    private void DetectButton()
    {
        currentButton = null;

        if (eventSystem == null)
            return;

        PointerEventData pointerData =
            new PointerEventData(eventSystem);

        pointerData.position =
            new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.5f);

        List<RaycastResult> results =
            new List<RaycastResult>();

        eventSystem.RaycastAll(
            pointerData,
            results);

        foreach (RaycastResult result in results)
        {
            Button button =
                result.gameObject
                    .GetComponent<Button>();

            if (button == null ||
                !button.interactable)
            {
                continue;
            }

            currentButton = button;
            break;
        }
    }

    private void ClickButton(
        Button button)
    {
        if (button == null)
            return;

        button.onClick.Invoke();
    }
}
