using Mirror;
using UnityEngine;

namespace CardCarouselUI
{
    public class MouseModePopup : IPopup
    {
        private readonly IPopup inner;
        private readonly HandCameraSnap cameraSnap;

        private MonoBehaviour frozen;
        private CursorLockMode previousLockMode;
        private bool previousVisible;

        public MouseModePopup(IPopup inner, HandCameraSnap cameraSnap)
        {
            global::GameLog.Write(
                "MouseModePopup",
                $"Constructor hasInner={inner != null} hasCameraSnap={cameraSnap != null}");

            this.inner = inner;
            this.cameraSnap = cameraSnap;
        }

        public bool IsVisible => inner.IsVisible;

        public void Show()
        {
            previousLockMode = Cursor.lockState;
            previousVisible = Cursor.visible;

            global::GameLog.Write(
                "MouseModePopup",
                $"Show mouse mode on, previousLockMode={previousLockMode} previousVisible={previousVisible}");

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            FreezeInteraction();
            cameraSnap.Engage();

            inner.Show();
        }

        public void Hide()
        {
            global::GameLog.Write(
                "MouseModePopup",
                $"Hide mouse mode off, restoring lockMode={previousLockMode} visible={previousVisible}");

            cameraSnap.Release();
            UnfreezeInteraction();

            Cursor.lockState = previousLockMode;
            Cursor.visible = previousVisible;

            inner.Hide();
        }

        private void FreezeInteraction()
        {
            NetworkIdentity player = NetworkClient.localPlayer;

            frozen = player != null ? player.GetComponentInChildren<Interactor>() : null;

            global::GameLog.Write(
                "MouseModePopup",
                $"FreezeInteraction localPlayer={(player != null ? player.name : "NULL")} interactor={(frozen != null ? frozen.name : "NULL")} hudExists={(global::HudController.Instance != null)}");

            if (frozen != null)
            {
                frozen.StopAllCoroutines();
                frozen.enabled = false;
            }

            if (global::HudController.Instance != null)
                global::HudController.Instance.HideCardInfo();
        }

        private void UnfreezeInteraction()
        {
            global::GameLog.Write(
                "MouseModePopup",
                $"UnfreezeInteraction interactor={(frozen != null ? frozen.name : "NULL")}");

            if (frozen != null)
                frozen.enabled = true;

            frozen = null;
        }
    }
}
