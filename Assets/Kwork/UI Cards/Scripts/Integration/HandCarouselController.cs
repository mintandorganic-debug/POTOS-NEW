using System.Collections.Generic;
using UnityEngine;

namespace CardCarouselUI
{
    public class HandCarouselController
    {
        private readonly IHandSource source;
        private readonly IHandCardFactory factory;
        private readonly CardCarouselController carousel;
        private readonly RectTransform cardParent;
        private readonly IPopup popup;

        public HandCarouselController(
            IHandSource source,
            IHandCardFactory factory,
            CardCarouselController carousel,
            RectTransform cardParent,
            IPopup popup)
        {
            this.source = source;
            this.factory = factory;
            this.carousel = carousel;
            this.cardParent = cardParent;
            this.popup = popup;
        }

        public void Open()
        {
            IReadOnlyList<HandCardInfo> hand = source.GetCards();

            if (hand.Count == 0)
                return;

            popup.Show();

            carousel.SetCards(CreateCards(hand));
        }

        public void Close()
        {
            if (!popup.IsVisible)
                return;

            popup.Hide();
        }

        private List<RectTransform> CreateCards(IReadOnlyList<HandCardInfo> hand)
        {
            List<RectTransform> cards = new List<RectTransform>(hand.Count);

            foreach (HandCardInfo info in hand)
                cards.Add(factory.Create(info, cardParent, carousel));

            return cards;
        }
    }
}
