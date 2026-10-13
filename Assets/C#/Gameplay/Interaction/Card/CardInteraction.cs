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
        this.card = card;
        this.visual = visual;
        this.movement = movement;

        CacheOwnerAnimator();
    }

    public void OnLookEnter()
    {
        if (card == null)
            return;

        ownerAnimator?.EnableCardLift();

        if (card.IsInRoulette ||
            !card.IsAvailable ||
            isZoomed)
        {
            return;
        }

        CacheOwnerAnimator();

        visual?.SetHover(true);
        movement?.StartHover();
    }

    public void OnLookExit()
    {
        if (card == null)
            return;

        if (card.IsInRoulette ||
            isZoomed)
        {
            return;
        }

        visual?.SetHover(false);
        movement?.ResetHover();
    }

    public bool Interact()
    {
        if (card == null)
            return false;

        if (card.IsInRoulette ||
            !card.IsAvailable)
        {
            return false;
        }

        if (!isZoomed)
        {
            ownerAnimator?.EnableCardFocus();
            return FocusCard();
        }

        ownerAnimator?.DisableCardFocus();

        if (movement != null &&
            movement.IsMoving)
        {
            return false;
        }

        if (AttackSequence.Instance == null)
            return false;

        ResetState();

        AttackSequence.Instance.CmdStartSequence(
            card.netIdentity);

        return true;
    }

    private bool FocusCard()
    {
        if (!card.TryGetOwner(
                out Player player))
        {
            return false;
        }

        if (player.cardZoomSocket == null)
        {
            Debug.LogError(
                "Card: Player.cardZoomSocket is not assigned.",
                card);

            return false;
        }

        movement?.ResetHover();

        isZoomed = true;

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
        movement?.ResetHover();
        visual?.SetHover(false);

        if (!isZoomed)
            return;

        isZoomed = false;

        visual?.ShowCardMesh();

        card.DisableLookZoom(
            card.OwnerNetId);

        card.UnlockCamera(
            card.OwnerNetId);
    }

    private void CacheOwnerAnimator()
    {
        if (card == null)
            return;

        if (!card.TryGetOwner(
                out Player player))
        {
            return;
        }

        ownerAnimator =
            player.GetComponentInChildren<PlayerAnimator>();
    }

    private void OnDisable()
    {
        movement?.ResetHover();
    }
}