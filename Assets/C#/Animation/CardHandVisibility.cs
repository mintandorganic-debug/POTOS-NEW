#if UNITY_EDITOR
using System.Collections.Generic;
using Mirror;
using UnityEngine;

// Hides the cards that the local player holds in the hand and shows them again.
public class CardHandVisibility
{
    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();

    public void Hide()
    {
        Show();

        if (NetworkClient.localPlayer == null)
        {
            return;
        }

        Player player =
            NetworkClient.localPlayer.GetComponent<Player>();

        if (player == null ||
            player.cardSockets == null)
        {
            return;
        }

        foreach (GameObject socket in player.cardSockets)
        {
            if (socket == null)
            {
                continue;
            }

            foreach (Renderer renderer in
                     socket.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled)
                {
                    renderer.enabled = false;
                    hiddenRenderers.Add(renderer);
                }
            }

            foreach (Canvas canvas in
                     socket.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.enabled)
                {
                    canvas.enabled = false;
                    hiddenCanvases.Add(canvas);
                }
            }
        }

        GameLog.Write(
            "CardHandVisibility",
            $"Hide: renderers={hiddenRenderers.Count} canvases={hiddenCanvases.Count}");
    }

    public void Show()
    {
        foreach (Renderer renderer in hiddenRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        foreach (Canvas canvas in hiddenCanvases)
        {
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        hiddenRenderers.Clear();
        hiddenCanvases.Clear();
    }
}
#endif
