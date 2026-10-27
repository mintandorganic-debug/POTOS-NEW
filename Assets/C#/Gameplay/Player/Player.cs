using Mirror;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering.HighDefinition;

public class Player : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject camera;
    [SerializeField] private GameObject dealerToken;
    [SerializeField] private GameObject enemyCanvas;
    [SerializeField] private Collider ownCollider;
    [SerializeField] public CameraLook cameraController;

    [SerializeField] public PlayerAnimator animator;

    [Header("Player Info")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text dealerTokenText;

    [Header("Cards")]
    [SerializeField] public Transform tumbSocket;
    [SerializeField] public List<GameObject> cardSockets;
    [SerializeField] public Transform cardZoomSocket;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private PlayerHand hand;

    [Header("Accessories")]
    [SerializeField] public List<GameObject> accesories;
    [SerializeField] public List<DissolveVFXController> accesoryVfxes;

    [Header("Passes")]
    [SerializeField] public CustomPassVolume DamagePass;
    [SerializeField] float damageDuration;

    [SyncVar]
    private int seatIndex = -1;

    [SyncVar]
    private ulong steamId;

    [SyncVar(hook = nameof(OnSteamNicknameChanged))]
    private string steamNickname;

    [SyncVar(hook = nameof(OnAccessoryMaskChanged))]
    private int accessoryMask;

    private Coroutine damagePassCoroutine;

    public int SeatIndex =>
        seatIndex;

    public ulong SteamId =>
        steamId;

    public string SteamNickname =>
        steamNickname;

    public PlayerHand Hand =>
        hand;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (hand == null)
            hand = GetComponent<PlayerHand>();

        if (camera != null)
        {
            Camera cam =
                camera.GetComponent<Camera>();

            if (cam != null)
                cam.enabled = false;

            AudioListener listener =
                camera.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = false;
        }

        if (DamagePass != null)
            DamagePass.gameObject.SetActive(false);

        if (dealerToken != null)
            dealerToken.SetActive(false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        accessoryMask = 0;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        ApplyNickname();

        ApplyAccessoryMask(
            accessoryMask);

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.CurrentPlayerChanged -=
                OnCurrentPlayerChanged;

            GameMaster.Instance.CurrentPlayerChanged +=
                OnCurrentPlayerChanged;

            ApplyDealerToken(
                GameMaster.Instance.CurrentPlayerIndex);
        }
        else
        {
            ApplyDealerToken(-1);
        }
    }

    public override void OnStopClient()
    {
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.CurrentPlayerChanged -=
                OnCurrentPlayerChanged;
        }

        base.OnStopClient();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (camera != null)
        {
            Camera cam =
                camera.GetComponent<Camera>();

            if (cam != null)
                cam.enabled = true;

            AudioListener listener =
                camera.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = true;
        }

        if (enemyCanvas != null)
            enemyCanvas.SetActive(false);

        if (ownCollider != null)
            ownCollider.enabled = false;

        dealerTokenText.text = "NEXT";

        CmdNotifyLoaded();
    }

    // =========================================================
    // TURN TOKEN
    // =========================================================

    private void OnCurrentPlayerChanged(
        int currentPlayerIndex)
    {
        ApplyDealerToken(
            currentPlayerIndex);
    }

    private void ApplyDealerToken(
        int currentPlayerIndex)
    {
        if (dealerToken == null)
            return;

        dealerToken.SetActive(
            seatIndex >= 0 &&
            seatIndex == currentPlayerIndex);
    }

    // =========================================================
    // LOADING
    // =========================================================

    [Command]
    private void CmdNotifyLoaded()
    {
        if (GameMaster.Instance == null)
        {
            Debug.LogError(
                "Player: GameMaster instance is missing.");

            return;
        }

        if (connectionToClient == null)
        {
            Debug.LogError(
                "Player: connectionToClient is null.");

            return;
        }

        GameMaster.Instance.PlayerLoaded(
            connectionToClient);
    }

    // =========================================================
    // STEAM DATA
    // =========================================================

    [Server]
    public void SetSteamData(
        ulong steamId,
        string nickname)
    {
        this.steamId = steamId;
        steamNickname = nickname;

        ApplyNickname();
    }

    private void OnSteamNicknameChanged(
        string oldValue,
        string newValue)
    {
        ApplyNickname();
    }

    private void ApplyNickname()
    {
        if (nicknameText == null)
            return;

        nicknameText.text =
            steamNickname ?? string.Empty;
    }

    // =========================================================
    // SEAT
    // =========================================================

    [Server]
    public void SetSeat(
        int index)
    {
        seatIndex = index;

        if (GameMaster.Instance != null)
        {
            ApplyDealerToken(
                GameMaster.Instance.CurrentPlayerIndex);
        }
    }

    // =========================================================
    // ACCESSORIES
    // =========================================================

    [Server]
    public bool SetAccesory(
        int index,
        bool value)
    {
        if (index < 0 ||
            index >= accesories.Count)
        {
            Debug.LogWarning(
                $"Invalid accessory index {index}. " +
                $"Count: {accesories.Count}",
                this);

            return false;
        }

        if (value)
        {
            accessoryMask |=
                (1 << index);
        }
        else
        {
            accessoryMask &=
                ~(1 << index);
        }

        ApplyAccessory(
            index,
            value);

        RpcPlayAccessoryVfx(
            index,
            value);

        return true;
    }

    private void OnAccessoryMaskChanged(
        int oldMask,
        int newMask)
    {
        ApplyAccessoryMask(
            newMask);
    }

    private void ApplyAccessoryMask(
        int mask)
    {
        if (accesories == null)
            return;

        for (int i = 0;
             i < accesories.Count;
             i++)
        {
            bool active =
                (mask & (1 << i)) != 0;

            ApplyAccessory(
                i,
                active);
        }
    }

    private void ApplyAccessory(
        int index,
        bool value)
    {
        if (index < 0 ||
            index >= accesories.Count)
        {
            return;
        }

        GameObject accessory =
            accesories[index];

        if (accessory == null)
            return;
    }

    // =========================================================
    // ACCESSORY QUERY
    // =========================================================

    public bool HasAccessory(
        int index)
    {
        if (index < 0 ||
            index >= accesories.Count)
        {
            return false;
        }

        return
            (accessoryMask & (1 << index)) != 0;
    }

    // =========================================================
    // ACCESSORY VFX
    // =========================================================

    [ClientRpc]
    private void RpcPlayAccessoryVfx(
        int index,
        bool value)
    {
        if (index < 0)
            return;

        if (value)
        {
            if (index >= accesoryVfxes.Count)
                return;

            if (accesoryVfxes[index] == null)
                return;

            accesoryVfxes[index].PlayAssemble();
        }
        else
        {

            if (index >= accesoryVfxes.Count)
                return;

            if (accesoryVfxes[index] == null)
                return;

            accesoryVfxes[index].PlayDissolve();
        }
    }

    public void StartDamagePass()
    {
        if (DamagePass == null)
            return;

        if (damagePassCoroutine != null)
            StopCoroutine(damagePassCoroutine);

        damagePassCoroutine =
            StartCoroutine(DamagePassRoutine());
    }

    private IEnumerator DamagePassRoutine()
    {
        DamagePass.gameObject.SetActive(true);

        yield return new WaitForSeconds(damageDuration);

        DamagePass.gameObject.SetActive(false);

        damagePassCoroutine = null;
    }

    // =========================================================
    // TURN
    // =========================================================

    public void OnTurnStart()
    {
        if (!isServer)
            return;

        if (GameMaster.Instance == null)
            return;

        ApplyDealerToken(
            GameMaster.Instance.CurrentPlayerIndex);
    }

    public void OnTurnEnd()
    {
        if (!isServer)
            return;

        if (dealerToken != null)
            dealerToken.SetActive(false);
    }

    // =========================================================
    // CARDS
    // =========================================================

    [Server]
    public void SummonCards()
    {
        if (hand == null)
        {
            Debug.LogError(
                "Player.SummonCards: PlayerHand is missing.",
                this);

            return;
        }

        for (int i = 0;
             i < cardSockets.Count;
             i++)
        {
            GameObject obj =
                Instantiate(cardPrefab);

            NetworkServer.Spawn(
                obj,
                connectionToClient);

            Card card =
                obj.GetComponent<Card>();

            if (card == null)
            {
                Debug.LogError(
                    "Player.SummonCards: spawned object has no Card component.",
                    obj);

                continue;
            }

            card.RandomData();

            hand.AddCard(card);
        }
    }
}