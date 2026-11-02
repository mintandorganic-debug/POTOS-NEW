using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Card : InteractableObject
{
    public Player owner;

    public enum UnavailableReason
    {
        SampleReason,
        WrongTurnPhase,
        Effect
    }

    public enum CardClass
    {
        AttackLight,
        AttackSteal,
        Defence,
        Util,
        Sin
    }

    [Header("Setup")]
    [SerializeField] private List<CardData> avalaibleCardTypes;

    [Header("Data")]
    [SerializeField] private CardData cardData;

    [SyncVar(hook = nameof(OnCardDataChanged))]
    private int cardDataIndex = -1;

    [Header("Components")]
    [SerializeField] private CardVisual visual;
    [SerializeField] private CardMovement movement;
    [SerializeField] private CardInteraction interaction;

    [Header("State")]
    [SyncVar(hook = nameof(OnAvailabilityChanged))]
    [SerializeField] private bool isAvailable = true;

    [SyncVar(hook = nameof(OnRouletteStateChanged))]
    [SerializeField] private bool isInRoulette;

    [SyncVar]
    [SerializeField]
    private UnavailableReason unavailableReason =
        UnavailableReason.SampleReason;

    [SyncVar(hook = nameof(OnOwnerChanged))]
    private uint ownerNetId;

    [SyncVar(hook = nameof(OnSocketChanged))]
    private int socketIndex;

    public override float LookZoomFov =>
        interaction != null
            ? interaction.LookZoomFov
            : -1f;

    public CardData Data =>
        cardData;

    public bool IsAvailable =>
        isAvailable;

    public UnavailableReason Reason =>
        unavailableReason;

    public bool IsInRoulette =>
        isInRoulette;

    public bool IsZoomed =>
        interaction != null &&
        interaction.IsZoomed;

    public uint OwnerNetId =>
        ownerNetId;

    private void OnEnable()
    {
        GameLog.Write(
            "Card",
            $"OnEnable netId={netId} data={(cardData != null ? cardData.name : "NULL")}");

        SubscribeToGamePhase();
    }

    private void OnDisable()
    {
        GameLog.Write(
            "Card",
            $"OnDisable netId={netId} data={(cardData != null ? cardData.name : "NULL")}");

        ResetLocalState();
        UnsubscribeFromGamePhase();
    }

    private void Start()
    {
        GameLog.Write(
            "Card",
            $"Start netId={netId} data={(cardData != null ? cardData.name : "NULL")} owner={ownerNetId} socket={socketIndex} available={isAvailable} inRoulette={isInRoulette}");

        ResetLocalState();
        ApplyVisualState();

        if (GameMaster.Instance != null)
        {
            OnGamePhaseChanged(
                GameMaster.Instance.CurrentPhase);
        }
        else
        {
            GameLog.Write(
                "Card",
                $"Start netId={netId} GameMaster is NULL, phase not applied");
        }
    }

    [ServerCallback]
    private void Update()
    {
        movement?.UpdateMovement();
    }

    [Server]
    public void RefreshAvailability()
    {
        GameLog.Write(
            "Card",
            $"RefreshAvailability netId={netId} inRoulette={isInRoulette} available={isAvailable}");

        if (isInRoulette)
        {
            GameLog.Write(
                "Card",
                $"RefreshAvailability skipped netId={netId}: in roulette");

            return;
        }

        if (GameMaster.Instance == null)
        {
            GameLog.Write(
                "Card",
                $"RefreshAvailability skipped netId={netId}: GameMaster is NULL");

            return;
        }

        OnGamePhaseChanged(
            GameMaster.Instance.CurrentPhase);
    }

    private void SubscribeToGamePhase()
    {
        if (GameMaster.Instance == null)
        {
            GameLog.Write(
                "Card",
                $"SubscribeToGamePhase skipped netId={netId}: GameMaster is NULL");

            return;
        }

        GameLog.Write(
            "Card",
            $"SubscribeToGamePhase netId={netId}");

        GameMaster.Instance.GamePhaseChanged -=
            OnGamePhaseChanged;

        GameMaster.Instance.GamePhaseChanged +=
            OnGamePhaseChanged;
    }

    private void UnsubscribeFromGamePhase()
    {
        if (GameMaster.Instance == null)
        {
            GameLog.Write(
                "Card",
                $"UnsubscribeFromGamePhase skipped netId={netId}: GameMaster is NULL");

            return;
        }

        GameLog.Write(
            "Card",
            $"UnsubscribeFromGamePhase netId={netId}");

        GameMaster.Instance.GamePhaseChanged -=
            OnGamePhaseChanged;
    }

    private void OnGamePhaseChanged(
        GameMaster.GamePhase phase)
    {
        GameLog.Write(
            "Card",
            $"OnGamePhaseChanged netId={netId} phase={phase} isServer={isServer} inRoulette={isInRoulette}");

        if (!isServer)
        {
            GameLog.Write(
                "Card",
                $"OnGamePhaseChanged skipped netId={netId}: not server");

            return;
        }

        if (isInRoulette)
        {
            GameLog.Write(
                "Card",
                $"OnGamePhaseChanged skipped netId={netId}: in roulette");

            return;
        }

        bool shouldBeAvailable;

        if (phase == GameMaster.GamePhase.Defense)
        {
            shouldBeAvailable =
                cardData != null &&
                cardData.CardClass == CardClass.Defence;
        }
        else
        {
            shouldBeAvailable =
                cardData != null &&
                cardData.CardClass != CardClass.Defence;
        }

        GameLog.Write(
            "Card",
            $"OnGamePhaseChanged netId={netId} phase={phase} class={(cardData != null ? cardData.CardClass.ToString() : "NULL")} shouldBeAvailable={shouldBeAvailable}");

        SetAvailable(
            shouldBeAvailable,
            shouldBeAvailable
                ? UnavailableReason.SampleReason
                : UnavailableReason.WrongTurnPhase);
    }

    private bool IsOwnedByLocalPlayer()
    {
        if (ownerNetId == 0)
        {
            GameLog.Write(
                "Card",
                $"IsOwnedByLocalPlayer=false netId={netId}: ownerNetId is 0");

            return false;
        }

        if (!NetworkClient.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity))
        {
            GameLog.Write(
                "Card",
                $"IsOwnedByLocalPlayer=false netId={netId}: owner {ownerNetId} not spawned on client");

            return false;
        }

        GameLog.Write(
            "Card",
            $"IsOwnedByLocalPlayer netId={netId} owner={ownerNetId} isOwned={identity.isOwned}");

        return identity.isOwned;
    }

    [Server]
    public bool IsOwnedByLocalConnection(
        NetworkConnectionToClient connection)
    {
        GameLog.Write(
            "Card",
            $"IsOwnedByLocalConnection netId={netId} connection={(connection != null ? connection.connectionId : -1)} owner={ownerNetId}");

        if (connection == null ||
            ownerNetId == 0)
        {
            GameLog.Write(
                "Card",
                $"IsOwnedByLocalConnection=false netId={netId}: connection is NULL or ownerNetId is 0");

            return false;
        }

        if (!NetworkServer.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity ownerIdentity))
        {
            GameLog.Write(
                "Card",
                $"IsOwnedByLocalConnection=false netId={netId}: owner {ownerNetId} not spawned on server");

            return false;
        }

        GameLog.Write(
            "Card",
            $"IsOwnedByLocalConnection netId={netId} match={ownerIdentity.connectionToClient == connection}");

        return ownerIdentity.connectionToClient ==
               connection;
    }

    [Server]
    public void RandomData()
    {
        GameLog.Write(
            "Card",
            $"RandomData begin netId={netId} types={(avalaibleCardTypes == null ? -1 : avalaibleCardTypes.Count)}");

        if (avalaibleCardTypes == null ||
            avalaibleCardTypes.Count == 0)
        {
            GameLog.Write(
                "Card",
                $"RandomData failed netId={netId}: avalaibleCardTypes is empty");

            Debug.LogError(
                "Card: avalaibleCardTypes is empty.",
                this);

            return;
        }

        int index =
            Random.Range(
                0,
                avalaibleCardTypes.Count);

        if (avalaibleCardTypes[index] == null)
        {
            GameLog.Write(
                "Card",
                $"RandomData failed netId={netId}: CardData at index {index} is null");

            Debug.LogError(
                $"Card: CardData at index {index} is null.",
                this);

            return;
        }

        cardDataIndex = index;

        GameLog.Write(
            "Card",
            $"RandomData netId={netId} index={index}/{avalaibleCardTypes.Count} data={avalaibleCardTypes[index].name}");

        ApplyCardData();

        if (GameMaster.Instance != null)
        {
            OnGamePhaseChanged(
                GameMaster.Instance.CurrentPhase);
        }
        else
        {
            GameLog.Write(
                "Card",
                $"RandomData netId={netId}: GameMaster is NULL, phase not applied");
        }
    }

    private void OnCardDataChanged(
        int oldValue,
        int newValue)
    {
        GameLog.Write(
            "Card",
            $"OnCardDataChanged netId={netId} {oldValue}->{newValue}");

        ApplyCardData();

        if (isServer &&
            GameMaster.Instance != null)
        {
            OnGamePhaseChanged(
                GameMaster.Instance.CurrentPhase);
        }
    }

    private void ApplyCardData()
    {
        if (avalaibleCardTypes == null ||
            cardDataIndex < 0 ||
            cardDataIndex >= avalaibleCardTypes.Count)
        {
            GameLog.Write(
                "Card",
                $"ApplyCardData skipped netId={netId} index={cardDataIndex} types={(avalaibleCardTypes == null ? -1 : avalaibleCardTypes.Count)}");

            return;
        }

        cardData =
            avalaibleCardTypes[cardDataIndex];

        if (cardData == null)
        {
            GameLog.Write(
                "Card",
                $"ApplyCardData skipped netId={netId} index={cardDataIndex}: CardData is NULL");

            return;
        }

        GameLog.Write(
            "Card",
            $"ApplyCardData netId={netId} index={cardDataIndex} data={cardData.name} texture={(cardData.texture == null ? "NULL" : cardData.texture.name)}");

        visual?.ApplyCardData(cardData);
    }

    [Server]
    public void SetAvailable(
        bool value,
        UnavailableReason reason =
            UnavailableReason.SampleReason)
    {
        GameLog.Write(
            "Card",
            $"SetAvailable netId={netId} {isAvailable}->{value} reason={reason} (previous reason={unavailableReason})");

        isAvailable = value;

        unavailableReason =
            value
                ? UnavailableReason.SampleReason
                : reason;
    }

    [Server]
    public void SetInRoulette(bool value)
    {
        GameLog.Write(
            "Card",
            $"SetInRoulette netId={netId} {isInRoulette}->{value}");

        isInRoulette = value;

        if (value)
        {
            movement?.Detach();
            ResetLocalState();

            visual?.HideUnavailableOverlay();
            visual?.SetColliderEnabled(false);

            return;
        }

        ApplyVisualState();

        if (GameMaster.Instance != null)
        {
            OnGamePhaseChanged(
                GameMaster.Instance.CurrentPhase);
        }
    }

    private void OnRouletteStateChanged(
        bool oldValue,
        bool newValue)
    {
        GameLog.Write(
            "Card",
            $"OnRouletteStateChanged netId={netId} {oldValue}->{newValue}");

        if (newValue)
        {
            movement?.Detach();
            ResetLocalState();

            visual?.HideUnavailableOverlay();
            visual?.SetColliderEnabled(false);

            return;
        }

        ApplyVisualState();
    }

    [Server]
    public void SetOwner(
        uint ownerNetId,
        int socketIndex)
    {
        GameLog.Write(
            "Card",
            $"SetOwner netId={netId} owner {this.ownerNetId}->{ownerNetId} socket {this.socketIndex}->{socketIndex}");

        this.ownerNetId = ownerNetId;
        this.socketIndex = socketIndex;

        AttachToSocket();
    }

    public override void OnStartClient()
    {
        GameLog.Write(
            "Card",
            $"OnStartClient netId={netId} owner={ownerNetId} socket={socketIndex} available={isAvailable} inRoulette={isInRoulette}");

        base.OnStartClient();

        SubscribeToGamePhase();

        AttachToSocket();
        ApplyCardData();
        ResetLocalState();
        ApplyVisualState();

        if (isInRoulette)
        {
            GameLog.Write(
                "Card",
                $"OnStartClient netId={netId}: in roulette, applying roulette state");

            OnRouletteStateChanged(
                false,
                true);

            return;
        }

        if (IsOwnedByLocalPlayer())
        {
            GameLog.Write(
                "Card",
                $"OnStartClient netId={netId}: owned by local player, initializing interaction");

            InitializeInteraction();

            if (GameMaster.Instance != null)
            {
                OnGamePhaseChanged(
                    GameMaster.Instance.CurrentPhase);
            }
        }
        else
        {
            GameLog.Write(
                "Card",
                $"OnStartClient netId={netId}: not owned by local player, disabling local interaction");

            DisableLocalInteraction();
        }
    }

    private void OnAvailabilityChanged(
        bool oldValue,
        bool newValue)
    {
        GameLog.Write(
            "Card",
            $"OnAvailabilityChanged netId={netId} {oldValue}->{newValue} reason={unavailableReason}");

        if (!newValue)
        {
            interaction?.ResetState();
            movement?.ResetState();
        }

        ApplyVisualState();
    }

    private void OnOwnerChanged(
        uint oldValue,
        uint newValue)
    {
        GameLog.Write(
            "Card",
            $"OnOwnerChanged netId={netId} {oldValue}->{newValue}");

        ResetLocalState();

        AttachToSocket();
        ApplyVisualState();

        if (IsOwnedByLocalPlayer())
        {
            GameLog.Write(
                "Card",
                $"OnOwnerChanged netId={netId}: owned by local player, initializing interaction");

            InitializeInteraction();

            if (isInRoulette)
            {
                GameLog.Write(
                    "Card",
                    $"OnOwnerChanged netId={netId}: in roulette, applying roulette state");

                OnRouletteStateChanged(
                    false,
                    true);

                return;
            }

            if (GameMaster.Instance != null)
            {
                OnGamePhaseChanged(
                    GameMaster.Instance.CurrentPhase);
            }
        }
        else
        {
            GameLog.Write(
                "Card",
                $"OnOwnerChanged netId={netId}: not owned by local player, disabling local interaction");

            DisableLocalInteraction();
        }
    }

    private void OnSocketChanged(
        int oldValue,
        int newValue)
    {
        GameLog.Write(
            "Card",
            $"OnSocketChanged netId={netId} {oldValue}->{newValue}");

        AttachToSocket();
    }

    private void InitializeInteraction()
    {
        GameLog.Write(
            "Card",
            $"InitializeInteraction netId={netId} interaction={(interaction != null ? "ok" : "NULL")} visual={(visual != null ? "ok" : "NULL")} movement={(movement != null ? "ok" : "NULL")}");

        interaction?.Initialize(
            this,
            visual,
            movement);
    }

    private void AttachToSocket()
    {
        GameLog.Write(
            "Card",
            $"AttachToSocket netId={netId} owner={ownerNetId} socket={socketIndex}");

        if (ownerNetId == 0)
        {
            GameLog.Write(
                "Card",
                $"AttachToSocket skipped netId={netId}: ownerNetId is 0");

            return;
        }

        if (!NetworkClient.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity))
        {
            GameLog.Write(
                "Card",
                $"AttachToSocket skipped netId={netId}: owner {ownerNetId} not spawned on client");

            return;
        }

        Player player =
            identity.GetComponent<Player>();

        if (player == null)
        {
            GameLog.Write(
                "Card",
                $"AttachToSocket skipped netId={netId}: owner {ownerNetId} has no Player");

            return;
        }

        if (socketIndex < 0 ||
            socketIndex >= player.cardSockets.Count)
        {
            GameLog.Write(
                "Card",
                $"AttachToSocket skipped netId={netId}: socketIndex {socketIndex} out of range (count={player.cardSockets.Count})");

            return;
        }

        Transform socket =
            player.cardSockets[socketIndex].transform;

        GameLog.Write(
            "Card",
            $"AttachToSocket netId={netId} attaching to socket={(socket != null ? socket.name : "NULL")} of player={player.name}");

        movement?.AttachToSocket(socket);

        if (isInRoulette)
        {
            GameLog.Write(
                "Card",
                $"AttachToSocket netId={netId}: in roulette, interaction disabled");

            visual?.DisableInteraction();
            visual?.HideUnavailableOverlay();
            return;
        }

        visual?.SetColliderEnabled(
            IsOwnedByLocalPlayer());
    }

    private void DisableLocalInteraction()
    {
        GameLog.Write(
            "Card",
            $"DisableLocalInteraction netId={netId}");

        interaction?.ResetState();
        movement?.ResetState();

        visual?.SetColliderEnabled(false);
        visual?.HideUnavailableOverlay();
    }

    private void ResetLocalState()
    {
        GameLog.Write(
            "Card",
            $"ResetLocalState netId={netId}");

        interaction?.ResetState();
        movement?.ResetState();
        visual?.ResetState();
    }

    private void ApplyVisualState()
    {
        if (visual == null)
        {
            GameLog.Write(
                "Card",
                $"ApplyVisualState skipped netId={netId}: visual is NULL");

            return;
        }

        if (isInRoulette)
        {
            GameLog.Write(
                "Card",
                $"ApplyVisualState netId={netId}: in roulette, overlay hidden, collider off");

            visual.HideUnavailableOverlay();
            visual.SetColliderEnabled(false);
            return;
        }

        if (!IsOwnedByLocalPlayer())
        {
            GameLog.Write(
                "Card",
                $"ApplyVisualState netId={netId}: not owned by local player, overlay hidden, collider off");

            visual.HideUnavailableOverlay();
            visual.SetColliderEnabled(false);
            return;
        }

        GameLog.Write(
            "Card",
            $"ApplyVisualState netId={netId}: available={isAvailable}, collider on");

        visual.ApplyAvailability(
            isAvailable);

        visual.SetColliderEnabled(true);
    }

    public void OnAttachmentToRoulette()
    {
        GameLog.Write(
            "Card",
            $"OnAttachmentToRoulette netId={netId}");

        movement?.Detach();
        ResetLocalState();

        visual?.HideUnavailableOverlay();
        visual?.SetColliderEnabled(false);
    }

    public void RequestMoveToZoomSocket()
    {
        GameLog.Write(
            "Card",
            $"RequestMoveToZoomSocket netId={netId}");

        if (!IsOwnedByLocalPlayer())
        {
            GameLog.Write(
                "Card",
                $"RequestMoveToZoomSocket skipped netId={netId}: not owned by local player");

            return;
        }

        CmdMoveToZoomSocket();
    }

    [Command]
    private void CmdMoveToZoomSocket(
        NetworkConnectionToClient sender = null)
    {
        GameLog.Write(
            "Card",
            $"CmdMoveToZoomSocket netId={netId} sender={(sender != null ? sender.connectionId : -1)}");

        if (!IsOwnedByLocalConnection(sender))
        {
            GameLog.Write(
                "Card",
                $"CmdMoveToZoomSocket rejected netId={netId}: sender does not own card");

            return;
        }

        if (isInRoulette)
        {
            GameLog.Write(
                "Card",
                $"CmdMoveToZoomSocket rejected netId={netId}: in roulette");

            return;
        }

        if (!NetworkServer.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity))
        {
            GameLog.Write(
                "Card",
                $"CmdMoveToZoomSocket rejected netId={netId}: owner {ownerNetId} not spawned on server");

            return;
        }

        Player player =
            identity.GetComponent<Player>();

        if (player == null)
        {
            GameLog.Write(
                "Card",
                $"CmdMoveToZoomSocket rejected netId={netId}: owner {ownerNetId} has no Player");

            return;
        }

        if (player.cardZoomSocket == null)
        {
            GameLog.Write(
                "Card",
                $"CmdMoveToZoomSocket rejected netId={netId}: cardZoomSocket is NULL");

            return;
        }

        GameLog.Write(
            "Card",
            $"CmdMoveToZoomSocket netId={netId} moving to zoom socket of player={player.name}");

        movement?.MoveToZoomSocket(
            player.cardZoomSocket);
    }

    public void ReturnToOwner()
    {
        GameLog.Write(
            "Card",
            $"ReturnToOwner netId={netId} owner={ownerNetId} socket={socketIndex}");

        if (ownerNetId == 0)
        {
            GameLog.Write(
                "Card",
                $"ReturnToOwner skipped netId={netId}: ownerNetId is 0");

            return;
        }

        if (!NetworkClient.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity))
        {
            GameLog.Write(
                "Card",
                $"ReturnToOwner skipped netId={netId}: owner {ownerNetId} not spawned on client");

            return;
        }

        Player player =
            identity.GetComponent<Player>();

        if (player == null)
        {
            GameLog.Write(
                "Card",
                $"ReturnToOwner skipped netId={netId}: owner {ownerNetId} has no Player");

            return;
        }

        if (socketIndex < 0 ||
            socketIndex >= player.cardSockets.Count)
        {
            GameLog.Write(
                "Card",
                $"ReturnToOwner skipped netId={netId}: socketIndex {socketIndex} out of range (count={player.cardSockets.Count})");

            return;
        }

        Transform socket =
            player.cardSockets[socketIndex].transform;

        GameLog.Write(
            "Card",
            $"ReturnToOwner netId={netId} moving to socket={(socket != null ? socket.name : "NULL")} of player={player.name}");

        interaction?.ResetState();
        visual?.DisableHighlight();

        movement?.MoveTo(
            socket,
            movement.ReturnMoveSpeed);
    }

    public override void OnLookEnter()
    {
        GameLog.Write(
            "Card",
            $"OnLookEnter netId={netId} data={(cardData != null ? cardData.name : "NULL")}");

        if (!CanInteract())
        {
            GameLog.Write(
                "Card",
                $"OnLookEnter ignored netId={netId}: cannot interact");

            return;
        }

        interaction?.OnLookEnter();
    }

    public override void OnLookExit()
    {
        GameLog.Write(
            "Card",
            $"OnLookExit netId={netId} data={(cardData != null ? cardData.name : "NULL")}");

        if (!IsOwnedByLocalPlayer())
        {
            GameLog.Write(
                "Card",
                $"OnLookExit ignored netId={netId}: not owned by local player");

            return;
        }

        interaction?.OnLookExit();
    }

    public override void Interact()
    {
        GameLog.Write(
            "Card",
            $"Interact netId={netId} data={(cardData != null ? cardData.name : "NULL")} zoomed={IsZoomed}");

        if (!CanInteract())
        {
            GameLog.Write(
                "Card",
                $"Interact ignored netId={netId}: cannot interact");

            return;
        }

        interaction?.Interact();
    }

    private bool CanInteract()
    {
        if (!IsOwnedByLocalPlayer() ||
            !isAvailable ||
            isInRoulette)
        {
            GameLog.Write(
                "Card",
                $"CanInteract=false netId={netId}: available={isAvailable} inRoulette={isInRoulette} (or not owned by local player)");

            return false;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "Card",
                $"CanInteract=false netId={netId}: GameMaster is NULL");

            return false;
        }

        if (!TryGetOwner(
                out Player player))
        {
            GameLog.Write(
                "Card",
                $"CanInteract=false netId={netId}: owner player not found");

            return false;
        }

        switch (gameMaster.CurrentPhase)
        {
            case GameMaster.GamePhase.Attack:
                GameLog.Write(
                    "Card",
                    $"CanInteract netId={netId} phase=Attack seat={player.SeatIndex} currentPlayer={gameMaster.CurrentPlayerIndex} result={player.SeatIndex == gameMaster.CurrentPlayerIndex}");

                return player.SeatIndex ==
                       gameMaster.CurrentPlayerIndex;

            case GameMaster.GamePhase.Defense:
                GameLog.Write(
                    "Card",
                    $"CanInteract netId={netId} phase=Defense target={(gameMaster.SelectedTarget != null ? gameMaster.SelectedTarget.name : "NULL")} owner={player.name} result={gameMaster.SelectedTarget == player}");

                return gameMaster.SelectedTarget ==
                       player;

            default:
                GameLog.Write(
                    "Card",
                    $"CanInteract=false netId={netId}: phase={gameMaster.CurrentPhase} does not allow card interaction");

                return false;
        }
    }

    public bool TryGetOwner(
        out Player player)
    {
        player = null;

        if (ownerNetId == 0)
        {
            GameLog.Write(
                "Card",
                $"TryGetOwner=false netId={netId}: ownerNetId is 0");

            return false;
        }

        if (!NetworkClient.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity))
        {
            GameLog.Write(
                "Card",
                $"TryGetOwner=false netId={netId}: owner {ownerNetId} not spawned on client");

            return false;
        }

        player =
            identity.GetComponent<Player>();

        GameLog.Write(
            "Card",
            $"TryGetOwner netId={netId} owner={ownerNetId} player={(player != null ? player.name : "NULL")}");

        return player != null;
    }
}
