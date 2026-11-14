using UnityEngine;

public class CardInteraction : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float lookZoomFov = 45f;

    private Card card;
    private CardVisual visual;
    private CardMovement movement;

    private PlayerAnimator ownerAnimator;

    private bool isZoomed;

    public float LookZoomFov =>
        lookZoomFov;

    public bool IsZoomed =>
        isZoomed;

    public void Initialize(
        Card card,
        CardVisual visual,
        CardMovement movement)
    {
        GameLog.Write(
            "CardInteraction",
            $"Initialize card={(card != null ? card.netId.ToString() : "NULL")} visual={(visual != null ? "ok" : "NULL")} movement={(movement != null ? "ok" : "NULL")}");

        this.card = card;
        this.visual = visual;
        this.movement = movement;

        CacheOwnerAnimator();
    }

    public void OnLookEnter()
    {
        if (card == null)
        {
            GameLog.Write(
                "CardInteraction",
                $"OnLookEnter skipped: card is NULL");

            return;
        }

        if (card.IsInRoulette ||
            !card.IsAvailable ||
            isZoomed)
        {
            GameLog.Write(
                "CardInteraction",
                $"OnLookEnter skipped card={card.netId}: inRoulette={card.IsInRoulette} available={card.IsAvailable} zoomed={isZoomed}");

            return;
        }

        CacheOwnerAnimator();

        GameLog.Write(
            "CardInteraction",
            $"OnLookEnter card={card.netId}: hover on");

        visual?.SetHover(true);
        GameAudioManager.Instance?.PlayHover();
    }

    public void OnLookExit()
    {
        if (card == null)
        {
            GameLog.Write(
                "CardInteraction",
                $"OnLookExit skipped: card is NULL");

            return;
        }

        if (card.IsInRoulette ||
            isZoomed)
        {
            GameLog.Write(
                "CardInteraction",
                $"OnLookExit skipped card={card.netId}: inRoulette={card.IsInRoulette} zoomed={isZoomed}");

            return;
        }

        GameLog.Write(
            "CardInteraction",
            $"OnLookExit card={card.netId}: hover off");

        visual?.SetHover(false);
    }

    public bool Interact()
    {
        GameLog.Write(
            "CardInteraction",
            $"Interact card={(card != null ? card.netId.ToString() : "NULL")} zoomed={isZoomed}");

        if (card == null)
        {
            GameLog.Write(
                "CardInteraction",
                $"Interact rejected: card is NULL");

            return false;
        }

        if (card.IsInRoulette ||
            !card.IsAvailable)
        {
            GameLog.Write(
                "CardInteraction",
                $"Interact rejected card={card.netId}: inRoulette={card.IsInRoulette} available={card.IsAvailable}");

            return false;
        }

        if (!isZoomed)
        {
            GameLog.Write(
                "CardInteraction",
                $"Interact card={card.netId}: not zoomed, focusing card");

            ownerAnimator?.EnableCardFocus();
            return FocusCard();
        }

        GameLog.Write(
            "CardInteraction",
            $"Interact card={card.netId}: zoomed, confirming (start attack sequence)");

        ownerAnimator?.DisableCardFocus();

        if (movement != null &&
            movement.IsMoving)
        {
            GameLog.Write(
                "CardInteraction",
                $"Interact rejected card={card.netId}: card is still moving");

            return false;
        }

        if (AttackSequence.Instance == null)
        {
            GameLog.Write(
                "CardInteraction",
                $"Interact rejected card={card.netId}: AttackSequence.Instance is NULL");

            return false;
        }

        ResetState();

        GameAudioManager.Instance?.PlayAccept();
        GameLog.Write(
            "CardInteraction",
            $"Interact card={card.netId}: sending CmdStartSequence");
        AttackSequence.Instance.CmdStartSequence(
            card.netIdentity);

        return true;
    }

    private bool FocusCard()
    {
        GameLog.Write(
            "CardInteraction",
            $"FocusCard card={card.netId}");

        if (!card.GetOwner(
                out Player player))
        {
            GameLog.Write(
                "CardInteraction",
                $"FocusCard failed card={card.netId}: owner player not found");

            return false;
        }

        if (player.cardZoomSocket == null)
        {
            GameLog.Write(
                "CardInteraction",
                $"FocusCard failed card={card.netId}: cardZoomSocket is NULL");

            Debug.LogError(
                "Card: Player.cardZoomSocket is not assigned.",
                card);

            return false;
        }

        movement?.ResetHover();

        isZoomed = true;

        GameLog.Write(
            "CardInteraction",
            $"FocusCard card={card.netId}: zoomed=true owner={player.name} fov={lookZoomFov}");

        ownerAnimator?.EnableCardLift();

        visual?.SetHover(false);

        card.RequestMoveToZoomSocket();

        card.EnableLookZoom(
            card.OwnerNetId,
            lookZoomFov);

        card.LockCameraAt(
            card.OwnerNetId,
            card.transform);

        visual?.StartDescriptionSwap();

        return true;
    }

    public void ResetState()
    {
        GameLog.Write(
            "CardInteraction",
            $"ResetState card={(card != null ? card.netId.ToString() : "NULL")} zoomed={isZoomed}");

        movement?.ResetHover();
        visual?.SetHover(false);

        if (!isZoomed)
            return;

        isZoomed = false;

        GameLog.Write(
            "CardInteraction",
            $"ResetState card={card.netId}: zoomed=false, releasing zoom and camera lock");

        visual?.ShowCardMesh();

        card.DisableLookZoom(
            card.OwnerNetId);

        card.UnlockCamera(
            card.OwnerNetId);
    }

    private void CacheOwnerAnimator()
    {
        if (card == null)
        {
            GameLog.Write(
                "CardInteraction",
                $"CacheOwnerAnimator skipped: card is NULL");

            return;
        }

        if (!card.GetOwner(
                out Player player))
        {
            GameLog.Write(
                "CardInteraction",
                $"CacheOwnerAnimator skipped card={card.netId}: owner player not found");

            return;
        }

        ownerAnimator =
            player.GetComponentInChildren<PlayerAnimator>();

        GameLog.Write(
            "CardInteraction",
            $"CacheOwnerAnimator card={card.netId} owner={player.name} animator={(ownerAnimator != null ? "ok" : "NULL")}");
    }

    private void OnDisable()
    {
        GameLog.Write(
            "CardInteraction",
            $"OnDisable card={(card != null ? card.netId.ToString() : "NULL")} zoomed={isZoomed}");

        movement?.ResetHover();
    }
}