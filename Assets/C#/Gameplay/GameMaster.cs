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

    [SyncVar(hook = nameof(OnGamePhaseChanged))]
    [SerializeField] private GamePhase currentPhase = GamePhase.Attack;

    public GamePhase CurrentPhase =>
        currentPhase;

    private readonly HashSet<int> expectedConnections = new();
    private readonly HashSet<int> loadedConnections = new();

    private readonly List<Player> _players = new();
    public IReadOnlyList<Player> Players => _players;

    private int currentPlayerIn = -1;

    private bool gameStarted;
    private bool startupCancelled;

    private Coroutine timeoutRoutine;
    private Coroutine startupRoutine;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================================================
    // PHASE
    // =========================================================

    [Server]
    public void SetGamePhase(GamePhase phase)
    {
        if (currentPhase == phase)
            return;

        currentPhase = phase;
    }

    private void OnGamePhaseChanged(
        GamePhase oldPhase,
        GamePhase newPhase)
    {
        GamePhaseChanged?.Invoke(newPhase);
    }

    // =========================================================
    // SERVER START
    // =========================================================

    public override void OnStartServer()
    {
        base.OnStartServer();

        expectedConnections.Clear();
        loadedConnections.Clear();
        _players.Clear();

        gameStarted = false;
        startupCancelled = false;
        currentPlayerIn = -1;

        currentPhase = GamePhase.Attack;

        StartCoroutine(
            InitializeLobbyConnections());
    }

    // =========================================================
    // CAPTURE LOBBY
    // =========================================================

    [Server]
    private IEnumerator InitializeLobbyConnections()
    {
        yield return null;

        while (!gameStarted &&
               !startupCancelled)
        {
            CaptureLobbyConnections();

            if (expectedConnections.Count > 0)
                break;

            yield return null;
        }

        timeoutRoutine =
            StartCoroutine(
                LoadTimeoutRoutine());

        TryStartGame();
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

        Debug.Log(
            $"GameMaster: captured {expectedConnections.Count} lobby connections.");
    }

    // =========================================================
    // PLAYER LOADED
    // =========================================================

    [Server]
    public void PlayerLoaded(
        NetworkConnectionToClient conn)
    {
        if (startupCancelled ||
            gameStarted)
        {
            return;
        }

        if (conn == null)
        {
            Debug.LogError(
                "GameMaster.PlayerLoaded: connection is null.");

            return;
        }

        int connectionId =
            conn.connectionId;

        if (!expectedConnections.Contains(
                connectionId))
        {
            Debug.LogWarning(
                $"GameMaster.PlayerLoaded: connection {connectionId} is not part of the captured lobby.");

            return;
        }

        loadedConnections.Add(
            connectionId);

        Debug.Log(
            $"GameMaster: player loaded. Connection={connectionId}, " +
            $"loaded={loadedConnections.Count}/{expectedConnections.Count}");

        TryStartGame();
    }

    // =========================================================
    // START CHECK
    // =========================================================

    [Server]
    private void TryStartGame()
    {
        if (gameStarted ||
            startupCancelled)
        {
            return;
        }

        if (expectedConnections.Count == 0)
            return;

        if (loadedConnections.Count !=
            expectedConnections.Count)
        {
            return;
        }

        foreach (int connectionId
                 in expectedConnections)
        {
            if (!loadedConnections.Contains(
                    connectionId))
            {
                return;
            }

            if (!NetworkServer.connections.TryGetValue(
                    connectionId,
                    out NetworkConnectionToClient conn))
            {
                return;
            }

            if (conn == null ||
                conn.identity == null)
            {
                return;
            }

            if (!conn.identity.TryGetComponent<Player>(
                    out Player player))
            {
                return;
            }

            if (player == null)
                return;
        }

        if (startupRoutine != null)
            return;

        startupRoutine =
            StartCoroutine(
                StartGameRoutine());
    }

    // =========================================================
    // START GAME ROUTINE
    // =========================================================

    [Server]
    private IEnumerator StartGameRoutine()
    {
        startupRoutine = null;

        yield return null;

        if (gameStarted ||
            startupCancelled)
        {
            yield break;
        }

        List<Player> players =
            BuildPlayerList();

        if (players == null ||
            players.Count == 0)
        {
            yield break;
        }

        if (players.Count !=
            expectedConnections.Count)
        {
            Debug.LogWarning(
                "GameMaster: player count changed before startup.");

            yield break;
        }

        if (!AreAllExpectedPlayersPresent(
                players))
        {
            yield break;
        }

        StartGame(
            players);
    }

    // =========================================================
    // BUILD PLAYER LIST
    // =========================================================

    [Server]
    private List<Player> BuildPlayerList()
    {
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
                return null;
            }

            if (conn == null ||
                conn.identity == null)
            {
                return null;
            }

            if (!conn.identity.TryGetComponent<Player>(
                    out Player player))
            {
                return null;
            }

            if (player == null)
                return null;

            players.Add(player);
        }

        return players;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    [Server]
    private bool AreAllExpectedPlayersPresent(
        List<Player> players)
    {
        if (players == null)
            return false;

        if (players.Count !=
            expectedConnections.Count)
        {
            return false;
        }

        if (players.Any(
                player => player == null))
        {
            return false;
        }

        if (players.Distinct().Count() !=
            players.Count)
        {
            return false;
        }

        HashSet<int> playerConnections =
            new HashSet<int>();

        foreach (Player player in players)
        {
            if (player.netIdentity == null)
                return false;

            NetworkConnectionToClient conn =
                player.netIdentity.connectionToClient;

            if (conn == null)
                return false;

            playerConnections.Add(
                conn.connectionId);
        }

        return playerConnections.SetEquals(
            expectedConnections);
    }

    // =========================================================
    // START GAME
    // =========================================================

    [Server]
    private void StartGame(
        List<Player> players)
    {
        if (gameStarted ||
            startupCancelled)
        {
            return;
        }

        if (players == null ||
            players.Count == 0)
        {
            CancelStartup(
                "No players found.");

            return;
        }

        if (players.Count > 5)
        {
            CancelStartup(
                "More than 5 players were captured.");

            return;
        }

        if (!AreAllExpectedPlayersPresent(
                players))
        {
            return;
        }

        if (spawnpointCalculator == null)
        {
            CancelStartup(
                "SpawnpointCalculator missing.");

            return;
        }

        spawnpointCalculator.CalculateSpawnPoints(
            players.Count);

        if (spawnpointCalculator.SpawnPoints.Count <
            players.Count)
        {
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
            CancelStartup(
                "Not enough accessories for all players.");

            return;
        }

        _players.Clear();
        _players.AddRange(players);

        gameStarted = true;

        if (timeoutRoutine != null)
        {
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

            player.SetSeat(i);

            player.SummonCards();

            int accessoryIndex =
                Random.Range(
                    0,
                    accessoryPool.Count);

            for(int j = 0; j < 5; j++)
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

        BeginTurn(
            Random.Range(
                0,
                _players.Count));
    }

    // =========================================================
    // TURN
    // =========================================================

    [Server]
    private void BeginTurn(
        int playerIndex)
    {
        if (!gameStarted)
            return;

        if (playerIndex < 0 ||
            playerIndex >= _players.Count)
        {
            Debug.LogError(
                $"GameMaster: invalid player index {playerIndex}.");

            return;
        }

        currentPlayerIn =
            playerIndex;

        SetGamePhase(
            GamePhase.Attack);

        _players[currentPlayerIn].OnTurnStart();
    }

    [Server]
    public void EndTurn()
    {
        if (!gameStarted)
            return;

        if (_players.Count == 0)
            return;

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            Debug.LogError(
                $"GameMaster: invalid currentPlayerIn {currentPlayerIn}.");

            return;
        }

        _players[currentPlayerIn].OnTurnEnd();

        BeginTurn(
            (currentPlayerIn + 1) %
            _players.Count);
    }

    // =========================================================
    // TIMEOUT
    // =========================================================

    [Server]
    private IEnumerator LoadTimeoutRoutine()
    {
        yield return new WaitForSeconds(
            loadTimeout);

        timeoutRoutine = null;

        if (gameStarted ||
            startupCancelled)
        {
            yield break;
        }

        List<int> missing =
            expectedConnections
                .Where(
                    id => !loadedConnections.Contains(id))
                .ToList();

        if (missing.Count > 0)
        {
            CancelStartup(
                $"Loading timeout. Missing connections: {string.Join(", ", missing)}");

            yield break;
        }

        TryStartGame();

        yield return null;

        if (!gameStarted &&
            !startupCancelled)
        {
            CancelStartup(
                "All players reported loaded, but their Player objects were not ready.");
        }
    }

    // =========================================================
    // CANCEL
    // =========================================================

    [Server]
    private void CancelStartup(
        string reason)
    {
        if (startupCancelled)
            return;

        startupCancelled = true;

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
            return;

        if (NetworkManager.singleton.mode ==
            NetworkManagerMode.Host)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopServer();
        }
    }

    // =========================================================
    // ACCESSORY
    // =========================================================

    [Server]
    public bool TryStealAccesory(Player target, int index)
    {
        if (currentPhase != GamePhase.Steal)
            return false;
        if (target == null)
            return false;

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            Debug.LogWarning(
                $"Invalid currentPlayerIn: {currentPlayerIn}");

            return false;
        }

        Player thief =
            _players[currentPlayerIn];

        if (thief == null)
            return false;

        if (thief == target)
            return false;

        if (!target.SetAccesory(
                index,
                false))
        {
            return false;
        }

        if (!thief.SetAccesory(
                index,
                true))
        {
            target.SetAccesory(
                index,
                true);

            return false;
        }

        EndTurn(); // TEMPORAL

        return true;
    }
}