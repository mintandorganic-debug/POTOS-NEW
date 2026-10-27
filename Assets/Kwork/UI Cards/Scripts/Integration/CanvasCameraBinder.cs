using UnityEngine;

namespace CardCarouselUI
{
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraBinder : MonoBehaviour
    {
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            Camera playerCamera = Camera.main;

            if (playerCamera != null && canvas.worldCamera != playerCamera)
                canvas.worldCamera = playerCamera;
        }
    }
}
