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

    private InteractableObject currentTarget;

    private Button currentButton;

    private EventSystem eventSystem;
    private HudController hudController;

    private Card selectedCard;

    private void Awake()
    {
        eventSystem = EventSystem.current;

        if (owner == null)
            owner = GetComponent<Player>();

        GameLog.Write(
            "Interactor",
            $"Awake object={name} owner={(owner != null ? owner.name : "NULL")} eventSystem={(eventSystem != null ? "ok" : "NULL")} camera={(playerCamera != null ? playerCamera.name : "NULL")}");
    }

    public override void OnStartLocalPlayer()
    {
        GameLog.Write(
            "Interactor",
            $"OnStartLocalPlayer netId={netId} owner={(owner != null ? owner.name : "NULL")}");

        base.OnStartLocalPlayer();

        hudController =
            FindFirstObjectByType<HudController>();

        GameLog.Write(
            "Interactor",
            $"OnStartLocalPlayer netId={netId} hudController={(hudController != null ? "ok" : "NULL")}");
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

        GameLog.Write(
            "Interactor",
            $"DetectTarget look target changed: {(currentTarget != null ? currentTarget.name : "NULL")} -> {(newTarget != null ? newTarget.name : "NULL")}");

        currentTarget?.OnLookExit();

        currentTarget = newTarget;

        currentTarget?.OnLookEnter();

        UpdateHint();
    }

    // =========================================================
    // HINT
    // =========================================================

    private void UpdateHint()
    {
        if (hudController == null)
        {
            GameLog.Write(
                "Interactor",
                $"UpdateHint skipped: hudController is NULL");

            return;
        }

        if (currentTarget is Card card &&
            !card.IsAvailable)
        {
            GameLog.Write(
                "Interactor",
                $"UpdateHint: unavailable card netId={card.netId} reason={card.Reason}, showing hint");

            hudController.SetHint(
                GetUnavailableReasonText(
                    card.Reason));

            return;
        }

        GameLog.Write(
            "Interactor",
            $"UpdateHint: clearing hint");

        hudController.SetHint(null);
    }

    private string GetUnavailableReasonText(
        Card.UnavailableReason reason)
    {
        GameLog.Write(
            "Interactor",
            $"GetUnavailableReasonText reason={reason}");

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
        GameLog.Write(
            "Interactor",
            $"HandleInteraction target={(currentTarget != null ? currentTarget.name : "NULL")} selectedCard={(selectedCard != null ? selectedCard.name : "NULL")}");

        if (currentTarget is Card currentCard)
        {
            if (!currentCard.IsAvailable)
            {
                GameLog.Write(
                    "Interactor",
                    $"HandleInteraction ignored: card netId={currentCard.netId} is unavailable");

                return;
            }

            if (selectedCard != null &&
                selectedCard != currentCard &&
                selectedCard.IsZoomed)
            {
                GameLog.Write(
                    "Interactor",
                    $"HandleInteraction: returning previously selected card netId={selectedCard.netId} to owner");

                selectedCard.ReturnToOwner();
                selectedCard = null;
            }

            GameLog.Write(
                "Interactor",
                $"HandleInteraction: interacting with card netId={currentCard.netId}");

            currentCard.Interact();

            if (currentCard.IsZoomed)
            {
                GameLog.Write(
                    "Interactor",
                    $"HandleInteraction: selectedCard set to netId={currentCard.netId}");

                selectedCard = currentCard;
            }
            else if (selectedCard == currentCard)
            {
                GameLog.Write(
                    "Interactor",
                    $"HandleInteraction: selectedCard cleared (netId={currentCard.netId} not zoomed)");

                selectedCard = null;
            }

            return;
        }

        if (selectedCard != null &&
            selectedCard.IsZoomed)
        {
            GameLog.Write(
                "Interactor",
                $"HandleInteraction: non-card target, returning selected card netId={selectedCard.netId} to owner");

            selectedCard.ReturnToOwner();
            selectedCard = null;
        }

        if (currentTarget != null)
        {
            GameLog.Write(
                "Interactor",
                $"HandleInteraction: interacting with target={(currentTarget != null ? currentTarget.name : "NULL")}");

            currentTarget.Interact();
        }
        else
        {
            GameLog.Write(
                "Interactor",
                $"HandleInteraction: nothing under look to interact with");

        }
    }

    // =========================================================
    // CANCEL
    // =========================================================

    private void CancelSelection()
    {
        GameLog.Write(
            "Interactor",
            $"CancelSelection target={(currentTarget != null ? currentTarget.name : "NULL")} selectedCard={(selectedCard != null ? selectedCard.name : "NULL")}");

        if (selectedCard != null &&
            selectedCard.IsZoomed)
        {
            GameLog.Write(
                "Interactor",
                $"CancelSelection: returning selected card netId={selectedCard.netId} to owner");

            selectedCard.ReturnToOwner();
            selectedCard = null;
            return;
        }

        if (currentTarget is Card card &&
            card.IsZoomed)
        {
            GameLog.Write(
                "Interactor",
                $"CancelSelection: returning looked-at zoomed card netId={card.netId} to owner");

            card.ReturnToOwner();
            return;
        }

        if (AttackSequence.Instance == null)
        {
            GameLog.Write(
                "Interactor",
                $"CancelSelection skipped: AttackSequence.Instance is NULL");

            return;
        }

        GameLog.Write(
            "Interactor",
            $"CancelSelection: sending CmdCancelSequence");

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
        GameLog.Write(
            "Interactor",
            $"ClickButton button={(button != null ? button.name : "NULL")}");

        if (button == null)
        {
            GameLog.Write(
                "Interactor",
                $"ClickButton skipped: button is NULL");

            return;
        }

        button.onClick.Invoke();
    }
}