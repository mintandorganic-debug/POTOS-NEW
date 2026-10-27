using System;
using UnityEngine;

namespace CardCarouselUI
{
    public enum HandCardCategory
    {
        Attack,
        Defence,
        Sin,
        Util
    }

    public readonly struct HandCardInfo
    {
        public HandCardInfo(
            string id,
            Sprite art,
            string description,
            HandCardCategory category,
            Action<bool> hoverChanged)
        {
            Id = id;
            Art = art;
            Description = description;
            Category = category;
            HoverChanged = hoverChanged;
        }

        public string Id { get; }
        public Sprite Art { get; }
        public string Description { get; }
        public HandCardCategory Category { get; }
        public Action<bool> HoverChanged { get; }
    }
}
