using UnityEngine;
using UnityEngine.UI;

namespace CardCarouselUI
{
    public static class CardFaceArt
    {
        private const string IllustrationLayer = "Image";
        private const string FrameLayer = "Frame";
        private const float LayerSize = 100f;

        public static void Apply(RectTransform card, Sprite face)
        {
            if (face == null || !card.TryGetComponent(out CardHover hover))
                return;

            Transform frame = FindLayer(card, FrameLayer);

            if (frame == null)
                return;

            Transform illustration = FindLayer(card, IllustrationLayer);

            if (illustration != null)
                illustration.gameObject.SetActive(false);

            RectTransform faceRect = CreateFace(frame, face, card.rect.height);

            Image faceImage = faceRect.GetComponent<Image>();

            hover.UseIllustration(faceRect, faceImage);

            if (card.TryGetComponent(out CardVisual visual))
                visual.AddTintGraphic(faceImage);
        }

        private static RectTransform CreateFace(Transform frame, Sprite face, float height)
        {
            GameObject faceObject = new GameObject("Face", typeof(RectTransform), typeof(Image));

            RectTransform rect = (RectTransform)faceObject.transform;
            rect.SetParent(frame.parent, false);
            rect.SetSiblingIndex(frame.GetSiblingIndex() + 1);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(LayerSize, LayerSize);
            rect.localScale = Vector3.one * (height / LayerSize);

            Image image = faceObject.GetComponent<Image>();
            image.sprite = face;
            image.preserveAspect = true;

            return rect;
        }

        private static Transform FindLayer(Transform root, string layerName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == layerName)
                    return child;
            }

            return null;
        }
    }
}
