using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>One voice sound with its own volume.</summary>
[Serializable]
public class VoiceSound
{
    // Shown as the element label in the inspector (filled automatically).
    public string name;

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
}

/// <summary>
/// All character sounds. Put it on the same object as Player.
///
///  - Idle sounds (exhale, burp, caugh, creaking chair): scheduled by the
///    server for ALL players together - never two at once, never the same
///    clip twice in a row. The player who makes the sound hears it close,
///    everybody else hears it from far away (quieter + muffled).
///  - Reactions after success / failure.
///  - ATK_*_VIC_HIT: plays on the victim when an attack lands.
///  - AFTERMATH_*: plays in the audience when the attack is cancelled.
///
/// ATK / AFTERMATH clips (and their volumes) live in CardData.
/// Every sound has its own Volume; the "Master" volumes multiply on top.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSounds : NetworkBehaviour
{
    public enum Outcome
    {
        Success,
        Failure
    }

    /// <summary>What caused the reaction (decides the chance to play it).</summary>
    public enum ReactionContext
    {
        Always,
        AfterAttackHit,
        AfterAftermath
    }

    [Header("Audio Sources (created automatically if empty)")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioMixerGroup mixerGroup;

    [Header("3D")]
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 30f;

    [Header("Idle: exhale, burp, caugh, creaking chair")]
    [SerializeField] private VoiceSound[] idleSounds;
    [SerializeField] private Vector2 idleInterval = new Vector2(8f, 20f);
    [Tooltip("Pause after ANY idle sound before the next one (any player).")]
    [SerializeField] private float idleGap = 1.5f;
    [Tooltip("No idle sound this long after a reaction of this player.")]
    [SerializeField] private float quietAfterVoice = 3f;

    [Header("Idle: how OTHER players hear it (far away)")]
    [Range(0f, 1f)]
    [SerializeField] private float remoteIdleVolume = 0.35f;
    [Tooltip("Low-pass cutoff for far idle sounds. Lower = more muffled.")]
    [SerializeField] private float remoteLowPassHz = 2500f;

    [Header("Reactions")]
    [Tooltip("joyful, whistle, wonderment")]
    [SerializeField] private VoiceSound[] successSounds;
    [Tooltip("disappointment, scoff")]
    [SerializeField] private VoiceSound[] failureSounds;
    [Tooltip("Pause between the event (hit, block...) and the reaction.")]
    [SerializeField] private float reactionDelay = 1.2f;
    [Tooltip("Chance (0..1) of a voice reaction after an ATK hit. " +
             "Applies to the attacker and to the victim.")]
    [Range(0f, 1f)]
    [SerializeField] private float chanceAfterAttackHit = 0.5f;
    [Tooltip("Chance (0..1) of a voice reaction after AFTERMATH.")]
    [Range(0f, 1f)]
    [SerializeField] private float chanceAfterAftermath = 0.5f;

    [Header("Master volume (multiplies the per-sound volume)")]
    [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float aftermathVolume = 1f;

    [Header("Debug")]
    [Tooltip("Logs every ATK / AFTERMATH step to the Console.")]
    [SerializeField] private bool debugLog = true;

    private Player player;
    private AudioLowPassFilter lowPass;

    // ---- server only ----
    private Coroutine idleRoutine;
    private float lastVoiceTime = -999f;
    private int lastIdleIndex = -1;
    private int lastSuccessIndex = -1;
    private int lastFailureIndex = -1;

    // Shared by all players on the server: this is what keeps idle
    // sounds from overlapping and from repeating.
    private static float globalIdleBusyUntil;
    private static AudioClip lastGlobalIdleClip;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        globalIdleBusyUntil = 0f;
        lastGlobalIdleClip = null;
    }

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        player =
            GetComponentInParent<Player>();

        voiceSource =
            EnsureSource(
                voiceSource,
                "VoiceSource");

        sfxSource =
            EnsureSource(
                sfxSource,
                "SfxSource");

        if (!voiceSource.TryGetComponent(
                out lowPass))
        {
            lowPass =
                voiceSource.gameObject
                    .AddComponent<AudioLowPassFilter>();
        }

        lowPass.enabled = false;
    }

    private void OnValidate()
    {
        FillNames(idleSounds);
        FillNames(successSounds);
        FillNames(failureSounds);

        if (idleInterval.y < idleInterval.x)
            idleInterval.y = idleInterval.x;
    }

    private static void FillNames(
        VoiceSound[] sounds)
    {
        if (sounds == null)
            return;

        foreach (VoiceSound sound in sounds)
        {
            if (sound != null &&
                sound.clip != null)
            {
                sound.name = sound.clip.name;
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (idleRoutine != null)
            StopCoroutine(idleRoutine);

        idleRoutine =
            StartCoroutine(IdleRoutine());
    }

    public override void OnStopServer()
    {
        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }

        base.OnStopServer();
    }

    private AudioSource EnsureSource(
        AudioSource existing,
        string childName)
    {
        // Source assigned in the inspector: respect its settings.
        if (existing != null)
            return existing;

        GameObject child =
            new GameObject(childName);

        child.transform.SetParent(
            transform,
            false);

        AudioSource source =
            child.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode =
            AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;

        if (mixerGroup != null)
            source.outputAudioMixerGroup = mixerGroup;

        return source;
    }

    // =========================================================
    // SERVER API
    // =========================================================

    /// <summary>
    /// Attack landed: ATK_*_VIC_HIT of the card plays on this (victim) player.
    /// </summary>
    [Server]
    public void PlayAttackHit(
        Card card)
    {
        if (card == null)
        {
            Log("SERVER attack hit: card is null");
            return;
        }

        Log(
            $"SERVER attack hit: card='{card.name}' " +
            $"data='{(card.Data != null ? card.Data.name : "NULL")}' " +
            $"clip='{(card.Data != null && card.Data.AttackHitSound != null ? card.Data.AttackHitSound.name : "NULL")}'");

        RpcPlayAttackHit(
            card.netIdentity);
    }

    /// <summary>
    /// Attack cancelled: the thrown item lands in the audience.
    /// Call on the ATTACKER's PlayerSounds.
    /// </summary>
    [Server]
    public void PlayAftermath(
        Card card)
    {
        if (card == null)
        {
            Log("SERVER aftermath: card is null");
            return;
        }

        Log(
            $"SERVER aftermath: card='{card.name}' " +
            $"data='{(card.Data != null ? card.Data.name : "NULL")}' " +
            $"clip='{(card.Data != null && card.Data.AftermathSound != null ? card.Data.AftermathSound.name : "NULL")}'");

        RpcPlayAftermath(
            card.netIdentity);
    }

    /// <summary>
    /// Success / failure reaction of this player.
    /// context decides the chance that it is played at all
    /// (Always = 100%). extraDelay is added to reactionDelay
    /// (e.g. to wait for AFTERMATH).
    /// </summary>
    [Server]
    public void React(
        Outcome outcome,
        ReactionContext context = ReactionContext.Always,
        float extraDelay = 0f)
    {
        // The dice are rolled first: a skipped reaction must not
        // block idle sounds either.
        if (!RollChance(context))
            return;

        float wait =
            reactionDelay +
            Mathf.Max(0f, extraDelay);

        // Keep idle sounds away while the reaction is pending.
        lastVoiceTime =
            Mathf.Max(
                lastVoiceTime,
                Time.time + wait);

        StartCoroutine(
            ReactRoutine(
                outcome,
                wait));
    }

    // =========================================================
    // SERVER LOGIC
    // =========================================================

    private bool RollChance(
        ReactionContext context)
    {
        float chance;

        switch (context)
        {
            case ReactionContext.AfterAttackHit:
                chance = chanceAfterAttackHit;
                break;

            case ReactionContext.AfterAftermath:
                chance = chanceAfterAftermath;
                break;

            default:
                return true;
        }

        return chance > 0f &&
               UnityEngine.Random.value <= chance;
    }

    private IEnumerator ReactRoutine(
        Outcome outcome,
        float wait)
    {
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        VoiceSound[] pool =
            outcome == Outcome.Success
                ? successSounds
                : failureSounds;

        if (pool == null ||
            pool.Length == 0)
        {
            yield break;
        }

        int index;

        if (outcome == Outcome.Success)
        {
            index =
                PickIndex(
                    pool.Length,
                    lastSuccessIndex);

            lastSuccessIndex = index;
        }
        else
        {
            index =
                PickIndex(
                    pool.Length,
                    lastFailureIndex);

            lastFailureIndex = index;
        }

        VoiceSound sound = pool[index];

        if (sound == null ||
            sound.clip == null)
        {
            yield break;
        }

        lastVoiceTime = Time.time;

        // Idle sounds of everybody wait until the reaction is over.
        globalIdleBusyUntil =
            Mathf.Max(
                globalIdleBusyUntil,
                Time.time + sound.clip.length);

        RpcPlayReaction(
            outcome,
            index);
    }

    private IEnumerator IdleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                UnityEngine.Random.Range(
                    idleInterval.x,
                    idleInterval.y));

            // Wait for our turn: nobody else may be making an idle
            // sound right now, and this player must be free.
            while (!CanPlayIdle())
            {
                yield return new WaitForSeconds(
                    UnityEngine.Random.Range(
                        0.5f,
                        1.5f));
            }

            int index =
                PickIdleIndex();

            if (index < 0)
                continue;

            AudioClip clip =
                idleSounds[index].clip;

            lastIdleIndex = index;
            lastVoiceTime = Time.time;

            // Reserve the shared slot BEFORE telling the clients.
            lastGlobalIdleClip = clip;

            globalIdleBusyUntil =
                Time.time +
                clip.length +
                idleGap;

            RpcPlayIdle(index);
        }
    }

    private bool CanPlayIdle()
    {
        if (idleSounds == null ||
            idleSounds.Length == 0)
        {
            return false;
        }

        // another player's idle sound (or a reaction) is playing
        if (Time.time < globalIdleBusyUntil)
            return false;

        if (Time.time - lastVoiceTime < quietAfterVoice)
            return false;

        if (GameMaster.Instance == null)
            return false;

        // Not idle while this player is the attacker or the target.
        AttackSequence sequence =
            AttackSequence.Instance;

        if (sequence != null &&
            sequence.IsActive &&
            player != null)
        {
            if (sequence.Attacker == player)
                return false;

            if (sequence.Target != null &&
                sequence.Target.GetComponentInParent<Player>() == player)
            {
                return false;
            }
        }

        return true;
    }

    // Prefers a clip that is different from this player's previous one
    // AND from the last idle clip of any player; relaxes the rules if
    // there are not enough clips.
    private int PickIdleIndex()
    {
        List<int> pool =
            new List<int>();

        for (int pass = 0; pass < 3; pass++)
        {
            for (int i = 0;
                 i < idleSounds.Length;
                 i++)
            {
                AudioClip clip =
                    idleSounds[i] != null
                        ? idleSounds[i].clip
                        : null;

                if (clip == null)
                    continue;

                if (pass < 2 &&
                    clip == lastGlobalIdleClip)
                {
                    continue;
                }

                if (pass < 1 &&
                    i == lastIdleIndex)
                {
                    continue;
                }

                pool.Add(i);
            }

            if (pool.Count > 0)
                break;
        }

        if (pool.Count == 0)
            return -1;

        return pool[
            UnityEngine.Random.Range(
                0,
                pool.Count)];
    }

    // random index, never the same as the previous one
    private static int PickIndex(
        int length,
        int last)
    {
        if (length <= 1)
            return 0;

        int index;

        do
        {
            index =
                UnityEngine.Random.Range(0, length);
        }
        while (index == last);

        return index;
    }

    // =========================================================
    // CLIENT RPC
    // =========================================================

    [ClientRpc]
    private void RpcPlayIdle(
        int index)
    {
        // The owner hears his own sound close, everybody else
        // hears it from far away.
        PlayVoice(
            GetSound(
                idleSounds,
                index),
            !isLocalPlayer);
    }

    [ClientRpc]
    private void RpcPlayReaction(
        Outcome outcome,
        int index)
    {
        PlayVoice(
            GetSound(
                outcome == Outcome.Success
                    ? successSounds
                    : failureSounds,
                index),
            false);
    }

    [ClientRpc]
    private void RpcPlayAttackHit(
        NetworkIdentity cardIdentity)
    {
        CardData data =
            GetCardData(cardIdentity);

        if (data == null ||
            data.AttackHitSound == null ||
            sfxSource == null)
        {
            Log(
                "CLIENT attack hit: NOT played " +
                $"(data={(data != null ? data.name : "NULL")}, " +
                $"clip={(data != null && data.AttackHitSound != null ? "ok" : "NULL")}, " +
                $"sfxSource={(sfxSource != null ? "ok" : "NULL")})");

            return;
        }

        Log(
            $"CLIENT attack hit: playing '{data.AttackHitSound.name}' " +
            $"volume={data.AttackHitVolume * hitVolume:0.00}");

        sfxSource.PlayOneShot(
            data.AttackHitSound,
            data.AttackHitVolume * hitVolume);
    }

    [ClientRpc]
    private void RpcPlayAftermath(
        NetworkIdentity cardIdentity)
    {
        CardData data =
            GetCardData(cardIdentity);

        if (data == null ||
            data.AftermathSound == null)
        {
            Log(
                "CLIENT aftermath: NOT played " +
                $"(identity={(cardIdentity != null ? "ok" : "NULL")}, " +
                $"data={(data != null ? data.name : "NULL")}, " +
                $"clip={(data != null && data.AftermathSound != null ? "ok" : "NULL")})");

            return;
        }

        Log(
            $"CLIENT aftermath: '{data.AftermathSound.name}' in " +
            $"{data.AftermathDelay:0.00}s, volume=" +
            $"{data.AftermathVolume * aftermathVolume:0.00} " +
            $"(card {data.AftermathVolume:0.00} x master {aftermathVolume:0.00})");

        StartCoroutine(
            AftermathRoutine(
                data.AftermathSound,
                data.AftermathDelay,
                data.AftermathVolume * aftermathVolume));
    }

    private IEnumerator AftermathRoutine(
        AudioClip clip,
        float delay,
        float volume)
    {
        // time for the item to fly into the hall
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (GameAudio.Instance != null)
        {
            GameAudio.Instance.PlayAtAudience(
                clip,
                volume);
        }
        else if (sfxSource != null)
        {
            sfxSource.PlayOneShot(
                clip,
                volume);
        }
    }

    // =========================================================
    // CLIENT HELPERS
    // =========================================================

    private void Log(
        string message)
    {
        if (debugLog)
        {
            Debug.Log(
                $"[PlayerSounds/{name}] {message}",
                this);
        }
    }

    private void PlayVoice(
        VoiceSound sound,
        bool distant)
    {
        if (sound == null ||
            sound.clip == null ||
            voiceSource == null)
        {
            return;
        }

        float volume =
            sound.volume * voiceVolume;

        if (distant)
            volume *= remoteIdleVolume;

        if (lowPass != null)
        {
            lowPass.enabled = distant;

            if (distant)
                lowPass.cutoffFrequency = remoteLowPassHz;
        }

        // A new voice line cuts the previous one.
        voiceSource.Stop();
        voiceSource.clip = sound.clip;
        voiceSource.volume = volume;
        voiceSource.Play();
    }

    private static VoiceSound GetSound(
        VoiceSound[] sounds,
        int index)
    {
        if (sounds == null ||
            index < 0 ||
            index >= sounds.Length)
        {
            return null;
        }

        return sounds[index];
    }

    private static CardData GetCardData(
        NetworkIdentity identity)
    {
        if (identity == null)
            return null;

        return identity.TryGetComponent(
            out Card card)
            ? card.Data
            : null;
    }
}
