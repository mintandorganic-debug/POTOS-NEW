using UnityEngine;

public class CardInteraction : MonoBehaviour
{
    private Card card;
    private CardVisual visual;
    private CardMovement movement;

    public void Initialize(
        Card card,
        CardVisual visual,
        CardMovement movement)
    {
        this.card = card;
        this.visual = visual;
        this.movement = movement;
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

    public void ResetState()
    {
        movement?.ResetHover();
        visual?.SetHover(false);
    }

    private void OnDisable()
    {
        movement?.ResetHover();
    }
}
