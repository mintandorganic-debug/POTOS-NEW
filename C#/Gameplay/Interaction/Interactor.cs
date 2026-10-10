using System.Collections;
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

    [Header("Card Info")]
    [SerializeField] private float cardInfoDelay = 0.5f;

    private InteractableObject currentTarget;

    private Button currentButton;

    private EventSystem eventSystem;
    private HudController hudController;

    private Card hoveredCard;
    private Card selectedCard;

    private Coroutine cardInfoRoutine;

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
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        DetectTarget();
        DetectButton();

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
            (
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame
            )
            ||
            (
                Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame
            );
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
                Keyboard.current.qKey.wasPressedThisFrame
            );
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

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxDistance,
                interactionMask))
        {
            newTarget =
                hit.collider
                    .GetComponentInParent<InteractableObject>();
        }

        if (newTarget == currentTarget)
            return;

        currentTarget?.OnLookExit();

        StopCardInfoRoutine();

        hoveredCard = null;

        currentTarget = newTarget;

        currentTarget?.OnLookEnter();

        UpdateHint();

        if (currentTarget is Card card &&
            card.IsAvailable)
        {
            hoveredCard = card;

            cardInfoRoutine =
                StartCoroutine(
                    ShowCardInfoAfterDelay(card));
        }
        else
        {
            hudController?.HideCardInfo();
        }
    }

    // =========================================================
    // CARD INFO
    // =========================================================

    private IEnumerator ShowCardInfoAfterDelay(
        Card card)
    {
        yield return new WaitForSeconds(
            cardInfoDelay);

        if (currentTarget != card ||
            hoveredCard != card ||
            !card.IsAvailable ||
            card.Data == null)
        {
            yield break;
        }

        hudController?.ShowCardInfo(
            card.Data);

        cardInfoRoutine = null;
    }

    private void StopCardInfoRoutine()
    {
        if (cardInfoRoutine == null)
            return;

        StopCoroutine(
            cardInfoRoutine);

        cardInfoRoutine = null;
    }

    // =========================================================
    // HINT
    // =========================================================

    private void UpdateHint()
    {
        if (hudController == null)
            return;

        if (currentTarget is Card card &&
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
        if (currentTarget is Card currentCard)
        {
            if (!currentCard.IsAvailable)
                return;

            if (selectedCard != null &&
                selectedCard != currentCard &&
                selectedCard.IsZoomed)
            {
                selectedCard.ReturnToOwner();
                selectedCard = null;
            }

            currentCard.Interact();

            if (currentCard.IsZoomed)
            {
                selectedCard = currentCard;
            }
            else if (selectedCard == currentCard)
            {
                selectedCard = null;
            }

            return;
        }

        if (selectedCard != null &&
            selectedCard.IsZoomed)
        {
            selectedCard.ReturnToOwner();
            selectedCard = null;
        }

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
        if (selectedCard != null &&
            selectedCard.IsZoomed)
        {
            selectedCard.ReturnToOwner();
            selectedCard = null;
            return;
        }

        if (currentTarget is Card card &&
            card.IsZoomed)
        {
            card.ReturnToOwner();
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