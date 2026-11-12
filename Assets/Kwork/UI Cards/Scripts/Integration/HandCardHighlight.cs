namespace CardCarouselUI
{
    public static class HandCardHighlight
    {
        public static void Set(global::Card card, bool highlighted)
        {
            global::GameLog.Write(
                "HandCardHighlight",
                $"Set card={(card != null ? card.name : "NULL")} highlighted={highlighted}");

            if (card == null)
                return;

            if (highlighted && (card.IsInRoulette || !card.IsAvailable || card.IsZoomed))
            {
                global::GameLog.Write(
                    "HandCardHighlight",
                    $"Set highlight skipped card={card.name} inRoulette={card.IsInRoulette} available={card.IsAvailable} zoomed={card.IsZoomed}");

                return;
            }

            if (card.TryGetComponent(out global::CardVisual visual))
                visual.SetHover(highlighted);
        }
    }
}
