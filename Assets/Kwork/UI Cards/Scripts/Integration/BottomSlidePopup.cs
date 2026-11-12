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
            global::GameLog.Write(
                "BottomSlidePopup",
                $"Initialize group={(group != null ? group.name : "NULL")} content={(content != null ? content.name : "NULL")}");

            this.group = group;
            this.content = content;

            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.gameObject.SetActive(false);
        }

        public void Show()
        {
            global::GameLog.Write(
                "BottomSlidePopup",
                $"Show wasVisible={IsVisible}");

            IsVisible = true;
            Play(0f);
        }

        public void Hide()
        {
            global::GameLog.Write(
                "BottomSlidePopup",
                $"Hide wasVisible={IsVisible}");

            IsVisible = false;
            if (group != null) group.blocksRaycasts = false;
            Play(HiddenY);
        }

        private float HiddenY => -content.rect.height;

        private void Play(float targetY)
        {
            global::GameLog.Write(
                "BottomSlidePopup",
                $"Play targetY={targetY} restartingRunningAnimation={animation != null} groupActive={(group != null ? group.gameObject.activeSelf.ToString() : "NULL")}");

            if (animation != null)
                StopCoroutine(animation);

            if (group != null)
                group.blocksRaycasts = false;

            float startY = group.gameObject.activeSelf
                ? content.anchoredPosition.y
                : HiddenY;

            group.gameObject.SetActive(true);

            animation = StartCoroutine(Animate(startY, targetY));
        }

        private IEnumerator Animate(float startY, float targetY)
        {
            global::GameLog.Write(
                "BottomSlidePopup",
                $"Animate started startY={startY} targetY={targetY} duration={duration}");

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                Apply(Mathf.Lerp(startY, targetY, t));

                yield return null;
            }

            Apply(targetY);

            global::GameLog.Write(
                "BottomSlidePopup",
                $"Animate finished targetY={targetY} visible={IsVisible}");

            if (!IsVisible)
            {
                if (group != null) group.blocksRaycasts = false;

                global::GameLog.Write(
                    "BottomSlidePopup",
                    "Animate popup hidden, content group deactivated");

                group.gameObject.SetActive(false);
            }
            else
            {
                if (group != null) group.blocksRaycasts = true;
            }
        }

        private void Apply(float y)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
            group.alpha = Mathf.InverseLerp(HiddenY, 0f, y);
        }
    }
}
