using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameMaster : NetworkBehaviour, IAttackSelectionContext, IAccessoryTheftContext
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
        selection?.Target;

    private readonly List<Player> _players = new();

    public IReadOnlyList<Player> Players =>
        _players;

    private AttackSelection selection;
    private AccessoryTheft theft;

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

        selection =
            new AttackSelection(
                this);

        theft =
            new AccessoryTheft(
                this,
                selection);

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

        int firstPlayer =
            DebugDeal.HostFirstIndex(
                _players);

        BeginTurn(
            firstPlayer >= 0
                ? firstPlayer
                : Random.Range(
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

        selection.Clear();

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

        selection.Clear();

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
        return selection.SelectAttack(
            card,
            player);
    }

    [Server]
    public bool SelectTarget(
        Player player)
    {
        return selection.SelectTarget(
            player);
    }

    [Server]
    public bool SelectDefense(
        Card card,
        Player player)
    {
        return selection.SelectDefense(
            card,
            player);
    }

    [Server]
    public bool SkipDefense(
        Player player)
    {
        return selection.SkipDefense(
            player);
    }

    [Server]
    public bool BeginStealPhase()
    {
        return theft.BeginStealPhase();
    }

    [Server]
    public bool StealAccesory(
        Player target,
        int index,
        NetworkConnectionToClient sender)
    {
        return theft.StealAccesory(
            target,
            index,
            sender);
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

        selection.Clear();
    }

    bool IAttackSelectionContext.GameStarted =>
        gameStarted;

    GamePhase IAttackSelectionContext.Phase =>
        currentPhase;

    bool IAttackSelectionContext.IsCurrentPlayer(
        Player player)
    {
        return IsCurrentPlayer(
            player);
    }

    void IAttackSelectionContext.SetPhase(
        GamePhase phase)
    {
        SetGamePhase(
            phase);
    }
}
