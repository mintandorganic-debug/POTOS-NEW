using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardCarouselUI
{
    public class HandCarouselInstaller : MonoBehaviour
    {
        [Serializable]
        private class CategoryTemplate
        {
            public HandCardCategory category;
            public RectTransform template;
        }

        [Header("Prefabs")]
        [SerializeField] private GameObject carouselPrefab;
        [SerializeField] private RectTransform defaultCardTemplate;
        [SerializeField] private List<CategoryTemplate> categoryTemplates = new();

        [Header("Canvas")]
        [SerializeField] private int sortingOrder = 50;
        [SerializeField] private float planeDistance = 0.1f;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Header("Look")]
        [SerializeField, Range(0f, 90f)] private float tiltBackDegrees = 18f;
        [SerializeField, Range(0.1f, 2f)] private float carouselScale = 0.55f;
        [SerializeField] private float verticalOffset = -261f;

        private HandCarouselController controller;
        private IHandInput input;

        private void Awake()
        {
            Canvas canvas = CreateCanvas();
            CanvasGroup group = CreateContentGroup(canvas.transform);

            GameObject carouselInstance = Instantiate(carouselPrefab, group.transform);
            FitToScreenCanvas((RectTransform)carouselInstance.transform, carouselScale, verticalOffset);

            CardCarouselController carousel =
                carouselInstance.GetComponentInChildren<CardCarouselController>(true);

            RectTransform content = (RectTransform)group.transform;
            content.localRotation = Quaternion.Euler(tiltBackDegrees, 0f, 0f);

            BottomSlidePopup popup = canvas.gameObject.AddComponent<BottomSlidePopup>();
            popup.Initialize(group, content);

            CardGazeHandInput gaze = canvas.gameObject.AddComponent<CardGazeHandInput>();
            gaze.SetPointerArea((RectTransform)carouselInstance.transform);
            input = gaze;

            controller = new HandCarouselController(
                new LocalPlayerHandSource(),
                new TemplateHandCardFactory(BuildTemplateMap(), defaultCardTemplate),
                carousel,
                (RectTransform)carousel.transform,
                new MouseModePopup(popup, new HandCameraSnap(gaze.Zone)));

            canvas.gameObject.AddComponent<HandHoverZoneGizmos>();
        }

        private void OnEnable()
        {
            input.ShowRequested += controller.Open;
            input.HideRequested += controller.Close;
        }

        private void OnDisable()
        {
            input.ShowRequested -= controller.Open;
            input.HideRequested -= controller.Close;
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "Hand Carousel Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.planeDistance = planeDistance;
            canvas.sortingOrder = sortingOrder;

            canvasObject.AddComponent<CanvasCameraBinder>();

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void FitToScreenCanvas(RectTransform root, float scale, float offsetY)
        {
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * scale;
            root.anchoredPosition3D = new Vector3(0f, offsetY, 0f);
        }

        private static CanvasGroup CreateContentGroup(Transform canvasTransform)
        {
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(CanvasGroup));
            content.transform.SetParent(canvasTransform, false);

            RectTransform rect = (RectTransform)content.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return content.GetComponent<CanvasGroup>();
        }

        private Dictionary<HandCardCategory, RectTransform> BuildTemplateMap()
        {
            Dictionary<HandCardCategory, RectTransform> map =
                new Dictionary<HandCardCategory, RectTransform>();

            foreach (CategoryTemplate entry in categoryTemplates)
                map[entry.category] = entry.template;

            return map;
        }
    }
}
