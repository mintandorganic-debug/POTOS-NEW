using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameMaster : NetworkBehaviour
{
    public enum GamePhase
    {
        Wait,
        Attack,
        SelectTarget,
        Defense,
        Steal
    }

    [SerializeField] private SpawnpointCalculator spawnpointCalculator;
    [SerializeField] private float loadTimeout = 30f;

    public static GameMaster Instance;

    public event Action<GamePhase> GamePhaseChanged;
    public event Action<int> CurrentPlayerChanged;

    [SyncVar(hook = nameof(OnGamePhaseChanged))]
    [SerializeField] private GamePhase currentPhase = GamePhase.Wait;

    public GamePhase CurrentPhase =>
        currentPhase;

    [SyncVar(hook = nameof(OnCurrentPlayerChanged))]
    [SerializeField] private int currentPlayerIn = -1;

    public int CurrentPlayerIndex =>
        currentPlayerIn;

    public Player SelectedTarget =>
        selectedTarget;

    private readonly List<Player> _players = new();

    public IReadOnlyList<Player> Players =>
        _players;

    private Card selectedAttack;
    private Player selectedTarget;

    private bool gameStarted;

    private MatchStartup startup;
    private MatchSetup setup;

    private void Awake()
    {
        GameLog.Write(
            "GameMaster",
            $"Awake instanceExists={Instance != null} isDuplicate={Instance != null && Instance != this}");

        if (Instance != null &&
            Instance != this)
        {
            GameLog.Write(
                "GameMaster",
                "Awake duplicate instance, destroying gameObject");

            Destroy(gameObject);
            return;
        }

        Instance = this;

        GameLog.Write(
            "GameMaster",
            $"Awake Instance set loadTimeout={loadTimeout}");
    }

    private void SetGamePhase(
        GamePhase phase)
    {
        GameLog.Write(
            "GameMaster",
            $"SetGamePhase requested={phase} current={currentPhase} isServer={isServer}");

        if (!isServer)
        {
            GameLog.Write(
                "GameMaster",
                "SetGamePhase ignored: not server");

            return;
        }

        if (currentPhase == phase)
        {
            GameLog.Write(
                "GameMaster",
                $"SetGamePhase ignored: phase unchanged phase={phase}");

            return;
        }

        GameLog.Write(
            "GameMaster",
            $"SetGamePhase changing {currentPhase} -> {phase}");

        currentPhase = phase;
    }

    private void OnGamePhaseChanged(
        GamePhase oldPhase,
        GamePhase newPhase)
    {
        GameLog.Write(
            "GameMaster",
            $"OnGamePhaseChanged hook {oldPhase} -> {newPhase} isServer={isServer} isClient={isClient}");

        GamePhaseChanged?.Invoke(
            newPhase);
    }

    private void OnCurrentPlayerChanged(
        int oldPlayerIndex,
        int newPlayerIndex)
    {
        GameLog.Write(
            "GameMaster",
            $"OnCurrentPlayerChanged hook {oldPlayerIndex} -> {newPlayerIndex} isServer={isServer} isClient={isClient}");

        CurrentPlayerChanged?.Invoke(
            newPlayerIndex);
    }

    public override void OnStartServer()
    {
        GameLog.Write(
            "GameMaster",
            "OnStartServer");

        base.OnStartServer();

        _players.Clear();

        gameStarted = false;
        currentPlayerIn = -1;

        selectedAttack = null;
        selectedTarget = null;

        currentPhase = GamePhase.Wait;

        startup =
            new MatchStartup(
                this,
                loadTimeout,
                () => gameStarted,
                StartGame);

        setup =
            new MatchSetup(
                spawnpointCalculator);

        GameLog.Write(
            "GameMaster",
            "OnStartServer state reset gameStarted=false currentPlayerIn=-1 phase=Wait, MatchStartup created, starting it");

        startup.Begin();
    }

    [Server]
    public void PlayerLoaded(
        NetworkConnectionToClient conn)
    {
        GameLog.Write(
            "GameMaster",
            $"PlayerLoaded forwarded to MatchStartup conn={(conn != null ? conn.connectionId.ToString() : "NULL")} startupExists={startup != null}");

        startup?.PlayerLoaded(
            conn);
    }


    [Server]
    private void StartGame(
        List<Player> players)
    {
        GameLog.Write(
            "GameMaster",
            $"StartGame players={(players != null ? players.Count.ToString() : "NULL")} gameStarted={gameStarted} startupCancelled={startup.IsCancelled}");

        if (gameStarted ||
            startup.IsCancelled)
        {
            GameLog.Write(
                "GameMaster",
                "StartGame skipped: game already started or startup cancelled");

            return;
        }

        if (players == null ||
            players.Count == 0)
        {
            GameLog.Write(
                "GameMaster",
                "StartGame failed: no players found");

            startup.Cancel(
                "No players found.");

            return;
        }

        if (players.Count > 5)
        {
            GameLog.Write(
                "GameMaster",
                $"StartGame failed: too many players count={players.Count}");

            startup.Cancel(
                "More than 5 players were captured.");

            return;
        }

        if (!startup.AreAllExpectedPlayersPresent(
                players))
        {
            GameLog.Write(
                "GameMaster",
                "StartGame skipped: not all expected players present");

            return;
        }

        string setupFailure =
            setup.Prepare(
                players.Count);

        if (setupFailure != null)
        {
            GameLog.Write(
                "GameMaster",
                $"StartGame failed: setup preparation reason={setupFailure}");

            startup.Cancel(
                setupFailure);

            return;
        }

        _players.Clear();
        _players.AddRange(players);

        gameStarted = true;

        GameLog.Write(
            "GameMaster",
            $"StartGame gameStarted=true players={_players.Count}");

        GameLog.Write(
            "GameMaster",
            "StartGame stopping load timeout timer");

        startup.StopLoadTimeout();

        setup.SeatPlayers(
            _players);

        GameLog.Write(
            "GameMaster",
            "StartGame players set up, calling BeginTurn with random first player");

        BeginTurn(
            Random.Range(
                0,
                _players.Count));
    }

    [Server]
    private void BeginTurn(
        int playerIndex)
    {
        GameLog.Write(
            "GameMaster",
            $"BeginTurn playerIndex={playerIndex} gameStarted={gameStarted} players={_players.Count} previousPlayerIn={currentPlayerIn}");

        if (!gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                "BeginTurn skipped: game not started");

            return;
        }

        if (playerIndex < 0 ||
            playerIndex >= _players.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"BeginTurn rejected: invalid player index {playerIndex} players={_players.Count}");

            Debug.LogError(
                $"GameMaster: invalid player index {playerIndex}.");

            return;
        }

        currentPlayerIn =
            playerIndex;

        selectedAttack = null;
        selectedTarget = null;

        GameLog.Write(
            "GameMaster",
            $"BeginTurn current player set currentPlayerIn={currentPlayerIn} player={(_players[currentPlayerIn] != null ? _players[currentPlayerIn].netId.ToString() : "NULL")} selectedAttack=NULL selectedTarget=NULL");

        SetGamePhase(
            GamePhase.Attack);

        GameLog.Write(
            "GameMaster",
            $"BeginTurn calling OnTurnStart player={(_players[currentPlayerIn] != null ? _players[currentPlayerIn].netId.ToString() : "NULL")}");

        _players[currentPlayerIn].OnTurnStart();
    }

    [Server]
    public void EndTurn()
    {
        GameLog.Write(
            "GameMaster",
            $"EndTurn gameStarted={gameStarted} currentPlayerIn={currentPlayerIn} phase={currentPhase} players={_players.Count}");

        if (!gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                "EndTurn skipped: game not started");

            return;
        }

        if (_players.Count == 0)
        {
            GameLog.Write(
                "GameMaster",
                "EndTurn skipped: no players");

            return;
        }

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"EndTurn rejected: invalid currentPlayerIn {currentPlayerIn} players={_players.Count}");

            Debug.LogError(
                $"GameMaster: invalid currentPlayerIn {currentPlayerIn}.");

            return;
        }

        selectedAttack = null;
        selectedTarget = null;

        GameLog.Write(
            "GameMaster",
            $"EndTurn calling OnTurnEnd player={(_players[currentPlayerIn] != null ? _players[currentPlayerIn].netId.ToString() : "NULL")} next={(currentPlayerIn + 1) % _players.Count}");

        _players[currentPlayerIn].OnTurnEnd();

        BeginTurn(
            (currentPlayerIn + 1) %
            _players.Count);
    }

    [Server]
    public bool SelectAttack(
        Card card,
        Player player)
    {
        GameLog.Write(
            "GameMaster",
            $"SelectAttack card={(card != null && card.Data != null ? card.Data.name : "NULL")} player={(player != null ? player.netId.ToString() : "NULL")} phase={currentPhase}");

        if (!gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                "SelectAttack rejected: game not started");

            return false;
        }

        if (currentPhase !=
            GamePhase.Attack)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectAttack rejected: wrong phase phase={currentPhase}");

            return false;
        }

        if (card == null ||
            player == null)
        {
            GameLog.Write(
                "GameMaster",
                "SelectAttack rejected: card or player is null");

            return false;
        }

        if (!IsCurrentPlayer(player))
        {
            GameLog.Write(
                "GameMaster",
                $"SelectAttack rejected: not current player player={player.netId} currentPlayerIn={currentPlayerIn}");

            return false;
        }

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectAttack rejected: card unavailable isAvailable={card.IsAvailable} isInRoulette={card.IsInRoulette}");

            return false;
        }

        if (card.OwnerNetId !=
            player.netId)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectAttack rejected: card owner mismatch cardOwner={card.OwnerNetId} player={player.netId}");

            return false;
        }

        if (card.Data == null)
        {
            GameLog.Write(
                "GameMaster",
                "SelectAttack rejected: card has no Data");

            return false;
        }

        if (card.Data.CardClass !=
                Card.CardClass.AttackLight &&
            card.Data.CardClass !=
                Card.CardClass.AttackSteal)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectAttack rejected: not an attack card class={card.Data.CardClass}");

            return false;
        }

        selectedAttack = card;

        GameLog.Write(
            "GameMaster",
            $"SelectAttack accepted selectedAttack={card.Data.name} class={card.Data.CardClass} player={player.netId}");

        SetGamePhase(
            GamePhase.SelectTarget);

        return true;
    }

    [Server]
    public bool SelectTarget(
        Player player)
    {
        GameLog.Write(
            "GameMaster",
            $"SelectTarget player={(player != null ? player.netId.ToString() : "NULL")} phase={currentPhase}");

        if (!gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                "SelectTarget rejected: game not started");

            return false;
        }

        if (currentPhase !=
            GamePhase.SelectTarget)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectTarget rejected: wrong phase phase={currentPhase}");

            return false;
        }

        if (selectedAttack == null ||
            player == null)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectTarget rejected: selectedAttack or player is null selectedAttackNull={selectedAttack == null} playerNull={player == null}");

            return false;
        }

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectTarget rejected: invalid currentPlayerIn {currentPlayerIn} players={_players.Count}");

            return false;
        }

        Player attacker =
            _players[currentPlayerIn];

        if (player == attacker)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectTarget rejected: target is the attacker player={player.netId}");

            return false;
        }

        if (!_players.Contains(player))
        {
            GameLog.Write(
                "GameMaster",
                $"SelectTarget rejected: player is not in the game player={player.netId}");

            return false;
        }

        selectedTarget = player;

        GameLog.Write(
            "GameMaster",
            $"SelectTarget accepted selectedTarget={player.netId} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

        SetGamePhase(
            GamePhase.Defense);

        return true;
    }

    [Server]
    public bool SelectDefense(
        Card card,
        Player player)
    {
        GameLog.Write(
            "GameMaster",
            $"SelectDefense card={(card != null && card.Data != null ? card.Data.name : "NULL")} player={(player != null ? player.netId.ToString() : "NULL")} phase={currentPhase}");

        if (!gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                "SelectDefense rejected: game not started");

            return false;
        }

        if (currentPhase !=
            GamePhase.Defense)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectDefense rejected: wrong phase phase={currentPhase}");

            return false;
        }

        if (selectedAttack == null ||
            selectedTarget == null)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectDefense rejected: selectedAttack or selectedTarget is null selectedAttackNull={selectedAttack == null} selectedTargetNull={selectedTarget == null}");

            return false;
        }

        if (player == null ||
            player != selectedTarget)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectDefense rejected: player is not the selected target player={(player != null ? player.netId.ToString() : "NULL")} selectedTarget={selectedTarget.netId}");

            return false;
        }

        if (card == null)
        {
            GameLog.Write(
                "GameMaster",
                "SelectDefense rejected: card is null");

            return false;
        }

        if (card.OwnerNetId !=
            player.netId)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectDefense rejected: card owner mismatch cardOwner={card.OwnerNetId} player={player.netId}");

            return false;
        }

        if (card.IsInRoulette ||
            !card.IsAvailable)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectDefense rejected: card unavailable isAvailable={card.IsAvailable} isInRoulette={card.IsInRoulette}");

            return false;
        }

        if (card.Data == null ||
            card.Data.CardClass !=
                Card.CardClass.Defence)
        {
            GameLog.Write(
                "GameMaster",
                $"SelectDefense rejected: not a defence card dataNull={card.Data == null}");

            return false;
        }

        GameLog.Write(
            "GameMaster",
            $"SelectDefense accepted card={card.Data.name} player={player.netId}");

        return true;
    }

    [Server]
    public bool SkipDefense(
        Player player)
    {
        GameLog.Write(
            "GameMaster",
            $"SkipDefense player={(player != null ? player.netId.ToString() : "NULL")} phase={currentPhase}");

        if (!gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                "SkipDefense rejected: game not started");

            return false;
        }

        if (currentPhase !=
            GamePhase.Defense)
        {
            GameLog.Write(
                "GameMaster",
                $"SkipDefense rejected: wrong phase phase={currentPhase}");

            return false;
        }

        if (selectedTarget == null ||
            player == null)
        {
            GameLog.Write(
                "GameMaster",
                $"SkipDefense rejected: selectedTarget or player is null selectedTargetNull={selectedTarget == null} playerNull={player == null}");

            return false;
        }

        if (player != selectedTarget)
        {
            GameLog.Write(
                "GameMaster",
                $"SkipDefense rejected: player is not the selected target player={player.netId} selectedTarget={selectedTarget.netId}");

            return false;
        }

        GameLog.Write(
            "GameMaster",
            $"SkipDefense accepted player={player.netId}");

        return true;
    }

    [Server]
    public bool BeginStealPhase()
    {
        GameLog.Write(
            "GameMaster",
            $"BeginStealPhase phase={currentPhase} selectedAttack={(selectedAttack != null && selectedAttack.Data != null ? selectedAttack.Data.name : "NULL")} selectedTarget={(selectedTarget != null ? selectedTarget.netId.ToString() : "NULL")}");

        if (!gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                "BeginStealPhase rejected: game not started");

            return false;
        }

        if (currentPhase !=
            GamePhase.Defense)
        {
            GameLog.Write(
                "GameMaster",
                $"BeginStealPhase rejected: wrong phase phase={currentPhase}");

            return false;
        }

        if (selectedAttack == null ||
            selectedTarget == null)
        {
            GameLog.Write(
                "GameMaster",
                $"BeginStealPhase rejected: selectedAttack or selectedTarget is null selectedAttackNull={selectedAttack == null} selectedTargetNull={selectedTarget == null}");

            return false;
        }

        if (selectedAttack.Data == null ||
            selectedAttack.Data.CardClass !=
                Card.CardClass.AttackSteal)
        {
            GameLog.Write(
                "GameMaster",
                $"BeginStealPhase rejected: selected attack is not a steal card dataNull={selectedAttack.Data == null}");

            return false;
        }

        SetGamePhase(
            GamePhase.Steal);

        GameLog.Write(
            "GameMaster",
            "BeginStealPhase accepted");

        return true;
    }

    [Server]
    public bool StealAccesory(
        Player target,
        int index,
        NetworkConnectionToClient sender)
    {
        GameLog.Write(
            "GameMaster",
            $"StealAccesory target={(target != null ? target.netId.ToString() : "NULL")} index={index} senderConn={(sender != null ? sender.connectionId.ToString() : "NULL")} phase={currentPhase}");

        if (!gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                "StealAccesory rejected: game not started");

            return false;
        }

        if (currentPhase !=
            GamePhase.Steal)
        {
            GameLog.Write(
                "GameMaster",
                $"StealAccesory rejected: wrong phase phase={currentPhase}");

            return false;
        }

        if (sender == null)
        {
            GameLog.Write(
                "GameMaster",
                "StealAccesory rejected: sender is null");

            return false;
        }

        if (selectedTarget == null ||
            target == null ||
            target != selectedTarget)
        {
            GameLog.Write(
                "GameMaster",
                $"StealAccesory rejected: target is not the selected target selectedTarget={(selectedTarget != null ? selectedTarget.netId.ToString() : "NULL")} target={(target != null ? target.netId.ToString() : "NULL")}");

            return false;
        }

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"StealAccesory rejected: invalid currentPlayerIn {currentPlayerIn} players={_players.Count}");

            return false;
        }

        Player thief =
            _players[currentPlayerIn];

        if (thief == null ||
            thief == target)
        {
            GameLog.Write(
                "GameMaster",
                $"StealAccesory rejected: invalid thief thiefNull={thief == null} thiefIsTarget={thief == target}");

            return false;
        }

        if (thief.netIdentity == null ||
            thief.netIdentity.connectionToClient !=
                sender)
        {
            GameLog.Write(
                "GameMaster",
                $"StealAccesory rejected: sender is not the thief thief={thief.netId} senderConn={sender.connectionId}");

            return false;
        }

        if (!target.HasAccessory(index))
        {
            GameLog.Write(
                "GameMaster",
                $"StealAccesory rejected: target has no accessory index={index} target={target.netId}");

            return false;
        }

        if (!target.SetAccesory(
                index,
                false))
        {
            GameLog.Write(
                "GameMaster",
                $"StealAccesory rejected: could not remove accessory from target index={index} target={target.netId}");

            return false;
        }

        if (!thief.SetAccesory(
                index,
                true))
        {
            GameLog.Write(
                "GameMaster",
                $"StealAccesory rejected: could not give accessory to thief index={index} thief={thief.netId}, restoring target accessory");

            target.SetAccesory(
                index,
                true);

            return false;
        }

        GameLog.Write(
            "GameMaster",
            $"StealAccesory accessory stolen index={index} from={target.netId} to={thief.netId}, ending turn");

        EndTurn();

        return true;
    }

    [Server]
    private bool IsCurrentPlayer(
        Player player)
    {
        GameLog.Write(
            "GameMaster",
            $"IsCurrentPlayer player={(player != null ? player.netId.ToString() : "NULL")} currentPlayerIn={currentPlayerIn} players={_players.Count}");

        if (player == null)
        {
            GameLog.Write(
                "GameMaster",
                "IsCurrentPlayer false: player is null");

            return false;
        }

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"IsCurrentPlayer false: invalid currentPlayerIn {currentPlayerIn}");

            return false;
        }

        return _players[currentPlayerIn] ==
               player;
    }

    [Server]
    private void ClearTurnState()
    {
        GameLog.Write(
            "GameMaster",
            "ClearTurnState selectedAttack=NULL selectedTarget=NULL");

        selectedAttack = null;
        selectedTarget = null;
    }
}
