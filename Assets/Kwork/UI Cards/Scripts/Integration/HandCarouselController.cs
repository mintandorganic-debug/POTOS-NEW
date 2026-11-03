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
            global::GameLog.Write(
                "HandCarouselController",
                $"Constructor hasSource={source != null} hasFactory={factory != null} carousel={(carousel != null ? carousel.name : "NULL")} cardParent={(cardParent != null ? cardParent.name : "NULL")} hasPopup={popup != null}");

            this.source = source;
            this.factory = factory;
            this.carousel = carousel;
            this.cardParent = cardParent;
            this.popup = popup;
        }

        public void Open()
        {
            global::GameLog.Write(
                "HandCarouselController",
                $"Open requested popupVisible={(popup != null ? popup.IsVisible.ToString() : "NULL")}");

            IReadOnlyList<HandCardInfo> hand = source.GetCards();

            if (hand.Count == 0)
            {
                global::GameLog.Write(
                    "HandCarouselController",
                    "Open skipped: local hand is empty");

                return;
            }

            global::GameLog.Write(
                "HandCarouselController",
                $"Open showing popup and building carousel cards={hand.Count}");

            popup.Show();

            carousel.SetCards(CreateCards(hand));
        }

        public void Close()
        {
            global::GameLog.Write(
                "HandCarouselController",
                $"Close requested popupVisible={(popup != null ? popup.IsVisible.ToString() : "NULL")}");

            if (!popup.IsVisible)
            {
                global::GameLog.Write(
                    "HandCarouselController",
                    "Close skipped: popup is not visible");

                return;
            }

            popup.Hide();

            global::GameLog.Write(
                "HandCarouselController",
                "Close popup hidden");
        }

        private List<RectTransform> CreateCards(IReadOnlyList<HandCardInfo> hand)
        {
            global::GameLog.Write(
                "HandCarouselController",
                $"CreateCards count={hand.Count}");

            List<RectTransform> cards = new List<RectTransform>(hand.Count);

            foreach (HandCardInfo info in hand)
                cards.Add(factory.Create(info, cardParent, carousel, Close));

            return cards;
        }
    }
}
