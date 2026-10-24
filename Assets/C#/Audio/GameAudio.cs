using UnityEngine;

/// <summary>
/// Scene-level audio hub (GameplayScene). Not networked: every client has
/// its own copy and is told what to play by PlayerSounds RPCs.
///
/// Holds the "audience" AudioSource: place the AudienceSoundPoint object
/// in the hall below the table - AFTERMATH sounds are played from there.
/// </summary>
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [Header("Audience (hall below the table)")]
    [SerializeField] private AudioSource audienceSource;
    [SerializeField, Range(0f, 1f)] private float audienceVolume = 1f;

    [Header("Debug")]
    [Tooltip("Logs every audience sound (source state, distance).")]
    [SerializeField] private bool debugLog = true;

    private bool warnedMissingSource;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LogAudienceState(
        AudioClip clip,
        float volume)
    {
        if (audienceSource == null)
        {
            Debug.Log(
                $"[GameAudio] '{clip.name}' volume={volume:0.00}, " +
                "audienceSource is NOT assigned (fallback).",
                this);

            return;
        }

        AudioListener listener =
            FindFirstObjectByType<AudioListener>();

        float distance =
            listener != null
                ? Vector3.Distance(
                    listener.transform.position,
                    audienceSource.transform.position)
                : -1f;

        string message =
            $"[GameAudio] '{clip.name}' volume={volume:0.00} " +
            $"(audience {audienceVolume:0.00}), " +
            $"source enabled={audienceSource.isActiveAndEnabled}, " +
            $"mute={audienceSource.mute}, " +
            $"source volume={audienceSource.volume:0.00}, " +
            $"spatialBlend={audienceSource.spatialBlend:0.0}, " +
            $"distance to listener={distance:0.0} " +
            $"(max {audienceSource.maxDistance:0.0})";

        if (volume <= 0.001f ||
            !audienceSource.isActiveAndEnabled ||
            audienceSource.mute ||
            listener == null ||
            (audienceSource.spatialBlend > 0f &&
             distance > audienceSource.maxDistance))
        {
            Debug.LogWarning(
                message + "  <-- probably INAUDIBLE",
                this);
        }
        else
        {
            Debug.Log(message, this);
        }
    }

    public void PlayAtAudience(
        AudioClip clip,
        float volumeScale = 1f)
    {
        if (clip == null)
            return;

        float volume =
            audienceVolume * volumeScale;

        if (debugLog)
            LogAudienceState(clip, volume);

        if (audienceSource != null)
        {
            // PlayOneShot so several thrown items can overlap.
            audienceSource.PlayOneShot(
                clip,
                volume);

            return;
        }

        if (!warnedMissingSource)
        {
            warnedMissingSource = true;

            Debug.LogWarning(
                "GameAudio: audienceSource is not assigned. " +
                "Falling back to the AudioListener position.",
                this);
        }

        AudioListener listener =
            FindFirstObjectByType<AudioListener>();

        if (listener != null)
        {
            AudioSource.PlayClipAtPoint(
                clip,
                listener.transform.position,
                volume);
        }
    }
}
