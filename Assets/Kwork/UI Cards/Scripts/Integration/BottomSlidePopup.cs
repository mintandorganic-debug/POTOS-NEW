using System.Collections;
using UnityEngine;

namespace CardCarouselUI
{
    public class BottomSlidePopup : MonoBehaviour, IPopup
    {
        [SerializeField] private float duration = 0.2f;

        private CanvasGroup group;
        private RectTransform content;
        private Coroutine animation;

        public bool IsVisible { get; private set; }

        public void Initialize(CanvasGroup group, RectTransform content)
        {
            this.group = group;
            this.content = content;

            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }

        public void Show()
        {
            IsVisible = true;
            Play(0f);
        }

        public void Hide()
        {
            IsVisible = false;
            Play(HiddenY);
        }

        private float HiddenY => -content.rect.height;

        private void Play(float targetY)
        {
            if (animation != null)
                StopCoroutine(animation);

            float startY = group.gameObject.activeSelf
                ? content.anchoredPosition.y
                : HiddenY;

            group.gameObject.SetActive(true);

            animation = StartCoroutine(Animate(startY, targetY));
        }

        private IEnumerator Animate(float startY, float targetY)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                Apply(Mathf.Lerp(startY, targetY, t));

                yield return null;
            }

            Apply(targetY);

            if (!IsVisible)
                group.gameObject.SetActive(false);
        }

        private void Apply(float y)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
            group.alpha = Mathf.InverseLerp(HiddenY, 0f, y);
        }
    }
}
