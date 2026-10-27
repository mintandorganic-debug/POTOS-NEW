using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardCarouselUI
{
    public class CardGazeHandInput : MonoBehaviour, IHandInput
    {
        [SerializeField] private float maxDistance = 3f;
        [SerializeField] private float hideDelay = 0.3f;
        [SerializeField] private float zoneRadiusPadding = 0.15f;
        [SerializeField] private float zoneDropFraction = 0.4f;

        private HandHoverZone zone;
        private RectTransform pointerArea;
        private bool isShown;
        private bool isSuppressed;
        private bool pointerEntered;
        private float pointerLostTime;

        public float MaxDistance => maxDistance;

        public HandHoverZone Zone => zone ??= new HandHoverZone(zoneRadiusPadding, zoneDropFraction);

        public event Action ShowRequested;
        public event Action HideRequested;

        public void SetPointerArea(RectTransform area)
        {
            pointerArea = area;
        }

        private void Update()
        {
            bool isAiming = IsAimingAtZone();

            if (!isAiming)
                isSuppressed = false;

            if (isShown)
            {
                if (WasEscapePressed() || HasPointerLeftArea())
                    Hide();

                return;
            }

            if (isAiming && !isSuppressed)
                Show();
        }

        private void Show()
        {
            isShown = true;
            pointerEntered = false;
            pointerLostTime = 0f;
            ShowRequested?.Invoke();
        }

        private void Hide()
        {
            isShown = false;
            isSuppressed = true;
            HideRequested?.Invoke();
        }

        private bool IsAimingAtZone()
        {
            Camera playerCamera = Camera.main;

            return playerCamera != null && Zone.IsAimedAt(playerCamera, maxDistance);
        }

        private bool HasPointerLeftArea()
        {
            if (pointerArea == null || Mouse.current == null)
                return false;

            bool inside = RectTransformUtility.RectangleContainsScreenPoint(
                pointerArea,
                Mouse.current.position.ReadValue(),
                Camera.main);

            if (inside)
            {
                pointerEntered = true;
                pointerLostTime = 0f;
                return false;
            }

            if (!pointerEntered)
                return false;

            pointerLostTime += Time.unscaledDeltaTime;

            return pointerLostTime >= hideDelay;
        }

        private static bool WasEscapePressed()
        {
            return Keyboard.current != null &&
                   Keyboard.current.escapeKey.wasPressedThisFrame;
        }
    }
}
