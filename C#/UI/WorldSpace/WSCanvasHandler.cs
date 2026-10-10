using Mirror;
using UnityEngine;

public class WSCanvasHandler : MonoBehaviour 
{
    [SerializeField] Canvas canvas;
    private void Awake()
    {
        canvas.worldCamera = Camera.main;
    }
}
