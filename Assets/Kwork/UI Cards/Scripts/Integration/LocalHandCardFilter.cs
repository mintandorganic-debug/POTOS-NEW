using UnityEngine;

namespace CardCarouselUI
{
    public static class LocalHandCardFilter
    {
        public static bool IsAnyCardSelected()
        {
            foreach (global::Card card in Object.FindObjectsByType<global::Card>(FindObjectsSortMode.None))
            {
                if (IsInLocalHand(card) && card.IsZoomed)
                    return true;
            }

            return false;
        }

        public static bool IsInLocalHand(global::Card card)
        {
            return card.Data != null &&
                   !card.IsInRoulette &&
                   card.TryGetOwner(out global::Player owner) &&
                   owner.isOwned;
        }
    }
}
