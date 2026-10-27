using UnityEngine;

namespace CardCarouselUI
{
    public interface IHandCardFactory
    {
        RectTransform Create(
            HandCardInfo info,
            RectTransform parent,
            CardCarouselController carousel);
    }
}
