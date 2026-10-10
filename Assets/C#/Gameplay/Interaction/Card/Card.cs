using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Card : InteractableObject
{
    public Player owner;

    public enum UnavailableReason
    {
        SampleReason,
        WrongTurnPhase
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

    private void OnEnable()
    {
        SubscribeToGamePhase();
    }

    private void OnDisable()
    {
        ResetLocalState();
        UnsubscribeFromGamePhase();
    }

    private void Start()
    {
        ResetLocalState();

        if (GameMaster.Instance != null)
        {
            OnGamePhaseChanged(
                GameMaster.Instance.CurrentPhase);
        }
    }

    private void Update()
    {
        if (!IsOwnedByLocalPlayer())
            return;

        if (isInRoulette)
            return;

        movement?.UpdateMovement();
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
            ResetLocalState();
        }
        else if (GameMaster.Instance != null)
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
            ResetLocalState();
            return;
        }

        if (IsOwnedByLocalPlayer())
        {
            visual?.ApplyAvailability(isAvailable);
        }
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

        if (isInRoulette)
        {
            OnRouletteStateChanged(
                false,
                true);

            return;
        }

        visual?.ApplyAvailability(isAvailable);

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
        if (!IsOwnedByLocalPlayer())
        {
            DisableLocalInteraction();
            return;
        }

        visual?.ApplyAvailability(newValue);

        if (!newValue)
            ResetLocalState();
    }

    private void OnOwnerChanged(
        uint oldValue,
        uint newValue)
    {
        ResetLocalState();

        AttachToSocket();

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

            visual?.ApplyAvailability(isAvailable);

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
            return;
        }

        visual?.SetColliderEnabled(
            IsOwnedByLocalPlayer());
    }

    private void DisableLocalInteraction()
    {
        interaction?.ResetState();
        movement?.ResetState();
        visual?.DisableInteraction();
    }

    private void ResetLocalState()
    {
        interaction?.ResetState();
        movement?.ResetState();
        visual?.ResetState();
    }

    public void OnAttachmentToRoulette()
    {
        ResetLocalState();
    }

    public void OnActionPerformed()
    {
        if (!IsOwnedByLocalPlayer())
            return;

        if (cardData == null)
            return;

        if (cardData.CardClass ==
            CardClass.AttackSteal)
        {
            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.SetGamePhase(
                    GameMaster.GamePhase.Steal);
            }
        }
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

    public override void OnLookEnter()
    {
        if (!CanInteract())
            return;

        interaction?.OnLookEnter();
    }

    public override void OnLookExit()
    {
        if (!IsOwnedByLocalPlayer())
            return;

        interaction?.OnLookExit();
    }

    public override void Interact()
    {
        if (!CanInteract())
            return;

        interaction?.Interact();
    }

    private bool CanInteract()
    {
        return IsOwnedByLocalPlayer() &&
               isAvailable &&
               !isInRoulette;
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