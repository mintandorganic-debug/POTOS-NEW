using UnityEngine;

public class RouletteCardAttachment
{
    private readonly IRouletteMotion motion;

    private Transform attachedCard;

    public RouletteCardAttachment(
        IRouletteMotion motion)
    {
        this.motion = motion;

        GameLog.Write(
            "RouletteCardAttachment",
            "Created");
    }

    public void AttachCard(
        Transform card)
    {
        GameLog.Write(
            "RouletteCardAttachment",
            $"AttachCard card={(card != null ? card.name : "NULL")} cardMarker={(motion.CardMarker != null ? motion.CardMarker.name : "NULL")}");

        if (card == null ||
            motion.CardMarker == null)
        {
            GameLog.Write(
                "RouletteCardAttachment",
                "AttachCard skipped: card or cardMarker is null");

            return;
        }

        AttachCardInternal(
            card,
            motion.CardMarker);
    }

    public void AttachDefenseCard(
        Transform card)
    {
        GameLog.Write(
            "RouletteCardAttachment",
            $"AttachDefenseCard card={(card != null ? card.name : "NULL")} defenseCardMarker={(motion.DefenseCardMarker != null ? motion.DefenseCardMarker.name : "NULL")}");

        if (card == null ||
            motion.DefenseCardMarker == null)
        {
            GameLog.Write(
                "RouletteCardAttachment",
                "AttachDefenseCard skipped: card or defenseCardMarker is null");

            return;
        }

        AttachCardInternal(
            card,
            motion.DefenseCardMarker);
    }

    private void AttachCardInternal(
        Transform card,
        Transform marker)
    {
        GameLog.Write(
            "RouletteCardAttachment",
            $"AttachCardInternal card={(card != null ? card.name : "NULL")} marker={(marker != null ? marker.name : "NULL")}");

        Card cardComponent =
            card.GetComponent<Card>();

        if (cardComponent != null)
        {
            GameLog.Write(
                "RouletteCardAttachment",
                $"AttachCardInternal calling OnAttachmentToRoulette card={card.name}");

            cardComponent.OnAttachmentToRoulette();
        }

        attachedCard = card;

        card.SetParent(
            marker,
            false);

        card.localPosition =
            Vector3.zero;

        card.localRotation =
            Quaternion.identity;

        card.localScale =
            Vector3.one;
    }

    public void DetachCard(
        Transform card)
    {
        GameLog.Write(
            "RouletteCardAttachment",
            $"DetachCard card={(card != null ? card.name : "NULL")} wasAttachedCard={attachedCard == card}");

        if (card == null)
        {
            GameLog.Write(
                "RouletteCardAttachment",
                "DetachCard skipped: card is null");

            return;
        }

        if (attachedCard == card)
        {
            attachedCard = null;
        }

        card.SetParent(
            null,
            true);
    }

    public void ResetMarker()
    {
        GameLog.Write(
            "RouletteCardAttachment",
            $"ResetMarker cardMarker={(motion.CardMarker != null ? motion.CardMarker.name : "NULL")} defaultLocalPosition={motion.DefaultCardMarkerLocalPosition}");

        if (motion.CardMarker == null)
        {
            GameLog.Write(
                "RouletteCardAttachment",
                "ResetMarker skipped: cardMarker is null");

            return;
        }

        motion.CardMarker.localPosition =
            motion.DefaultCardMarkerLocalPosition;
    }
}
