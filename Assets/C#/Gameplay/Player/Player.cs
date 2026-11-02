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

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        GameLog.Write(
            "Player",
            $"Awake object={gameObject.name} camera={(camera != null ? camera.name : "NULL")} damagePass={(DamagePass != null ? DamagePass.name : "NULL")} dealerToken={(dealerToken != null ? dealerToken.name : "NULL")}");

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

            GameLog.Write(
                "Player",
                $"Awake own camera and audio listener disabled camera={(camera != null ? camera.name : "NULL")}");
        }

        if (DamagePass != null)
            DamagePass.gameObject.SetActive(false);

        if (dealerToken != null)
            dealerToken.SetActive(false);

        GameLog.Write(
            "Player",
            $"Awake done damagePass and dealerToken set inactive object={gameObject.name}");
    }

    public override void OnStartServer()
    {
        GameLog.Write(
            "Player",
            $"OnStartServer player={netId} seat={seatIndex} oldAccessoryMask={accessoryMask}");

        base.OnStartServer();

        accessoryMask = 0;
    }

    public override void OnStartClient()
    {
        GameLog.Write(
            "Player",
            $"OnStartClient player={netId} isLocal={isLocalPlayer} isServer={isServer} seat={seatIndex} steamId={steamId} nickname={steamNickname} accessoryMask={accessoryMask} gameMaster={(GameMaster.Instance != null ? "OK" : "NULL")}");

        base.OnStartClient();

        ApplyNickname();

        DebugDeal.StyleAccessories(
            accesories);

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

            GameLog.Write(
                "Player",
                $"OnStartClient subscribed to CurrentPlayerChanged player={netId} currentPlayer={GameMaster.Instance.CurrentPlayerIndex}");
        }
        else
        {
            ApplyDealerToken(-1);

            GameLog.Write(
                "Player",
                $"OnStartClient GameMaster missing, dealer token reset player={netId}");
        }
    }

    public override void OnStopClient()
    {
        GameLog.Write(
            "Player",
            $"OnStopClient player={netId} gameMaster={(GameMaster.Instance != null ? "OK" : "NULL")}");

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.CurrentPlayerChanged -=
                OnCurrentPlayerChanged;
        }

        base.OnStopClient();
    }

    public override void OnStartLocalPlayer()
    {
        GameLog.Write(
            "Player",
            $"OnStartLocalPlayer player={netId} seat={seatIndex} camera={(camera != null ? camera.name : "NULL")} enemyCanvas={(enemyCanvas != null ? enemyCanvas.name : "NULL")} ownCollider={(ownCollider != null ? ownCollider.name : "NULL")} dealerTokenText={(dealerTokenText != null ? dealerTokenText.name : "NULL")}");

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

        GameLog.Write(
            "Player",
            $"OnStartLocalPlayer local setup applied player={netId} camera on, enemyCanvas off, ownCollider off");

        dealerTokenText.text = "NEXT";

        GameLog.Write(
            "Player",
            $"OnStartLocalPlayer sending CmdNotifyLoaded player={netId}");

        CmdNotifyLoaded();
    }

    // =========================================================
    // TURN TOKEN
    // =========================================================

    private void OnCurrentPlayerChanged(
        int currentPlayerIndex)
    {
        GameLog.Write(
            "Player",
            $"OnCurrentPlayerChanged player={netId} seat={seatIndex} currentPlayer={currentPlayerIndex}");

        ApplyDealerToken(
            currentPlayerIndex);
    }

    private void ApplyDealerToken(
        int currentPlayerIndex)
    {
        GameLog.Write(
            "Player",
            $"ApplyDealerToken player={netId} seat={seatIndex} currentPlayer={currentPlayerIndex}");

        if (dealerToken == null)
        {
            GameLog.Write(
                "Player",
                $"ApplyDealerToken skipped: dealerToken is NULL player={netId}");

            return;
        }

        dealerToken.SetActive(
            seatIndex >= 0 &&
            seatIndex == currentPlayerIndex);

        GameLog.Write(
            "Player",
            $"ApplyDealerToken done player={netId} tokenActive={(seatIndex >= 0 && seatIndex == currentPlayerIndex)}");
    }

    // =========================================================
    // LOADING
    // =========================================================

    [Command]
    private void CmdNotifyLoaded()
    {
        GameLog.Write(
            "Player",
            $"CmdNotifyLoaded player={netId} connection={(connectionToClient != null ? connectionToClient.connectionId : -1)} gameMaster={(GameMaster.Instance != null ? "OK" : "NULL")}");

        if (GameMaster.Instance == null)
        {
            Debug.LogError(
                "Player: GameMaster instance is missing.");

            GameLog.Write(
                "Player",
                $"CmdNotifyLoaded failed: GameMaster instance is missing player={netId}");

            return;
        }

        if (connectionToClient == null)
        {
            Debug.LogError(
                "Player: connectionToClient is null.");

            GameLog.Write(
                "Player",
                $"CmdNotifyLoaded failed: connectionToClient is null player={netId}");

            return;
        }

        GameLog.Write(
            "Player",
            $"CmdNotifyLoaded forwarding to GameMaster.PlayerLoaded player={netId} connection={connectionToClient.connectionId}");

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
        GameLog.Write(
            "Player",
            $"SetSteamData player={netId} steamId={steamId} nickname={nickname}");

        this.steamId = steamId;
        steamNickname = nickname;

        ApplyNickname();
    }

    private void OnSteamNicknameChanged(
        string oldValue,
        string newValue)
    {
        GameLog.Write(
            "Player",
            $"OnSteamNicknameChanged player={netId} old={oldValue} new={newValue}");

        ApplyNickname();
    }

    private void ApplyNickname()
    {
        GameLog.Write(
            "Player",
            $"ApplyNickname player={netId} nickname={steamNickname} nicknameText={(nicknameText != null ? nicknameText.name : "NULL")}");

        if (nicknameText == null)
        {
            GameLog.Write(
                "Player",
                $"ApplyNickname skipped: nicknameText is NULL player={netId}");

            return;
        }

        nicknameText.text =
            steamNickname ?? string.Empty;

        GameLog.Write(
            "Player",
            $"ApplyNickname done player={netId} text={nicknameText.text}");
    }

    // =========================================================
    // SEAT
    // =========================================================

    [Server]
    public void SetSeat(
        int index)
    {
        GameLog.Write(
            "Player",
            $"SetSeat player={netId} oldSeat={seatIndex} newSeat={index}");

        seatIndex = index;

        GameLog.Write(
            "Player",
            $"SetSeat seat set player={netId} seat={seatIndex} gameMaster={(GameMaster.Instance != null ? "OK" : "NULL")}");

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
        GameLog.Write(
            "Player",
            $"SetAccesory player={netId} index={index} value={value} mask={accessoryMask} count={(accesories != null ? accesories.Count : -1)}");

        if (index < 0 ||
            index >= accesories.Count)
        {
            Debug.LogWarning(
                $"Invalid accessory index {index}. " +
                $"Count: {accesories.Count}",
                this);

            GameLog.Write(
                "Player",
                $"SetAccesory failed: invalid index={index} count={accesories.Count} player={netId}");

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

        GameLog.Write(
            "Player",
            $"SetAccesory mask updated player={netId} index={index} value={value} mask={accessoryMask}");

        ApplyAccessory(
            index,
            value);

        RpcPlayAccessoryVfx(
            index,
            value);

        GameLog.Write(
            "Player",
            $"SetAccesory done player={netId} index={index} value={value} rpcSent=true");

        return true;
    }

    private void OnAccessoryMaskChanged(
        int oldMask,
        int newMask)
    {
        GameLog.Write(
            "Player",
            $"OnAccessoryMaskChanged player={netId} oldMask={oldMask} newMask={newMask}");

        ApplyAccessoryMask(
            newMask);
    }

    private void ApplyAccessoryMask(
        int mask)
    {
        GameLog.Write(
            "Player",
            $"ApplyAccessoryMask player={netId} mask={mask} count={(accesories != null ? accesories.Count : -1)}");

        if (accesories == null)
        {
            GameLog.Write(
                "Player",
                $"ApplyAccessoryMask skipped: accesories list is NULL player={netId}");

            return;
        }

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
        GameLog.Write(
            "Player",
            $"ApplyAccessory player={netId} index={index} value={value}");

        if (index < 0 ||
            index >= accesories.Count)
        {
            GameLog.Write(
                "Player",
                $"ApplyAccessory skipped: invalid index={index} count={accesories.Count} player={netId}");

            return;
        }

        GameObject accessory =
            accesories[index];

        if (accessory == null)
        {
            GameLog.Write(
                "Player",
                $"ApplyAccessory skipped: accessory is NULL index={index} player={netId}");

            return;
        }

        GameLog.Write(
            "Player",
            $"ApplyAccessory done player={netId} index={index} value={value} accessory={accessory.name}");
    }

    // =========================================================
    // ACCESSORY QUERY
    // =========================================================

    public bool HasAccessory(
        int index)
    {
        GameLog.Write(
            "Player",
            $"HasAccessory player={netId} index={index} mask={accessoryMask}");

        if (index < 0 ||
            index >= accesories.Count)
        {
            GameLog.Write(
                "Player",
                $"HasAccessory false: invalid index={index} count={accesories.Count} player={netId}");

            return false;
        }

        GameLog.Write(
            "Player",
            $"HasAccessory result player={netId} index={index} has={((accessoryMask & (1 << index)) != 0)}");

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
        GameLog.Write(
            "Player",
            $"RpcPlayAccessoryVfx player={netId} index={index} value={value} vfxCount={(accesoryVfxes != null ? accesoryVfxes.Count : -1)}");

        if (index < 0)
        {
            GameLog.Write(
                "Player",
                $"RpcPlayAccessoryVfx skipped: negative index={index} player={netId}");

            return;
        }

        if (value)
        {
            if (index >= accesoryVfxes.Count)
            {
                GameLog.Write(
                    "Player",
                    $"RpcPlayAccessoryVfx skipped: index={index} out of range count={accesoryVfxes.Count} player={netId}");

                return;
            }

            if (accesoryVfxes[index] == null)
            {
                GameLog.Write(
                    "Player",
                    $"RpcPlayAccessoryVfx skipped: vfx is NULL index={index} player={netId}");

                return;
            }

            GameLog.Write(
                "Player",
                $"RpcPlayAccessoryVfx PlayAssemble player={netId} index={index}");

            accesoryVfxes[index].PlayAssemble();
        }
        else
        {

            if (index >= accesoryVfxes.Count)
            {
                GameLog.Write(
                    "Player",
                    $"RpcPlayAccessoryVfx skipped: index={index} out of range count={accesoryVfxes.Count} player={netId}");

                return;
            }

            if (accesoryVfxes[index] == null)
            {
                GameLog.Write(
                    "Player",
                    $"RpcPlayAccessoryVfx skipped: vfx is NULL index={index} player={netId}");

                return;
            }

            GameLog.Write(
                "Player",
                $"RpcPlayAccessoryVfx PlayDissolve player={netId} index={index}");

            accesoryVfxes[index].PlayDissolve();
        }
    }

    public void StartDamagePass()
    {
        GameLog.Write(
            "Player",
            $"StartDamagePass player={netId} damagePass={(DamagePass != null ? DamagePass.name : "NULL")} alreadyRunning={damagePassCoroutine != null} duration={damageDuration}");

        if (DamagePass == null)
        {
            GameLog.Write(
                "Player",
                $"StartDamagePass skipped: DamagePass is NULL player={netId}");

            return;
        }

        if (damagePassCoroutine != null)
            StopCoroutine(damagePassCoroutine);

        damagePassCoroutine =
            StartCoroutine(DamagePassRoutine());
    }

    private IEnumerator DamagePassRoutine()
    {
        GameLog.Write(
            "Player",
            $"DamagePassRoutine start player={netId} duration={damageDuration}");

        DamagePass.gameObject.SetActive(true);

        yield return new WaitForSeconds(damageDuration);

        DamagePass.gameObject.SetActive(false);

        damagePassCoroutine = null;

        GameLog.Write(
            "Player",
            $"DamagePassRoutine end player={netId}");
    }

    // =========================================================
    // TURN
    // =========================================================

    public void OnTurnStart()
    {
        GameLog.Write(
            "Player",
            $"OnTurnStart player={netId} seat={seatIndex} isServer={isServer} gameMaster={(GameMaster.Instance != null ? "OK" : "NULL")}");

        if (!isServer)
        {
            GameLog.Write(
                "Player",
                $"OnTurnStart skipped: not server player={netId}");

            return;
        }

        if (GameMaster.Instance == null)
        {
            GameLog.Write(
                "Player",
                $"OnTurnStart skipped: GameMaster is NULL player={netId}");

            return;
        }

        ApplyDealerToken(
            GameMaster.Instance.CurrentPlayerIndex);
    }

    public void OnTurnEnd()
    {
        GameLog.Write(
            "Player",
            $"OnTurnEnd player={netId} seat={seatIndex} isServer={isServer}");

        if (!isServer)
        {
            GameLog.Write(
                "Player",
                $"OnTurnEnd skipped: not server player={netId}");

            return;
        }

        if (dealerToken != null)
            dealerToken.SetActive(false);

        GameLog.Write(
            "Player",
            $"OnTurnEnd done player={netId} dealerToken={(dealerToken != null ? dealerToken.name : "NULL")} set inactive");
    }

    // =========================================================
    // CARDS
    // =========================================================

    [Server]
    public void SummonCards()
    {
        GameLog.Write(
            "Player",
            $"SummonCards start player={netId} sockets={(cardSockets != null ? cardSockets.Count : -1)} cardPrefab={(cardPrefab != null ? cardPrefab.name : "NULL")}");

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

                GameLog.Write(
                    "Player",
                    $"SummonCards skipped socket={i}: spawned object has no Card component obj={obj.name} player={netId}");

                continue;
            }

            if (DebugDeal.IsColtPlayer(this))
            {
                card.SetData(
                    DebugDeal.ColtIndex);
            }
            else
            {
                card.RandomData(
                    DebugDeal.ExcludedIndex);
            }

            card.owner = this;

            GameLog.Write(
                "Player",
                $"SummonCards player={netId} socket={i}/{cardSockets.Count} card={obj.name}");

            card.SetOwner(
                netId,
                i);
        }

        GameLog.Write(
            "Player",
            $"SummonCards done player={netId}");
    }
}