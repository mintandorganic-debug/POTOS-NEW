namespace CardCarouselUI
{
    public interface IPopup
    {
        bool IsVisible { get; }

        void Show();

        void Hide();
    }
}
