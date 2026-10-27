namespace CardCarouselUI
{
    public static class HandCardHighlight
    {
        public static void Set(global::Card card, bool highlighted)
        {
            if (card == null)
                return;

            if (highlighted && (card.IsInRoulette || !card.IsAvailable || card.IsZoomed))
                return;

            if (card.TryGetComponent(out global::CardVisual visual))
                visual.SetHover(highlighted);
        }
    }
}
