using Mirror;
using UnityEngine;

public class WSCanvasHandler : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    private void Awake()
    {
        GameLog.Write(
            "WSCanvasHandler",
            $"Awake object={name} canvas={(canvas != null ? canvas.name : "NULL")} mainCamera={(Camera.main != null ? Camera.main.name : "NULL")}");

        canvas.worldCamera = Camera.main;
    }
}
