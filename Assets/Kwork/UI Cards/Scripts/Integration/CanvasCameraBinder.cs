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

            global::GameLog.Write(
                "CanvasCameraBinder",
                $"Awake object={name} canvas={(canvas != null ? canvas.name : "NULL")}");
        }

        private void Update()
        {
            Camera playerCamera = Camera.main;

            if (playerCamera != null && canvas.worldCamera != playerCamera)
                canvas.worldCamera = playerCamera;
        }
    }
}
