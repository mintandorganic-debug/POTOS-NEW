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

    private readonly HashSet<int> expectedConnections = new();
    private readonly HashSet<int> loadedConnections = new();

    private readonly List<Player> _players = new();

    public IReadOnlyList<Player> Players =>
        _players;

    private Card selectedAttack;
    private Player selectedTarget;

    private bool gameStarted;
    private bool startupCancelled;

    private Coroutine timeoutRoutine;
    private Coroutine startupRoutine;

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

        expectedConnections.Clear();
        loadedConnections.Clear();
        _players.Clear();

        gameStarted = false;
        startupCancelled = false;
        currentPlayerIn = -1;

        selectedAttack = null;
        selectedTarget = null;

        currentPhase = GamePhase.Wait;

        GameLog.Write(
            "GameMaster",
            "OnStartServer state reset gameStarted=false startupCancelled=false currentPlayerIn=-1 phase=Wait, starting InitializeLobbyConnections");

        StartCoroutine(
            InitializeLobbyConnections());
    }

    [Server]
    private IEnumerator InitializeLobbyConnections()
    {
        GameLog.Write(
            "GameMaster",
            "InitializeLobbyConnections start");

        yield return null;

        while (!gameStarted &&
               !startupCancelled)
        {
            CaptureLobbyConnections();

            if (expectedConnections.Count > 0)
                break;

            yield return null;
        }

        GameLog.Write(
            "GameMaster",
            $"InitializeLobbyConnections lobby captured expected={expectedConnections.Count} gameStarted={gameStarted} startupCancelled={startupCancelled}");

        timeoutRoutine =
            StartCoroutine(
                LoadTimeoutRoutine());

        GameLog.Write(
            "GameMaster",
            $"InitializeLobbyConnections load timeout timer started loadTimeout={loadTimeout}");

        TryStartGame();

        GameLog.Write(
            "GameMaster",
            "InitializeLobbyConnections end");
    }

    [Server]
    private void CaptureLobbyConnections()
    {
        expectedConnections.Clear();

        foreach (NetworkConnectionToClient conn
                 in NetworkServer.connections.Values)
        {
            if (conn == null)
                continue;

            expectedConnections.Add(
                conn.connectionId);
        }

        if (expectedConnections.Count > 0)
        {
            GameLog.Write(
                "GameMaster",
                $"CaptureLobbyConnections expected={expectedConnections.Count} ids={string.Join(",", expectedConnections)}");
        }
    }

    [Server]
    public void PlayerLoaded(
        NetworkConnectionToClient conn)
    {
        GameLog.Write(
            "GameMaster",
            $"PlayerLoaded conn={(conn != null ? conn.connectionId.ToString() : "NULL")} startupCancelled={startupCancelled} gameStarted={gameStarted} loaded={loadedConnections.Count} expected={expectedConnections.Count}");

        if (startupCancelled ||
            gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                $"PlayerLoaded ignored: startupCancelled={startupCancelled} gameStarted={gameStarted}");

            return;
        }

        if (conn == null)
        {
            GameLog.Write(
                "GameMaster",
                "PlayerLoaded rejected: connection is null");

            Debug.LogError(
                "GameMaster.PlayerLoaded: connection is null.");

            return;
        }

        int connectionId =
            conn.connectionId;

        if (!expectedConnections.Contains(
                connectionId))
        {
            GameLog.Write(
                "GameMaster",
                $"PlayerLoaded rejected: connection {connectionId} is not part of the captured lobby");

            Debug.LogWarning(
                $"GameMaster.PlayerLoaded: connection {connectionId} is not part of the captured lobby.");

            return;
        }

        loadedConnections.Add(
            connectionId);

        GameLog.Write(
            "GameMaster",
            $"PlayerLoaded connection {connectionId} marked loaded loaded={loadedConnections.Count} expected={expectedConnections.Count}");

        TryStartGame();
    }

    [Server]
    private void TryStartGame()
    {
        GameLog.Write(
            "GameMaster",
            $"TryStartGame gameStarted={gameStarted} startupCancelled={startupCancelled} expected={expectedConnections.Count} loaded={loadedConnections.Count} startupRoutineRunning={startupRoutine != null}");

        if (gameStarted ||
            startupCancelled)
        {
            GameLog.Write(
                "GameMaster",
                "TryStartGame skipped: game already started or startup cancelled");

            return;
        }

        if (expectedConnections.Count == 0)
        {
            GameLog.Write(
                "GameMaster",
                "TryStartGame skipped: no expected connections");

            return;
        }

        if (loadedConnections.Count !=
            expectedConnections.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"TryStartGame waiting: loaded={loadedConnections.Count} expected={expectedConnections.Count}");

            return;
        }

        foreach (int connectionId
                 in expectedConnections)
        {
            if (!loadedConnections.Contains(
                    connectionId))
            {
                GameLog.Write(
                    "GameMaster",
                    $"TryStartGame waiting: connection {connectionId} not loaded");

                return;
            }

            if (!NetworkServer.connections.TryGetValue(
                    connectionId,
                    out NetworkConnectionToClient conn))
            {
                GameLog.Write(
                    "GameMaster",
                    $"TryStartGame waiting: connection {connectionId} not found in NetworkServer.connections");

                return;
            }

            if (conn == null ||
                conn.identity == null)
            {
                GameLog.Write(
                    "GameMaster",
                    $"TryStartGame waiting: connection {connectionId} has no identity yet");

                return;
            }

            if (!conn.identity.TryGetComponent<Player>(
                    out Player player))
            {
                GameLog.Write(
                    "GameMaster",
                    $"TryStartGame waiting: connection {connectionId} identity has no Player component");

                return;
            }

            if (player == null)
            {
                GameLog.Write(
                    "GameMaster",
                    $"TryStartGame waiting: connection {connectionId} Player is null");

                return;
            }
        }

        if (startupRoutine != null)
        {
            GameLog.Write(
                "GameMaster",
                "TryStartGame skipped: startupRoutine already running");

            return;
        }

        GameLog.Write(
            "GameMaster",
            "TryStartGame all players ready, starting StartGameRoutine");

        startupRoutine =
            StartCoroutine(
                StartGameRoutine());
    }

    [Server]
    private IEnumerator StartGameRoutine()
    {
        GameLog.Write(
            "GameMaster",
            "StartGameRoutine start");

        startupRoutine = null;

        yield return null;

        if (gameStarted ||
            startupCancelled)
        {
            GameLog.Write(
                "GameMaster",
                $"StartGameRoutine end: aborted gameStarted={gameStarted} startupCancelled={startupCancelled}");

            yield break;
        }

        List<Player> players =
            BuildPlayerList();

        if (players == null ||
            players.Count == 0)
        {
            GameLog.Write(
                "GameMaster",
                $"StartGameRoutine end: no players built playersNull={players == null}");

            yield break;
        }

        if (players.Count !=
            expectedConnections.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"StartGameRoutine end: player count changed players={players.Count} expected={expectedConnections.Count}");

            Debug.LogWarning(
                "GameMaster: player count changed before startup.");

            yield break;
        }

        if (!AreAllExpectedPlayersPresent(
                players))
        {
            GameLog.Write(
                "GameMaster",
                "StartGameRoutine end: not all expected players present");

            yield break;
        }

        GameLog.Write(
            "GameMaster",
            $"StartGameRoutine calling StartGame players={players.Count}");

        StartGame(
            players);

        GameLog.Write(
            "GameMaster",
            "StartGameRoutine end");
    }

    [Server]
    private List<Player> BuildPlayerList()
    {
        GameLog.Write(
            "GameMaster",
            $"BuildPlayerList expected={expectedConnections.Count}");

        List<Player> players =
            new List<Player>();

        foreach (int connectionId
                 in expectedConnections.OrderBy(
                     id => id))
        {
            if (!NetworkServer.connections.TryGetValue(
                    connectionId,
                    out NetworkConnectionToClient conn))
            {
                GameLog.Write(
                    "GameMaster",
                    $"BuildPlayerList null: connection {connectionId} not found");

                return null;
            }

            if (conn == null ||
                conn.identity == null)
            {
                GameLog.Write(
                    "GameMaster",
                    $"BuildPlayerList null: connection {connectionId} has no identity");

                return null;
            }

            if (!conn.identity.TryGetComponent<Player>(
                    out Player player))
            {
                GameLog.Write(
                    "GameMaster",
                    $"BuildPlayerList null: connection {connectionId} identity has no Player");

                return null;
            }

            if (player == null)
            {
                GameLog.Write(
                    "GameMaster",
                    $"BuildPlayerList null: connection {connectionId} Player is null");

                return null;
            }

            players.Add(player);
        }

        GameLog.Write(
            "GameMaster",
            $"BuildPlayerList built players={players.Count}");

        return players;
    }

    [Server]
    private bool AreAllExpectedPlayersPresent(
        List<Player> players)
    {
        GameLog.Write(
            "GameMaster",
            $"AreAllExpectedPlayersPresent players={(players != null ? players.Count.ToString() : "NULL")} expected={expectedConnections.Count}");

        if (players == null)
        {
            GameLog.Write(
                "GameMaster",
                "AreAllExpectedPlayersPresent false: players is null");

            return false;
        }

        if (players.Count !=
            expectedConnections.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"AreAllExpectedPlayersPresent false: count mismatch players={players.Count} expected={expectedConnections.Count}");

            return false;
        }

        if (players.Any(
                player => player == null))
        {
            GameLog.Write(
                "GameMaster",
                "AreAllExpectedPlayersPresent false: list contains a null player");

            return false;
        }

        if (players.Distinct().Count() !=
            players.Count)
        {
            GameLog.Write(
                "GameMaster",
                "AreAllExpectedPlayersPresent false: duplicate players in list");

            return false;
        }

        HashSet<int> playerConnections =
            new HashSet<int>();

        foreach (Player player in players)
        {
            if (player.netIdentity == null)
            {
                GameLog.Write(
                    "GameMaster",
                    $"AreAllExpectedPlayersPresent false: player {player.name} has no netIdentity");

                return false;
            }

            NetworkConnectionToClient conn =
                player.netIdentity.connectionToClient;

            if (conn == null)
            {
                GameLog.Write(
                    "GameMaster",
                    $"AreAllExpectedPlayersPresent false: player {player.netId} has no connectionToClient");

                return false;
            }

            playerConnections.Add(
                conn.connectionId);
        }

        GameLog.Write(
            "GameMaster",
            $"AreAllExpectedPlayersPresent checking connection sets playerConnections={string.Join(",", playerConnections)} expected={string.Join(",", expectedConnections)}");

        return playerConnections.SetEquals(
            expectedConnections);
    }

    [Server]
    private void StartGame(
        List<Player> players)
    {
        GameLog.Write(
            "GameMaster",
            $"StartGame players={(players != null ? players.Count.ToString() : "NULL")} gameStarted={gameStarted} startupCancelled={startupCancelled}");

        if (gameStarted ||
            startupCancelled)
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

            CancelStartup(
                "No players found.");

            return;
        }

        if (players.Count > 5)
        {
            GameLog.Write(
                "GameMaster",
                $"StartGame failed: too many players count={players.Count}");

            CancelStartup(
                "More than 5 players were captured.");

            return;
        }

        if (!AreAllExpectedPlayersPresent(
                players))
        {
            GameLog.Write(
                "GameMaster",
                "StartGame skipped: not all expected players present");

            return;
        }

        if (spawnpointCalculator == null)
        {
            GameLog.Write(
                "GameMaster",
                "StartGame failed: spawnpointCalculator is null");

            CancelStartup(
                "SpawnpointCalculator missing.");

            return;
        }

        spawnpointCalculator.CalculateSpawnPoints(
            players.Count);

        GameLog.Write(
            "GameMaster",
            $"StartGame spawn points calculated spawnPoints={spawnpointCalculator.SpawnPoints.Count} players={players.Count}");

        if (spawnpointCalculator.SpawnPoints.Count <
            players.Count)
        {
            GameLog.Write(
                "GameMaster",
                "StartGame failed: not enough spawn points");

            CancelStartup(
                "Not enough spawn points.");

            return;
        }

        List<int> accessoryPool =
            new List<int>
            {
                0,
                1,
                2,
                3,
                4
            };

        if (accessoryPool.Count <
            players.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"StartGame failed: not enough accessories pool={accessoryPool.Count} players={players.Count}");

            CancelStartup(
                "Not enough accessories for all players.");

            return;
        }

        _players.Clear();
        _players.AddRange(players);

        gameStarted = true;

        GameLog.Write(
            "GameMaster",
            $"StartGame gameStarted=true players={_players.Count}");

        if (timeoutRoutine != null)
        {
            GameLog.Write(
                "GameMaster",
                "StartGame stopping load timeout timer");

            StopCoroutine(
                timeoutRoutine);

            timeoutRoutine = null;
        }

        for (int i = 0;
             i < _players.Count;
             i++)
        {
            Player player =
                _players[i];

            Pose pose =
                spawnpointCalculator.SpawnPoints[i];

            GameLog.Write(
                "GameMaster",
                $"StartGame setting up seat={i} player={(player != null ? player.netId.ToString() : "NULL")} position={pose.position}");

            player.SetSeat(i);

            player.SummonCards();

            int accessoryIndex =
                Random.Range(
                    0,
                    accessoryPool.Count);

            GameLog.Write(
                "GameMaster",
                $"StartGame seat={i} accessoryIndex={accessoryIndex} poolCount={accessoryPool.Count}");

            for (int j = 0;
                 j < 5;
                 j++)
            {
                player.SetAccesory(
                    j,
                    false);
            }

            player.SetAccesory(
                accessoryIndex,
                true);

            accessoryPool.Remove(
                accessoryIndex);

            player.transform.SetPositionAndRotation(
                pose.position,
                pose.rotation);
        }

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
    public bool TryStealAccesory(
        Player target,
        int index,
        NetworkConnectionToClient sender)
    {
        GameLog.Write(
            "GameMaster",
            $"TryStealAccesory target={(target != null ? target.netId.ToString() : "NULL")} index={index} senderConn={(sender != null ? sender.connectionId.ToString() : "NULL")} phase={currentPhase}");

        if (!gameStarted)
        {
            GameLog.Write(
                "GameMaster",
                "TryStealAccesory rejected: game not started");

            return false;
        }

        if (currentPhase !=
            GamePhase.Steal)
        {
            GameLog.Write(
                "GameMaster",
                $"TryStealAccesory rejected: wrong phase phase={currentPhase}");

            return false;
        }

        if (sender == null)
        {
            GameLog.Write(
                "GameMaster",
                "TryStealAccesory rejected: sender is null");

            return false;
        }

        if (selectedTarget == null ||
            target == null ||
            target != selectedTarget)
        {
            GameLog.Write(
                "GameMaster",
                $"TryStealAccesory rejected: target is not the selected target selectedTarget={(selectedTarget != null ? selectedTarget.netId.ToString() : "NULL")} target={(target != null ? target.netId.ToString() : "NULL")}");

            return false;
        }

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            GameLog.Write(
                "GameMaster",
                $"TryStealAccesory rejected: invalid currentPlayerIn {currentPlayerIn} players={_players.Count}");

            return false;
        }

        Player thief =
            _players[currentPlayerIn];

        if (thief == null ||
            thief == target)
        {
            GameLog.Write(
                "GameMaster",
                $"TryStealAccesory rejected: invalid thief thiefNull={thief == null} thiefIsTarget={thief == target}");

            return false;
        }

        if (thief.netIdentity == null ||
            thief.netIdentity.connectionToClient !=
                sender)
        {
            GameLog.Write(
                "GameMaster",
                $"TryStealAccesory rejected: sender is not the thief thief={thief.netId} senderConn={sender.connectionId}");

            return false;
        }

        if (!target.HasAccessory(index))
        {
            GameLog.Write(
                "GameMaster",
                $"TryStealAccesory rejected: target has no accessory index={index} target={target.netId}");

            return false;
        }

        if (!target.SetAccesory(
                index,
                false))
        {
            GameLog.Write(
                "GameMaster",
                $"TryStealAccesory rejected: could not remove accessory from target index={index} target={target.netId}");

            return false;
        }

        if (!thief.SetAccesory(
                index,
                true))
        {
            GameLog.Write(
                "GameMaster",
                $"TryStealAccesory rejected: could not give accessory to thief index={index} thief={thief.netId}, restoring target accessory");

            target.SetAccesory(
                index,
                true);

            return false;
        }

        GameLog.Write(
            "GameMaster",
            $"TryStealAccesory accessory stolen index={index} from={target.netId} to={thief.netId}, ending turn");

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
    private IEnumerator LoadTimeoutRoutine()
    {
        GameLog.Write(
            "GameMaster",
            $"LoadTimeoutRoutine start loadTimeout={loadTimeout}");

        yield return new WaitForSeconds(
            loadTimeout);

        timeoutRoutine = null;

        GameLog.Write(
            "GameMaster",
            $"LoadTimeoutRoutine timeout elapsed gameStarted={gameStarted} startupCancelled={startupCancelled} loaded={loadedConnections.Count} expected={expectedConnections.Count}");

        if (gameStarted ||
            startupCancelled)
        {
            GameLog.Write(
                "GameMaster",
                "LoadTimeoutRoutine end: game already started or startup cancelled");

            yield break;
        }

        List<int> missing =
            expectedConnections
                .Where(
                    id => !loadedConnections.Contains(id))
                .ToList();

        if (missing.Count > 0)
        {
            GameLog.Write(
                "GameMaster",
                $"LoadTimeoutRoutine loading timeout, missing connections: {string.Join(", ", missing)}");

            CancelStartup(
                $"Loading timeout. Missing connections: {string.Join(", ", missing)}");

            yield break;
        }

        GameLog.Write(
            "GameMaster",
            "LoadTimeoutRoutine all connections loaded, retrying TryStartGame");

        TryStartGame();

        yield return null;

        if (!gameStarted &&
            !startupCancelled)
        {
            GameLog.Write(
                "GameMaster",
                "LoadTimeoutRoutine all players loaded but Player objects not ready");

            CancelStartup(
                "All players reported loaded, but their Player objects were not ready.");
        }

        GameLog.Write(
            "GameMaster",
            "LoadTimeoutRoutine end");
    }

    [Server]
    private void CancelStartup(
        string reason)
    {
        GameLog.Write(
            "GameMaster",
            $"CancelStartup reason={reason} alreadyCancelled={startupCancelled}");

        if (startupCancelled)
        {
            GameLog.Write(
                "GameMaster",
                "CancelStartup skipped: already cancelled");

            return;
        }

        startupCancelled = true;

        GameLog.Write(
            "GameMaster",
            "CancelStartup startupCancelled=true");

        if (timeoutRoutine != null)
        {
            StopCoroutine(
                timeoutRoutine);

            timeoutRoutine = null;
        }

        if (startupRoutine != null)
        {
            StopCoroutine(
                startupRoutine);

            startupRoutine = null;
        }

        Debug.LogError(
            $"Game startup cancelled: {reason}");

        if (NetworkManager.singleton == null)
        {
            GameLog.Write(
                "GameMaster",
                "CancelStartup: NetworkManager.singleton is null, nothing to stop");

            return;
        }

        if (NetworkManager.singleton.mode ==
            NetworkManagerMode.Host)
        {
            GameLog.Write(
                "GameMaster",
                "CancelStartup stopping host");

            NetworkManager.singleton.StopHost();
        }
        else
        {
            GameLog.Write(
                "GameMaster",
                "CancelStartup stopping server");

            NetworkManager.singleton.StopServer();
        }
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
