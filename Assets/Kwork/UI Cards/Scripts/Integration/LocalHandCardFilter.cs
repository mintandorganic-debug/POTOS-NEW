namespace CardCarouselUI
{
    public static class LocalHandCardFilter
    {
        public static bool IsInLocalHand(global::Card card)
        {
            return card.Data != null &&
                   !card.IsInRoulette &&
                   card.TryGetOwner(out global::Player owner) &&
                   owner.isOwned;
        }
    }
}
