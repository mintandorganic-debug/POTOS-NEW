using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameAudioManager : MonoBehaviour
{
    public enum ReactionClipType
    {
        Burp = 0,
        Caugh = 1,
        CreakingChair = 2,
        Exhale = 3,
        Joyful = 4,
        Whistle = 5,
        Wonderment = 6,
        Scoff = 7,
        Disappointment = 8
    }

    public static GameAudioManager Instance { get; private set; }

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 0.45f;
    [Range(0f, 1f)] public float tensionVolume = 0.5f;
    [Range(0f, 1f)] public float heartbeatVolume = 0.65f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    [Range(0f, 1f)] public float uiVolume = 0.5f;
    [Range(0f, 1f)] public float platformVolume = 0.85f;
    [Range(0f, 1f)] public float voiceVolume = 0.85f;
    [Range(0f, 1f)] public float idleVolumeMultiplier = 0.45f;
    [Range(1f, 6f)] public float aftermathVolumeMultiplier = 3.7f;


    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource pressureSource;
    [SerializeField] private AudioSource riskSource;
    [SerializeField] private AudioSource dangerSource;
    [SerializeField] private AudioSource heartbeatSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource platformSource;
    [SerializeField] private AudioSource voiceSource;

    private Coroutine ambientSyncRoutine;
    private Coroutine pressureFadeRoutine;
    private Coroutine riskFadeRoutine;
    private Coroutine dangerFadeRoutine;
    private Coroutine heartbeatFadeRoutine;

    [Header("Ambient (5)")]
    public AudioClip potosAmbient;
    public AudioClip potosDanger;
    public AudioClip potosHeartbeat;
    public AudioClip potosPressure;
    public AudioClip potosRisk;

    [Header("Cards (2)")]
    public AudioClip handsPick;
    public AudioClip handsRoll;

    [Header("Coin (1)")]
    public AudioClip coinToss;

    [Header("Game Sounds (5)")]
    public AudioClip accessoryDisappearSound;
    public AudioClip cardBurnSound;
    public AudioClip lostSound;
    public AudioClip nextStageSound;
    public AudioClip winSound;

    [Header("Misc (11)")]
    public AudioClip tenLeft;
    public AudioClip accessoryAdd;
    public AudioClip attackSuccessful;
    public AudioClip attackUnsuccessful;
    public AudioClip cardsGivingHole1;
    public AudioClip cardsGivingHole2;
    public AudioClip cardsGivingHole3;
    public AudioClip cardsShuffle;
    public AudioClip cardDestroy;
    public AudioClip nextSound;
    public AudioClip pointsAdd;

    [Header("Platform (5)")]
    public AudioClip platformBase;
    public AudioClip platformPlateDown;
    public AudioClip platformPlateUp;
    public AudioClip platformSmDown;
    public AudioClip platformSmUp;

    [Header("Slot Machine (7)")]
    public AudioClip slotMachineCardGive;
    public AudioClip slotMachinePick1;
    public AudioClip slotMachinePick2;
    public AudioClip slotMachinePick3;
    public AudioClip slotMachineSpin;
    public AudioClip slotMachineStop;
    public AudioClip slotMachineWindup;

    [Header("UI (11)")]
    public AudioClip uiAccept;
    public AudioClip uiCancel;
    public AudioClip uiHover1;
    public AudioClip uiHover2;
    public AudioClip uiHover3;
    public AudioClip uiHover4;
    public AudioClip uiHover5;
    public AudioClip uiMenuClose;
    public AudioClip uiMenuOpen;
    public AudioClip uiMenuPress;
    public AudioClip uiUnavailable;

    [Header("Character Reactions & Voice (9)")]
    public AudioClip burpSound;
    public AudioClip caughSound;
    public AudioClip creakingChairSound;
    public AudioClip disappointmentSound;
    public AudioClip exhaleSound;
    public AudioClip joyfulSound;
    public AudioClip scoffSound;
    public AudioClip whistleSound;
    public AudioClip wondermentSound;

    [Header("Weapon - Pistol / Revolver (4)")]
    public AudioClip pistolReady;
    public AudioClip pistolAction;
    public AudioClip pistolCancel;
    public AudioClip aftermathRevolverSound;

    [Header("Weapon - Knife (6)")]
    public AudioClip knifeReady;
    public AudioClip knifeAction;
    public AudioClip knifeCancel;
    public AudioClip knifeHitsHand;
    public AudioClip atkKnifeVicHitSound;
    public AudioClip aftermathKnifeSound;

    [Header("Weapon - Shears (5)")]
    public AudioClip shearsReady;
    public AudioClip shearsAction;
    public AudioClip shearsCancel;
    public AudioClip atkShearsVicHitSound;
    public AudioClip aftermathShearsSound;

    [Header("Weapon - Bucket (5)")]
    public AudioClip bucketReady;
    public AudioClip bucketAction;
    public AudioClip bucketCancel;
    public AudioClip atkBucketVicHitSound;
    public AudioClip aftermathBucketSound;

    [Header("Weapon - Fork (5)")]
    public AudioClip forkReady;
    public AudioClip forkAction;
    public AudioClip forkCancel;
    public AudioClip atkForkVicHitSound;
    public AudioClip aftermathForkSound;

    [Header("Weapon - Grenade (5)")]
    public AudioClip grenadeReady;
    public AudioClip grenadeAction;
    public AudioClip grenadeCancel;
    public AudioClip grenadeBoom;
    public AudioClip aftermathGrenadeSound;

    [Header("Weapon - Shotgun (4)")]
    public AudioClip shotgunReady;
    public AudioClip shotgunAction;
    public AudioClip shotgunCancel;
    public AudioClip aftermathShotgunSound;

    [Header("Weapon - Whiskey (5)")]
    public AudioClip whiskeyReady;
    public AudioClip whiskeyAction;
    public AudioClip whiskeyCancel;
    public AudioClip atkWhiskyVicHitSound;
    public AudioClip aftermathWhiskeySound;

    [Header("Weapon - Flame (3)")]
    public AudioClip atkFlamePrepSound;
    public AudioClip atkFlameActionSound;
    public AudioClip atkFlameEndSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureAudioSources();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StopAllMusic();
        BindSceneUiButtons();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this != Instance)
            return;

        if ((scene.name != "GameplayScene" && scene.name != "TestScene") ||
            GameMaster.Instance == null ||
            GameMaster.Instance.CurrentPhase == GameMaster.GamePhase.Wait)
        {
            StopAllMusic();
        }

        BindSceneUiButtons();
    }

    public void BindSceneUiButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include);

        foreach (Button btn in buttons)
        {
            if (btn == null)
                continue;

            if (btn.GetComponent<UiButtonAudioHook>() == null)
            {
                btn.gameObject.AddComponent<UiButtonAudioHook>();
            }
        }
    }

    private sealed class UiButtonAudioHook : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Instance == null)
                return;

            Instance.PlayHover();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Instance == null)
                return;

            if (button == null)
                button = GetComponent<Button>();

            if (button != null && !button.interactable)
            {
                Instance.PlayUnavailable();
                return;
            }

            Instance.PlayMenuPress();
        }
    }

    private void OnValidate()
    {
        ApplyVolumes();
    }

    public void ApplyVolumes()
    {
        float targetVol = ambientVolume * masterVolume;
        if (ambientSource != null)
            ambientSource.volume = targetVol;
        if (pressureSource != null && pressureFadeRoutine == null && pressureSource.volume > 0.01f)
            pressureSource.volume = targetVol;
        if (riskSource != null && riskFadeRoutine == null && riskSource.volume > 0.01f)
            riskSource.volume = targetVol;
        if (dangerSource != null && dangerFadeRoutine == null && dangerSource.volume > 0.01f)
            dangerSource.volume = targetVol;
        if (heartbeatSource != null && heartbeatFadeRoutine == null && heartbeatSource.volume > 0.01f)
            heartbeatSource.volume = targetVol;

        if (sfxSource != null)
            sfxSource.volume = sfxVolume * masterVolume;
        if (uiSource != null)
            uiSource.volume = uiVolume * masterVolume;
        if (platformSource != null)
            platformSource.volume = platformVolume * masterVolume;
        if (voiceSource != null)
            voiceSource.volume = voiceVolume * masterVolume;
    }

    private void EnsureAudioSources()
    {
        if (ambientSource == null)
        {
            ambientSource = CreateSource("AmbientSource", true, ambientVolume * masterVolume);
            ambientSource.clip = potosAmbient;
        }
        if (pressureSource == null)
        {
            pressureSource = CreateSource("PressureSource", true, 0f);
            pressureSource.clip = potosPressure;
        }
        if (riskSource == null)
        {
            riskSource = CreateSource("RiskSource", true, 0f);
            riskSource.clip = potosRisk;
        }
        if (dangerSource == null)
        {
            dangerSource = CreateSource("DangerSource", true, 0f);
            dangerSource.clip = potosDanger;
        }
        if (heartbeatSource == null)
        {
            heartbeatSource = CreateSource("HeartbeatSource", true, 0f);
            heartbeatSource.clip = potosHeartbeat;
        }
        if (sfxSource == null)
        {
            sfxSource = CreateSource("SfxSource", false, sfxVolume * masterVolume);
        }
        if (uiSource == null)
        {
            uiSource = CreateSource("UiSource", false, uiVolume * masterVolume);
        }
        if (platformSource == null)
        {
            platformSource = CreateSource("PlatformSource", false, platformVolume * masterVolume);
        }
        if (voiceSource == null)
        {
            voiceSource = CreateSource("VoiceSource", false, voiceVolume * masterVolume);
        }
        ApplyVolumes();
    }

    private AudioSource CreateSource(string sourceName, bool loop, float volume)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        AudioSource src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.volume = volume;
        src.spatialBlend = 0f;
        src.panStereo = 0f;
        return src;
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        EnsureAudioSources();
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlayAftermath(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSources();

        float totalGain = Mathf.Max(0f, aftermathVolumeMultiplier);
        int fullShots = Mathf.FloorToInt(totalGain);
        float remainder = totalGain - fullShots;

        for (int i = 0; i < fullShots; i++)
        {
            sfxSource.PlayOneShot(clip, 1f);
        }

        if (remainder > 0.01f)
        {
            sfxSource.PlayOneShot(clip, remainder);
        }
    }

    public void PlayUi(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        EnsureAudioSources();
        uiSource.PlayOneShot(clip, volumeScale);
    }

    public void PlayPlatform(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        EnsureAudioSources();
        platformSource.PlayOneShot(clip, volumeScale);
    }

    public void PlayVoice(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        EnsureAudioSources();
        voiceSource.PlayOneShot(clip, volumeScale);
    }

    // --- Ambient & Continuous Game State Music ---

    public void StartContinuousAmbient()
    {
        EnsureAudioSources();

        if (ambientSource.clip != potosAmbient)
            ambientSource.clip = potosAmbient;
        if (pressureSource.clip != potosPressure)
            pressureSource.clip = potosPressure;
        if (riskSource.clip != potosRisk)
            riskSource.clip = potosRisk;
        if (dangerSource.clip != potosDanger)
            dangerSource.clip = potosDanger;
        if (heartbeatSource.clip != potosHeartbeat)
            heartbeatSource.clip = potosHeartbeat;

        ambientSource.loop = true;
        pressureSource.loop = true;
        riskSource.loop = true;
        dangerSource.loop = true;
        heartbeatSource.loop = true;

        ambientSource.volume = ambientVolume * masterVolume;

        if (!ambientSource.isPlaying) ambientSource.Play();
        if (!pressureSource.isPlaying) pressureSource.Play();
        if (!riskSource.isPlaying) riskSource.Play();
        if (!dangerSource.isPlaying) dangerSource.Play();
        if (!heartbeatSource.isPlaying) heartbeatSource.Play();

        // Синхронизируем воспроизведение всех слоев
        int ambientSamples = ambientSource.timeSamples;
        pressureSource.timeSamples = ambientSamples;
        riskSource.timeSamples = ambientSamples;
        dangerSource.timeSamples = ambientSamples;
        heartbeatSource.timeSamples = ambientSamples;

        if (ambientSyncRoutine == null)
            ambientSyncRoutine = StartCoroutine(PeriodicAmbientSyncRoutine());
    }

    private IEnumerator PeriodicAmbientSyncRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (ambientSource != null && ambientSource.isPlaying)
            {
                int samples = ambientSource.timeSamples;
                if (pressureSource != null && Mathf.Abs(pressureSource.timeSamples - samples) > 1000)
                    pressureSource.timeSamples = samples;
                if (riskSource != null && Mathf.Abs(riskSource.timeSamples - samples) > 1000)
                    riskSource.timeSamples = samples;
                if (dangerSource != null && Mathf.Abs(dangerSource.timeSamples - samples) > 1000)
                    dangerSource.timeSamples = samples;
                if (heartbeatSource != null && Mathf.Abs(heartbeatSource.timeSamples - samples) > 1000)
                    heartbeatSource.timeSamples = samples;
            }
        }
    }

    public void StopAllMusic()
    {
        if (ambientSyncRoutine != null)
        {
            StopCoroutine(ambientSyncRoutine);
            ambientSyncRoutine = null;
        }

        if (ambientSource != null) ambientSource.Stop();
        if (pressureSource != null) { pressureSource.Stop(); pressureSource.volume = 0f; }
        if (riskSource != null) { riskSource.Stop(); riskSource.volume = 0f; }
        if (dangerSource != null) { dangerSource.Stop(); dangerSource.volume = 0f; }
        if (heartbeatSource != null) { heartbeatSource.Stop(); heartbeatSource.volume = 0f; }
    }

    public enum MusicLayerState
    {
        AmbientOnly,
        AmbientPressure,
        AmbientRisk,
        AmbientDanger,
        AmbientDangerHeartbeat
    }

    public MusicLayerState CurrentMusicState { get; private set; } = MusicLayerState.AmbientOnly;

    public void SetMusicState(MusicLayerState state, float fadeDuration = 1.2f)
    {
        EnsureAudioSources();
        StartContinuousAmbient();
        CurrentMusicState = state;

        float targetVol = ambientVolume * masterVolume;

        float pressureTarget = (state == MusicLayerState.AmbientPressure) ? targetVol : 0f;
        float riskTarget = (state == MusicLayerState.AmbientRisk) ? targetVol : 0f;
        float dangerTarget = (state == MusicLayerState.AmbientDanger || state == MusicLayerState.AmbientDangerHeartbeat) ? targetVol : 0f;
        float heartbeatTarget = (state == MusicLayerState.AmbientDangerHeartbeat) ? targetVol : 0f;

        FadeTrack(ref pressureFadeRoutine, pressureSource, pressureTarget, fadeDuration);
        FadeTrack(ref riskFadeRoutine, riskSource, riskTarget, fadeDuration);
        FadeTrack(ref dangerFadeRoutine, dangerSource, dangerTarget, fadeDuration);
        FadeTrack(ref heartbeatFadeRoutine, heartbeatSource, heartbeatTarget, fadeDuration > 0.9f ? 0.9f : fadeDuration);
    }

    public void SetGameStateAudio(GameMaster.GamePhase state, bool isLocalTargeted = false, bool isTurnPressure = false)
    {
        switch (state)
        {
            case GameMaster.GamePhase.Wait:
                SetMusicState(MusicLayerState.AmbientOnly);
                break;

            case GameMaster.GamePhase.Attack:
                // Only ambient at start of game/turn; pressure kicks in when low HP or thinking too long
                SetMusicState(isTurnPressure ? MusicLayerState.AmbientPressure : MusicLayerState.AmbientOnly);
                break;

            case GameMaster.GamePhase.SelectTarget:
                // ambient + risk strictly
                SetMusicState(MusicLayerState.AmbientRisk);
                break;

            case GameMaster.GamePhase.Defense:
            case GameMaster.GamePhase.Steal:
                // ambient + danger for everyone; victim also gets heartbeat
                SetMusicState(isLocalTargeted ? MusicLayerState.AmbientDangerHeartbeat : MusicLayerState.AmbientDanger);
                break;
        }
    }

    private void FadeTrack(ref Coroutine routine, AudioSource source, float targetVolume, float duration)
    {
        if (source == null) return;
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FadeSourceVolumeRoutine(source, targetVolume, duration));
    }

    private IEnumerator FadeSourceVolumeRoutine(AudioSource source, float targetVol, float duration)
    {
        if (source == null) yield break;
        if (!source.isPlaying)
            source.Play();

        float startV = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startV, targetVol, elapsed / duration);
            yield return null;
        }

        source.volume = targetVol;
    }

    public void PlayAmbient(AudioClip clip)
    {
        StartContinuousAmbient();
    }

    public void StopAmbient()
    {
        if (ambientSource != null)
            ambientSource.Stop();
    }

    public void PlayHeartbeat(float fadeDuration = 0.9f)
    {
        EnsureAudioSources();
        StartContinuousAmbient();
        FadeTrack(ref heartbeatFadeRoutine, heartbeatSource, heartbeatVolume * masterVolume, fadeDuration);
    }

    public void StopHeartbeat(bool instant = false)
    {
        if (heartbeatSource == null) return;
        if (instant)
        {
            if (heartbeatFadeRoutine != null) StopCoroutine(heartbeatFadeRoutine);
            heartbeatSource.volume = 0f;
        }
        else
        {
            FadeTrack(ref heartbeatFadeRoutine, heartbeatSource, 0f, 0.9f);
        }
    }

    // --- Music Layers & Mute Controls (Test Panel & Settings) ---

    public AudioSource AmbientSource => ambientSource;
    public AudioSource PressureSource => pressureSource;
    public AudioSource RiskSource => riskSource;
    public AudioSource DangerSource => dangerSource;
    public AudioSource HeartbeatSource => heartbeatSource;

    public bool IsMusicMuted { get; private set; }

    public void ToggleMusicMute()
    {
        SetMusicMuted(!IsMusicMuted);
    }

    public void SetMusicMuted(bool muted)
    {
        IsMusicMuted = muted;
        if (ambientSource != null) ambientSource.mute = muted;
        if (pressureSource != null) pressureSource.mute = muted;
        if (riskSource != null) riskSource.mute = muted;
        if (dangerSource != null) dangerSource.mute = muted;
        if (heartbeatSource != null) heartbeatSource.mute = muted;
    }

    public void SetLayerVolumeDirect(string layerName, float volume)
    {
        EnsureAudioSources();
        if (!ambientSource.isPlaying)
            StartContinuousAmbient();

        float targetVol = ambientVolume * masterVolume;
        float scaledVol = Mathf.Clamp01(volume) * masterVolume;

        switch (layerName.ToLowerInvariant())
        {
            case "ambient":
                ambientSource.volume = scaledVol;
                break;
            case "pressure":
                if (pressureFadeRoutine != null) { StopCoroutine(pressureFadeRoutine); pressureFadeRoutine = null; }
                pressureSource.volume = scaledVol > 0.01f ? targetVol : 0f;
                if (scaledVol > 0.01f)
                {
                    // Strict exclusivity: ambient + pressure only
                    if (riskSource != null) riskSource.volume = 0f;
                    if (dangerSource != null) dangerSource.volume = 0f;
                    if (heartbeatSource != null) heartbeatSource.volume = 0f;
                    CurrentMusicState = MusicLayerState.AmbientPressure;
                }
                else if (CurrentMusicState == MusicLayerState.AmbientPressure)
                {
                    CurrentMusicState = MusicLayerState.AmbientOnly;
                }
                break;
            case "risk":
                if (riskFadeRoutine != null) { StopCoroutine(riskFadeRoutine); riskFadeRoutine = null; }
                riskSource.volume = scaledVol > 0.01f ? targetVol : 0f;
                if (scaledVol > 0.01f)
                {
                    // Strict exclusivity: ambient + risk only
                    if (pressureSource != null) pressureSource.volume = 0f;
                    if (dangerSource != null) dangerSource.volume = 0f;
                    if (heartbeatSource != null) heartbeatSource.volume = 0f;
                    CurrentMusicState = MusicLayerState.AmbientRisk;
                }
                else if (CurrentMusicState == MusicLayerState.AmbientRisk)
                {
                    CurrentMusicState = MusicLayerState.AmbientOnly;
                }
                break;
            case "danger":
                if (dangerFadeRoutine != null) { StopCoroutine(dangerFadeRoutine); dangerFadeRoutine = null; }
                dangerSource.volume = scaledVol > 0.01f ? targetVol : 0f;
                if (scaledVol > 0.01f)
                {
                    if (pressureSource != null) pressureSource.volume = 0f;
                    if (riskSource != null) riskSource.volume = 0f;
                    CurrentMusicState = (heartbeatSource != null && heartbeatSource.volume > 0.01f)
                        ? MusicLayerState.AmbientDangerHeartbeat
                        : MusicLayerState.AmbientDanger;
                }
                else
                {
                    if (heartbeatSource != null) heartbeatSource.volume = 0f;
                    CurrentMusicState = MusicLayerState.AmbientOnly;
                }
                break;
            case "heartbeat":
                if (heartbeatFadeRoutine != null) { StopCoroutine(heartbeatFadeRoutine); heartbeatFadeRoutine = null; }
                heartbeatSource.volume = scaledVol > 0.01f ? targetVol : 0f;
                if (scaledVol > 0.01f)
                {
                    // Heartbeat only pairs with Danger for victim
                    if (dangerSource != null) dangerSource.volume = targetVol;
                    if (pressureSource != null) pressureSource.volume = 0f;
                    if (riskSource != null) riskSource.volume = 0f;
                    CurrentMusicState = MusicLayerState.AmbientDangerHeartbeat;
                }
                else if (CurrentMusicState == MusicLayerState.AmbientDangerHeartbeat)
                {
                    CurrentMusicState = (dangerSource != null && dangerSource.volume > 0.01f)
                        ? MusicLayerState.AmbientDanger
                        : MusicLayerState.AmbientOnly;
                }
                break;
        }
    }

    public float GetLayerVolume(string layerName)
    {
        return layerName.ToLowerInvariant() switch
        {
            "ambient" => ambientSource != null ? ambientSource.volume : 0f,
            "pressure" => pressureSource != null ? pressureSource.volume : 0f,
            "risk" => riskSource != null ? riskSource.volume : 0f,
            "danger" => dangerSource != null ? dangerSource.volume : 0f,
            "heartbeat" => heartbeatSource != null ? heartbeatSource.volume : 0f,
            _ => 0f
        };
    }

    // --- UI & Card Interactions ---

    private float lastHoverSoundTime;
    private float hoverSuppressedUntil;

    public void SuppressHoverFor(float duration = 0.35f)
    {
        hoverSuppressedUntil = Mathf.Max(hoverSuppressedUntil, Time.unscaledTime + duration);
    }

    public void PlayHover()
    {
        if (Time.unscaledTime < hoverSuppressedUntil)
            return;

        if (Time.unscaledTime - lastHoverSoundTime < 0.1f)
            return;

        lastHoverSoundTime = Time.unscaledTime;
        AudioClip[] hovers = { uiHover1, uiHover2, uiHover3, uiHover4, uiHover5 };
        AudioClip chosen = hovers[Random.Range(0, hovers.Length)];
        PlayUi(chosen, 0.45f);
    }

    public void PlayAccept() => PlayUi(uiAccept, 0.55f);

    public void PlayCancel()
    {
        SuppressHoverFor(0.4f);
        PlayUi(uiCancel);
    }
    public void PlayUnavailable() => PlayUi(uiUnavailable, 0.55f);
    public void PlayMenuOpen() => PlayUi(uiMenuOpen, 0.55f);
    public void PlayMenuClose() => PlayUi(uiMenuClose, 0.55f);
    public void PlayMenuPress() => PlayUi(uiMenuPress, 0.55f);

    public void PlayHandsPick() => PlaySfx(handsPick);
    public void PlayHandsRoll() => PlaySfx(handsRoll);

    // --- Game Loop, Cards & Economy ---

    public void PlayCardsShuffle() => PlaySfx(cardsShuffle);

    public void PlayCardDealing(int holeIndex = 1)
    {
        AudioClip clip = holeIndex switch
        {
            2 => cardsGivingHole2,
            3 => cardsGivingHole3,
            _ => cardsGivingHole1
        };
        PlaySfx(clip != null ? clip : cardsGivingHole1);
    }

    public void PlayNextStage() => PlaySfx(nextStageSound);
    public void PlayNextTurn() => PlaySfx(nextSound);
    public void PlayCoinToss() => PlaySfx(coinToss);
    public void PlayPointsAdd() => PlaySfx(pointsAdd);
    public void PlayAccessoryAdd() => PlaySfx(accessoryAdd);
    public void PlayAccessoryDisappear() => PlaySfx(accessoryDisappearSound);
    public void PlayTenSecondsLeft() => PlaySfx(tenLeft);
    public void PlayCardBurn() => PlaySfx(cardBurnSound);
    public void PlayCardDestroy() => PlaySfx(cardDestroy);
    public void PlayAttackSuccessful(float delay = 0.5f)
    {
        if (delay > 0f)
            StartCoroutine(PlayDelayedSfxRoutine(attackSuccessful, delay));
        else
            PlaySfx(attackSuccessful);
    }

    public void PlayAttackUnsuccessful(float delay = 0.5f)
    {
        if (delay > 0f)
            StartCoroutine(PlayDelayedSfxRoutine(attackUnsuccessful, delay));
        else
            PlaySfx(attackUnsuccessful);
    }

    private IEnumerator PlayDelayedSfxRoutine(AudioClip clip, float delay, float volumeScale = 1f)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (clip != null)
            PlaySfx(clip, volumeScale);
    }
    public void PlayWin() => PlaySfx(winSound);
    public void PlayLost() => PlaySfx(lostSound);

    // --- Platform & Roulette ---

    public void PlayPlatformBase() => PlayPlatform(platformBase);
    public void PlayPlatformPlateDown() => PlayPlatform(platformPlateDown);
    public void PlayPlatformPlateUp() => PlayPlatform(platformPlateUp);
    public void PlayPlatformSlotMachineDown() => PlayPlatform(platformSmDown);
    public void PlayPlatformSlotMachineUp() => PlayPlatform(platformSmUp);

    // --- Slot Machine ---

    public void PlaySlotMachineWindup() => PlaySfx(slotMachineWindup);
    public void PlaySlotMachineSpin() => PlaySfx(slotMachineSpin);
    public void PlaySlotMachineStop() => PlaySfx(slotMachineStop);
    public void PlaySlotMachineCardGive() => PlaySfx(slotMachineCardGive);

    public void PlaySlotMachinePick(int index = 0)
    {
        AudioClip clip = index switch
        {
            1 => slotMachinePick2,
            2 => slotMachinePick3,
            _ => slotMachinePick1
        };
        PlaySfx(clip != null ? clip : slotMachinePick1);
    }

    // --- Character Reactions ---

    public void PlayChairCreak() => PlayVoice(creakingChairSound, 0.35f);

    public void PlayVictimReaction()
    {
        AudioClip[] clips = { exhaleSound, caughSound, disappointmentSound, scoffSound };
        AudioClip chosen = clips[Random.Range(0, clips.Length)];
        PlayVoice(chosen, 0.85f);
    }

    public void PlayAttackerReaction()
    {
        AudioClip[] clips = { joyfulSound, whistleSound, wondermentSound, burpSound };
        AudioClip chosen = clips[Random.Range(0, clips.Length)];
        PlayVoice(chosen, 0.85f);
    }

    public AudioClip GetReactionClip(ReactionClipType reactionType)
    {
        return reactionType switch
        {
            ReactionClipType.Burp => burpSound,
            ReactionClipType.Caugh => caughSound,
            ReactionClipType.CreakingChair => creakingChairSound,
            ReactionClipType.Exhale => exhaleSound,
            ReactionClipType.Joyful => joyfulSound,
            ReactionClipType.Whistle => whistleSound,
            ReactionClipType.Wonderment => wondermentSound,
            ReactionClipType.Scoff => scoffSound,
            ReactionClipType.Disappointment => disappointmentSound,
            _ => null
        };
    }

    public static bool IsIdleReaction(ReactionClipType reactionType)
    {
        return reactionType == ReactionClipType.Burp ||
               reactionType == ReactionClipType.Caugh ||
               reactionType == ReactionClipType.CreakingChair ||
               reactionType == ReactionClipType.Exhale;
    }

    public void PlayPlayerReactionClip(
        NetworkIdentity speakerIdentity,
        ReactionClipType reactionType,
        float delay = 0f)
    {
        AudioClip clip = GetReactionClip(reactionType);
        if (clip == null)
            return;

        float volumeScale = IsIdleReaction(reactionType) ? idleVolumeMultiplier : 1f;
        PlaySoundAtPlayer(speakerIdentity, clip, volumeScale, isAftermath: false, delay: delay, isVoice: true);
    }

    public void PlaySoundAtPlayer(
        Player player,
        AudioClip clip,
        float volumeScale = 1f,
        bool isAftermath = false,
        float delay = 0f,
        bool isVoice = false)
    {
        PlaySoundAtPlayer(player != null ? player.netIdentity : null, clip, volumeScale, isAftermath, delay, isVoice);
    }

    private readonly Dictionary<(uint, AudioClip), float> lastPlayedClipTimes = new();

    public void PlaySoundAtPlayer(
        NetworkIdentity speakerIdentity,
        AudioClip clip,
        float volumeScale = 1f,
        bool isAftermath = false,
        float delay = 0f,
        bool isVoice = false)
    {
        if (clip == null)
            return;

        uint netId = speakerIdentity != null ? speakerIdentity.netId : 0;
        if (lastPlayedClipTimes.TryGetValue((netId, clip), out float lastTime))
        {
            if (Time.unscaledTime - lastTime < 0.25f)
            {
                return;
            }
        }
        lastPlayedClipTimes[(netId, clip)] = Time.unscaledTime;

        StartCoroutine(PlaySoundAtPlayerRoutine(speakerIdentity, clip, volumeScale, isAftermath, delay, isVoice));
    }

    private IEnumerator PlaySoundAtPlayerRoutine(
        NetworkIdentity speakerIdentity,
        AudioClip clip,
        float volumeScale,
        bool isAftermath,
        float delay,
        bool isVoice)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (clip == null)
            yield break;

        bool isLocal = false;
        if (speakerIdentity == null)
        {
            isLocal = true;
        }
        else if (NetworkClient.localPlayer != null)
        {
            isLocal = speakerIdentity.netId == NetworkClient.localPlayer.netId;
        }
        else
        {
            Player p = speakerIdentity.GetComponent<Player>();
            if (p != null && p.isLocalPlayer)
                isLocal = true;
        }

        if (isLocal)
        {
            if (isAftermath)
                PlayAftermath(clip);
            else if (isVoice)
                PlayVoice(clip, volumeScale);
            else
                PlaySfx(clip, volumeScale);
            yield break;
        }

        // It is a remote player or a bot: play in 3D attached to speaker
        Transform speakerTransform = speakerIdentity.transform;
        Transform audioChild = speakerTransform.Find("PlayerVoiceAudio");
        AudioSource speakerSource;

        if (audioChild == null)
        {
            GameObject childObj = new GameObject("PlayerVoiceAudio");
            childObj.transform.SetParent(speakerTransform, false);
            childObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            speakerSource = childObj.AddComponent<AudioSource>();
            speakerSource.playOnAwake = false;
            speakerSource.loop = false;
            speakerSource.rolloffMode = AudioRolloffMode.Linear;
            speakerSource.minDistance = 1.5f;
            speakerSource.maxDistance = 15f;
        }
        else
        {
            speakerSource = audioChild.GetComponent<AudioSource>();
            if (speakerSource == null)
                speakerSource = audioChild.gameObject.AddComponent<AudioSource>();
        }

        speakerSource.spatialBlend = 0.85f;
        speakerSource.panStereo = 0f;

        if (isAftermath)
        {
            float totalGain = Mathf.Max(0f, aftermathVolumeMultiplier) * volumeScale;
            int fullShots = Mathf.FloorToInt(totalGain);
            float remainder = totalGain - fullShots;
            speakerSource.volume = sfxVolume * masterVolume;

            for (int i = 0; i < fullShots; i++)
            {
                speakerSource.PlayOneShot(clip, 1f);
            }

            if (remainder > 0.01f)
            {
                speakerSource.PlayOneShot(clip, remainder);
            }
        }
        else
        {
            speakerSource.volume = (isVoice ? voiceVolume : sfxVolume) * masterVolume;
            speakerSource.PlayOneShot(clip, volumeScale);
        }
    }

    // --- Weapons ---

    public AudioClip GetAftermathClip(string cardOrActionName)
    {
        string key = NormalizeWeaponKey(cardOrActionName);
        return key switch
        {
            "knife" => aftermathKnifeSound,
            "shears" => aftermathShearsSound,
            "bucket" => aftermathBucketSound,
            "fork" => aftermathForkSound,
            "grenade" => aftermathGrenadeSound,
            "shotgun" => aftermathShotgunSound,
            "whiskey" => aftermathWhiskeySound,
            "flame" => null,
            _ => aftermathRevolverSound
        };
    }

    public void PlayWeaponReady(string cardOrActionName, NetworkIdentity speakerIdentity = null)
    {
        string key = NormalizeWeaponKey(cardOrActionName);
        AudioClip readyClip = key switch
        {
            "knife" => knifeReady,
            "shears" => shearsReady,
            "bucket" => bucketReady,
            "fork" => forkReady,
            "grenade" => grenadeReady,
            "shotgun" => shotgunReady,
            "whiskey" => whiskeyReady,
            "flame" => null, // Flame prep sound (ATK_FLAME_ATK_PREP_SOUND) is already played by Blowtorch_Prep StateMachineBehaviour
            _ => pistolReady
        };

        if (readyClip != null)
        {
            PlaySoundAtPlayer(speakerIdentity, readyClip);
        }
    }

    public void PlayWeaponCancel(string cardOrActionName, NetworkIdentity attackerIdentity = null)
    {
        string key = NormalizeWeaponKey(cardOrActionName);
        AudioClip cancelClip = key switch
        {
            "knife" => knifeCancel,
            "shears" => shearsCancel,
            "bucket" => bucketCancel,
            "fork" => forkCancel,
            "grenade" => grenadeCancel,
            "shotgun" => shotgunCancel,
            "whiskey" => whiskeyCancel,
            "flame" => atkFlameEndSound,
            _ => pistolCancel
        };

        if (cancelClip != null)
        {
            PlaySoundAtPlayer(attackerIdentity, cancelClip);
        }
    }

    public void PlayWeaponAttackAndHit(string cardOrActionName, bool hitTarget = true, NetworkIdentity attackerIdentity = null)
    {
        string key = NormalizeWeaponKey(cardOrActionName);
        AudioClip actionClip = null;

        switch (key)
        {
            case "knife":
                actionClip = hitTarget ? (atkKnifeVicHitSound != null ? atkKnifeVicHitSound : knifeAction) : knifeAction;
                break;
            case "shears":
                actionClip = hitTarget ? (atkShearsVicHitSound != null ? atkShearsVicHitSound : shearsAction) : shearsAction;
                break;
            case "bucket":
                actionClip = hitTarget ? (atkBucketVicHitSound != null ? atkBucketVicHitSound : bucketAction) : bucketAction;
                break;
            case "fork":
                actionClip = hitTarget ? (atkForkVicHitSound != null ? atkForkVicHitSound : forkAction) : forkAction;
                break;
            case "grenade":
                actionClip = hitTarget ? (grenadeBoom != null ? grenadeBoom : grenadeAction) : grenadeAction;
                break;
            case "shotgun":
                actionClip = shotgunAction;
                break;
            case "whiskey":
                actionClip = hitTarget ? (atkWhiskyVicHitSound != null ? atkWhiskyVicHitSound : whiskeyAction) : whiskeyAction;
                break;
            case "flame":
                actionClip = atkFlameActionSound;
                break;
            default:
                actionClip = pistolAction;
                break;
        }

        if (actionClip != null)
        {
            PlaySoundAtPlayer(attackerIdentity, actionClip);
        }
    }

    public void PlayWeaponAftermath(string cardOrActionName, NetworkIdentity attackerIdentity = null)
    {
        AudioClip aftermathClip = GetAftermathClip(cardOrActionName);
        if (aftermathClip != null)
        {
            PlaySoundAtPlayer(attackerIdentity, aftermathClip, 1f, isAftermath: true);
        }
    }

    private string NormalizeWeaponKey(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "pistol";
        string lower = raw.ToLowerInvariant();

        if (lower.Contains("нож") || lower.Contains("knife"))
            return "knife";
        if (lower.Contains("ножниц") || lower.Contains("shears"))
            return "shears";
        if (lower.Contains("ведр") || lower.Contains("bucket"))
            return "bucket";
        if (lower.Contains("вилк") || lower.Contains("fork"))
            return "fork";
        if (lower.Contains("гранат") || lower.Contains("grenade"))
            return "grenade";
        if (lower.Contains("дробов") || lower.Contains("shotgun"))
            return "shotgun";
        if (lower.Contains("виски") || lower.Contains("whisk"))
            return "whiskey";
        if (lower.Contains("огн") || lower.Contains("flame") || lower.Contains("fire"))
            return "flame";

        return "pistol";
    }
}
