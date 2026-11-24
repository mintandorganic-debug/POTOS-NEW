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
        private Func<bool> isOpenProbe;

        public float MaxDistance => maxDistance;

        public HandHoverZone Zone => zone ??= new HandHoverZone(zoneRadiusPadding, zoneDropFraction);

        public event Action ShowRequested;
        public event Action HideRequested;

        public void SetPointerArea(RectTransform area)
        {
            global::GameLog.Write(
                "CardGazeHandInput",
                $"SetPointerArea area={(area != null ? area.name : "NULL")}");

            pointerArea = area;
        }

        public void SetOpenStateProbe(Func<bool> probe)
        {
            isOpenProbe = probe;
        }

        private void Update()
        {
            bool isAiming = IsAimingAtZone();

            if (!isAiming)
                isSuppressed = false;

            if (isShown && isOpenProbe != null && !isOpenProbe())
            {
                // The popup was closed without Hide() (a card was played from the
                // carousel) or never opened. Previously isShown stayed true and the
                // carousel stopped appearing when the gaze returned to the fan.
                global::GameLog.Write(
                    "CardGazeHandInput",
                    "Update popup closed elsewhere, resetting shown state");

                isShown = false;
                isSuppressed = true;
            }

            if (isShown)
            {
                if (WasEscapePressed() || HasPointerLeftArea())
                    Hide();

                return;
            }

            if (isAiming && !isSuppressed && !LocalHandCardFilter.IsAnyCardSelected())
                Show();
        }

        private void Show()
        {
            global::GameLog.Write(
                "CardGazeHandInput",
                $"Show gaze entered hand zone, requesting open hasListeners={ShowRequested != null}");

            isShown = true;
            pointerEntered = false;
            pointerLostTime = 0f;
            ShowRequested?.Invoke();
        }

        private void Hide()
        {
            global::GameLog.Write(
                "CardGazeHandInput",
                $"Hide requesting close (escape or pointer left area) pointerEntered={pointerEntered} pointerLostTime={pointerLostTime} hasListeners={HideRequested != null}");

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
