using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardCarouselUI
{
    public class LocalPlayerHandSource : IHandSource
    {
        public IReadOnlyList<HandCardInfo> GetCards()
        {
            global::Card[] cards =
                UnityEngine.Object.FindObjectsByType<global::Card>(
                    FindObjectsSortMode.None);

            Array.Sort(cards, CompareByNetId);

            List<HandCardInfo> hand = new List<HandCardInfo>();

            foreach (global::Card card in cards)
            {
                if (LocalHandCardFilter.IsInLocalHand(card))
                    hand.Add(ToInfo(card));
            }

            return hand;
        }

        private static int CompareByNetId(global::Card first, global::Card second)
        {
            return first.netId.CompareTo(second.netId);
        }

        private static HandCardInfo ToInfo(global::Card card)
        {
            global::CardData data = card.Data;

            return new HandCardInfo(
                data.CardName,
                data.Sprite,
                data.Description,
                ToCategory(data.CardClass),
                isHovered => HandCardHighlight.Set(card, isHovered));
        }

        private static HandCardCategory ToCategory(global::Card.CardClass cardClass)
        {
            switch (cardClass)
            {
                case global::Card.CardClass.AttackLight:
                case global::Card.CardClass.AttackSteal:
                    return HandCardCategory.Attack;

                case global::Card.CardClass.Defence:
                    return HandCardCategory.Defence;

                case global::Card.CardClass.Sin:
                    return HandCardCategory.Sin;

                default:
                    return HandCardCategory.Util;
            }
        }
    }
}
