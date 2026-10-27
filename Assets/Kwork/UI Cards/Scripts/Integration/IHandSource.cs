using System.Collections.Generic;

namespace CardCarouselUI
{
    public interface IHandSource
    {
        IReadOnlyList<HandCardInfo> GetCards();
    }
}
