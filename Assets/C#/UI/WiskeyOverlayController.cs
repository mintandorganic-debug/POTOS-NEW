using UnityEngine;

public class WhiskeyOverlayController : MonoBehaviour
{
    public static WhiskeyOverlayController Instance { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Material overlayInstance;

    private void Awake()
    {
        Instance = this;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            overlayInstance = new Material(spriteRenderer.sharedMaterial);
            spriteRenderer.sharedMaterial = overlayInstance;

            GameLog.Write("WhiskeyOverlayController", $"Awake instance created material={overlayInstance.name}");
        }
        else
        {
            GameLog.Write("WhiskeyOverlayController", "Awake spriteRenderer is NULL");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (overlayInstance != null)
        {
            Destroy(overlayInstance);
            overlayInstance = null;
        }
    }

    public void Play()
    {
        if (overlayInstance == null)
        {
            GameLog.Write("WhiskeyOverlayController", "Play skipped: overlayInstance is NULL");
            return;
        }

        overlayInstance.SetFloat("_FadeStart", Time.time);

        GameLog.Write("WhiskeyOverlayController", $"Play _FadeStart={Time.time}");
    }
}