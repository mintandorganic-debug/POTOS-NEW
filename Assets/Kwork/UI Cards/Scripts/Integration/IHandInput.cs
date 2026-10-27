using System;

namespace CardCarouselUI
{
    public interface IHandInput
    {
        event Action ShowRequested;

        event Action HideRequested;
    }
}
