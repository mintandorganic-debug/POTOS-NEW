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
        Sin,
        Poison,
        Luck,
        Craft
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

    public CardData Data =>
        cardData;

    public bool IsAvailable =>
        isAvailable;

    public UnavailableReason Reason =>
        unavailableReason;

    public bool IsInRoulette =>
        isInRoulette;

    public bool IsPlayable =>
        CanInteract();

    public uint OwnerNetId =>
        ownerNetId;

    private void OnEnable()
    {
        SubscribeToGamePhase();
    }

    private void OnDisable()
    {
        ResetLocalState();
        UnsubscribeFromGamePhase();
    }

    public override void OnStopServer()
    {
        if (NetworkServer.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity) &&
            identity.TryGetComponent(
                out PlayerHand hand))
        {
            hand.RemoveCard(this);
        }

        base.OnStopServer();
    }

    private void Start()
    {
        ResetLocalState();
        ApplyVisualState();

        if (GameMaster.Instance != null)
        {
            OnGamePhaseChanged(
                GameMaster.Instance.CurrentPhase);
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
        if (isInRoulette)
            return;

        if (GameMaster.Instance == null)
            return;

        OnGamePhaseChanged(
            GameMaster.Instance.CurrentPhase);
    }

    private void SubscribeToGamePhase()
    {
        if (GameMaster.Instance == null)
            return;

        GameMaster.Instance.GamePhaseChanged -=
            OnGamePhaseChanged;

        GameMaster.Instance.GamePhaseChanged +=
            OnGamePhaseChanged;
    }

    private void UnsubscribeFromGamePhase()
    {
        if (GameMaster.Instance == null)
            return;

        GameMaster.Instance.GamePhaseChanged -=
            OnGamePhaseChanged;
    }

    private void OnGamePhaseChanged(
        GameMaster.GamePhase phase)
    {
        if (!isServer)
            return;

        if (isInRoulette)
            return;

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

        SetAvailable(
            shouldBeAvailable,
            shouldBeAvailable
                ? UnavailableReason.SampleReason
                : UnavailableReason.WrongTurnPhase);
    }

    private bool IsOwnedByLocalPlayer()
    {
        if (ownerNetId == 0)
            return false;

        if (!NetworkClient.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity))
        {
            return false;
        }

        return identity.isOwned;
    }

    [Server]
    public bool IsOwnedByLocalConnection(
        NetworkConnectionToClient connection)
    {
        if (connection == null ||
            ownerNetId == 0)
        {
            return false;
        }

        if (!NetworkServer.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity ownerIdentity))
        {
            return false;
        }

        return ownerIdentity.connectionToClient ==
               connection;
    }

    [Server]
    public void RandomData()
    {
        if (avalaibleCardTypes == null ||
            avalaibleCardTypes.Count == 0)
        {
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
            Debug.LogError(
                $"Card: CardData at index {index} is null.",
                this);

            return;
        }

        cardDataIndex = index;

        ApplyCardData();

        if (GameMaster.Instance != null)
        {
            OnGamePhaseChanged(
                GameMaster.Instance.CurrentPhase);
        }
    }

    private void OnCardDataChanged(
        int oldValue,
        int newValue)
    {
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
            return;
        }

        cardData =
            avalaibleCardTypes[cardDataIndex];

        if (cardData == null)
            return;

        visual?.ApplyCardData(cardData);
    }

    [Server]
    public void SetAvailable(
        bool value,
        UnavailableReason reason =
            UnavailableReason.SampleReason)
    {
        isAvailable = value;

        unavailableReason =
            value
                ? UnavailableReason.SampleReason
                : reason;
    }

    [Server]
    public void SetInRoulette(bool value)
    {
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
        this.ownerNetId = ownerNetId;
        this.socketIndex = socketIndex;

        AttachToSocket();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        SubscribeToGamePhase();

        AttachToSocket();
        ApplyCardData();
        ResetLocalState();
        ApplyVisualState();

        if (isInRoulette)
        {
            OnRouletteStateChanged(
                false,
                true);

            return;
        }

        if (IsOwnedByLocalPlayer())
        {
            InitializeInteraction();

            if (GameMaster.Instance != null)
            {
                OnGamePhaseChanged(
                    GameMaster.Instance.CurrentPhase);
            }
        }
        else
        {
            DisableLocalInteraction();
        }
    }

    private void OnAvailabilityChanged(
        bool oldValue,
        bool newValue)
    {
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
        ResetLocalState();

        AttachToSocket();
        ApplyVisualState();

        if (IsOwnedByLocalPlayer())
        {
            InitializeInteraction();

            if (isInRoulette)
            {
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
            DisableLocalInteraction();
        }
    }

    private void OnSocketChanged(
        int oldValue,
        int newValue)
    {
        AttachToSocket();
    }

    private void InitializeInteraction()
    {
        interaction?.Initialize(
            this,
            visual,
            movement);
    }

    private void AttachToSocket()
    {
        if (ownerNetId == 0)
            return;

        if (!NetworkClient.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity))
        {
            return;
        }

        Player player =
            identity.GetComponent<Player>();

        if (player == null)
            return;

        if (socketIndex < 0 ||
            socketIndex >= player.cardSockets.Count)
        {
            return;
        }

        Transform socket =
            player.cardSockets[socketIndex].transform;

        movement?.AttachToSocket(socket);

        if (isInRoulette)
        {
            visual?.DisableInteraction();
            visual?.HideUnavailableOverlay();
            return;
        }

        visual?.SetColliderEnabled(
            IsOwnedByLocalPlayer());
    }

    private void DisableLocalInteraction()
    {
        interaction?.ResetState();
        movement?.ResetState();

        visual?.SetColliderEnabled(false);
        visual?.HideUnavailableOverlay();
    }

    private void ResetLocalState()
    {
        interaction?.ResetState();
        movement?.ResetState();
        visual?.ResetState();
    }

    private void ApplyVisualState()
    {
        if (visual == null)
            return;

        if (isInRoulette)
        {
            visual.HideUnavailableOverlay();
            visual.SetColliderEnabled(false);
            return;
        }

        if (!IsOwnedByLocalPlayer())
        {
            visual.HideUnavailableOverlay();
            visual.SetColliderEnabled(false);
            return;
        }

        visual.ApplyAvailability(
            isAvailable);

        visual.SetColliderEnabled(true);
    }

    public void OnAttachmentToRoulette()
    {
        movement?.Detach();
        ResetLocalState();

        visual?.HideUnavailableOverlay();
        visual?.SetColliderEnabled(false);
    }

    public void ReturnToOwner()
    {
        if (ownerNetId == 0)
            return;

        if (!NetworkClient.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity))
        {
            return;
        }

        Player player =
            identity.GetComponent<Player>();

        if (player == null)
            return;

        if (socketIndex < 0 ||
            socketIndex >= player.cardSockets.Count)
        {
            return;
        }

        Transform socket =
            player.cardSockets[socketIndex].transform;

        interaction?.ResetState();
        visual?.DisableHighlight();

        movement?.MoveTo(
            socket,
            movement.ReturnMoveSpeed);
    }

    public void SetHighlight(
        bool value)
    {
        if (isInRoulette)
            value = false;

        visual?.SetHover(value);
    }

    public override void Interact()
    {
        if (!CanInteract())
            return;

        interaction?.Interact();
    }

    private bool CanInteract()
    {
        if (!IsOwnedByLocalPlayer() ||
            !isAvailable ||
            isInRoulette)
        {
            return false;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
            return false;

        if (!TryGetOwner(
                out Player player))
        {
            return false;
        }

        switch (gameMaster.CurrentPhase)
        {
            case GameMaster.GamePhase.Attack:
                return player.SeatIndex ==
                       gameMaster.CurrentPlayerIndex;

            case GameMaster.GamePhase.Defense:
                return gameMaster.SelectedTarget ==
                       player;

            default:
                return false;
        }
    }

    public bool TryGetOwner(
        out Player player)
    {
        player = null;

        if (ownerNetId == 0)
            return false;

        if (!NetworkClient.spawned.TryGetValue(
                ownerNetId,
                out NetworkIdentity identity))
        {
            return false;
        }

        player =
            identity.GetComponent<Player>();

        return player != null;
    }
}