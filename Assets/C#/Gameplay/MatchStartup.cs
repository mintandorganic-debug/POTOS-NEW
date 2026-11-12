using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchStartup
{
    private readonly MonoBehaviour runner;
    private readonly float loadTimeout;
    private readonly Func<bool> isGameStarted;
    private readonly Action<List<Player>> startGame;

    private readonly HashSet<int> expectedConnections = new();
    private readonly HashSet<int> loadedConnections = new();

    public bool HoldStart { get; set; }

    private bool startupCancelled;

    private Coroutine timeoutRoutine;
    private Coroutine startupRoutine;

    public MatchStartup(
        MonoBehaviour runner,
        float loadTimeout,
        Func<bool> isGameStarted,
        Action<List<Player>> startGame)
    {
        this.runner = runner;
        this.loadTimeout = loadTimeout;
        this.isGameStarted = isGameStarted;
        this.startGame = startGame;

        GameLog.Write(
            "MatchStartup",
            $"Created runner={(runner != null ? runner.name : "NULL")} loadTimeout={loadTimeout} expected=0 loaded=0 startupCancelled=false");
    }

    public bool IsCancelled =>
        startupCancelled;

    public void Begin()
    {
        GameLog.Write(
            "MatchStartup",
            $"Begin starting InitializeLobbyConnections loadTimeout={loadTimeout}");

        runner.StartCoroutine(
            InitializeLobbyConnections());
    }

    public void StopLoadTimeout()
    {
        GameLog.Write(
            "MatchStartup",
            $"StopLoadTimeout timerRunning={timeoutRoutine != null}");

        if (timeoutRoutine != null)
        {
            runner.StopCoroutine(
                timeoutRoutine);

            timeoutRoutine = null;
        }
    }

    private IEnumerator InitializeLobbyConnections()
    {
        GameLog.Write(
            "MatchStartup",
            "InitializeLobbyConnections start");

        yield return null;

        while (!isGameStarted() &&
               !startupCancelled)
        {
            CaptureLobbyConnections();

            if (expectedConnections.Count > 0)
                break;

            yield return null;
        }

        GameLog.Write(
            "MatchStartup",
            $"InitializeLobbyConnections lobby captured expected={expectedConnections.Count} gameStarted={isGameStarted()} startupCancelled={startupCancelled}");

        timeoutRoutine =
            runner.StartCoroutine(
                LoadTimeoutRoutine());

        GameLog.Write(
            "MatchStartup",
            $"InitializeLobbyConnections load timeout timer started loadTimeout={loadTimeout}");

        StartWhenReady();

        GameLog.Write(
            "MatchStartup",
            "InitializeLobbyConnections end");
    }

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
                "MatchStartup",
                $"CaptureLobbyConnections expected={expectedConnections.Count} ids={string.Join(",", expectedConnections)}");
        }
    }

    public void PlayerLoaded(
        NetworkConnectionToClient conn)
    {
        GameLog.Write(
            "MatchStartup",
            $"PlayerLoaded conn={(conn != null ? conn.connectionId.ToString() : "NULL")} startupCancelled={startupCancelled} gameStarted={isGameStarted()} loaded={loadedConnections.Count} expected={expectedConnections.Count}");

        if (startupCancelled ||
            isGameStarted())
        {
            GameLog.Write(
                "MatchStartup",
                $"PlayerLoaded ignored: startupCancelled={startupCancelled} gameStarted={isGameStarted()}");

            return;
        }

        if (conn == null)
        {
            GameLog.Write(
                "MatchStartup",
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
                "MatchStartup",
                $"PlayerLoaded rejected: connection {connectionId} is not part of the captured lobby");

            Debug.LogWarning(
                $"GameMaster.PlayerLoaded: connection {connectionId} is not part of the captured lobby.");

            return;
        }

        loadedConnections.Add(
            connectionId);

        GameLog.Write(
            "MatchStartup",
            $"PlayerLoaded connection {connectionId} marked loaded loaded={loadedConnections.Count} expected={expectedConnections.Count}");

        StartWhenReady();
    }

    private void StartWhenReady()
    {
        if (HoldStart)
        {
            GameLog.Write(
                "MatchStartup",
                "StartWhenReady skipped: HoldStart is true");
            return;
        }

        GameLog.Write(
            "MatchStartup",
            $"StartWhenReady gameStarted={isGameStarted()} startupCancelled={startupCancelled} expected={expectedConnections.Count} loaded={loadedConnections.Count} startupRoutineRunning={startupRoutine != null}");

        if (isGameStarted() ||
            startupCancelled)
        {
            GameLog.Write(
                "MatchStartup",
                "StartWhenReady skipped: game already started or startup cancelled");

            return;
        }

        if (expectedConnections.Count == 0)
        {
            GameLog.Write(
                "MatchStartup",
                "StartWhenReady skipped: no expected connections");

            return;
        }

        if (loadedConnections.Count !=
            expectedConnections.Count)
        {
            GameLog.Write(
                "MatchStartup",
                $"StartWhenReady waiting: loaded={loadedConnections.Count} expected={expectedConnections.Count}");

            return;
        }

        foreach (int connectionId
                 in expectedConnections)
        {
            if (!loadedConnections.Contains(
                    connectionId))
            {
                GameLog.Write(
                    "MatchStartup",
                    $"StartWhenReady waiting: connection {connectionId} not loaded");

                return;
            }

            if (!NetworkServer.connections.TryGetValue(
                    connectionId,
                    out NetworkConnectionToClient conn))
            {
                GameLog.Write(
                    "MatchStartup",
                    $"StartWhenReady waiting: connection {connectionId} not found in NetworkServer.connections");

                return;
            }

            if (conn == null ||
                conn.identity == null)
            {
                GameLog.Write(
                    "MatchStartup",
                    $"StartWhenReady waiting: connection {connectionId} has no identity yet");

                return;
            }

            if (!conn.identity.TryGetComponent<Player>(
                    out Player player))
            {
                GameLog.Write(
                    "MatchStartup",
                    $"StartWhenReady waiting: connection {connectionId} identity has no Player component");

                return;
            }

            if (player == null)
            {
                GameLog.Write(
                    "MatchStartup",
                    $"StartWhenReady waiting: connection {connectionId} Player is null");

                return;
            }
        }

        if (startupRoutine != null)
        {
            GameLog.Write(
                "MatchStartup",
                "StartWhenReady skipped: startupRoutine already running");

            return;
        }

        GameLog.Write(
            "MatchStartup",
            "StartWhenReady all players ready, starting StartGameRoutine");

        startupRoutine =
            runner.StartCoroutine(
                StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        GameLog.Write(
            "MatchStartup",
            "StartGameRoutine start");

        startupRoutine = null;

        yield return null;

        if (isGameStarted() ||
            startupCancelled)
        {
            GameLog.Write(
                "MatchStartup",
                $"StartGameRoutine end: aborted gameStarted={isGameStarted()} startupCancelled={startupCancelled}");

            yield break;
        }

        List<Player> players =
            BuildPlayerList();

        if (players == null ||
            players.Count == 0)
        {
            GameLog.Write(
                "MatchStartup",
                $"StartGameRoutine end: no players built playersNull={players == null}");

            yield break;
        }

        if (players.Count !=
            expectedConnections.Count)
        {
            GameLog.Write(
                "MatchStartup",
                $"StartGameRoutine end: player count changed players={players.Count} expected={expectedConnections.Count}");

            Debug.LogWarning(
                "GameMaster: player count changed before startup.");

            yield break;
        }

        if (!AreAllExpectedPlayersPresent(
                players))
        {
            GameLog.Write(
                "MatchStartup",
                "StartGameRoutine end: not all expected players present");

            yield break;
        }

        GameLog.Write(
            "MatchStartup",
            $"StartGameRoutine calling StartGame players={players.Count}");

        startGame(
            players);

        GameLog.Write(
            "MatchStartup",
            "StartGameRoutine end");
    }

    private List<Player> BuildPlayerList()
    {
        GameLog.Write(
            "MatchStartup",
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
                    "MatchStartup",
                    $"BuildPlayerList null: connection {connectionId} not found");

                return null;
            }

            if (conn == null ||
                conn.identity == null)
            {
                GameLog.Write(
                    "MatchStartup",
                    $"BuildPlayerList null: connection {connectionId} has no identity");

                return null;
            }

            if (!conn.identity.TryGetComponent<Player>(
                    out Player player))
            {
                GameLog.Write(
                    "MatchStartup",
                    $"BuildPlayerList null: connection {connectionId} identity has no Player");

                return null;
            }

            if (player == null)
            {
                GameLog.Write(
                    "MatchStartup",
                    $"BuildPlayerList null: connection {connectionId} Player is null");

                return null;
            }

            players.Add(player);
        }

        GameLog.Write(
            "MatchStartup",
            $"BuildPlayerList built players={players.Count}");

        return players;
    }

    public bool AreAllExpectedPlayersPresent(
        List<Player> players)
    {
        GameLog.Write(
            "MatchStartup",
            $"AreAllExpectedPlayersPresent players={(players != null ? players.Count.ToString() : "NULL")} expected={expectedConnections.Count}");

        if (players == null)
        {
            GameLog.Write(
                "MatchStartup",
                "AreAllExpectedPlayersPresent false: players is null");

            return false;
        }

        if (players.Count !=
            expectedConnections.Count)
        {
            GameLog.Write(
                "MatchStartup",
                $"AreAllExpectedPlayersPresent false: count mismatch players={players.Count} expected={expectedConnections.Count}");

            return false;
        }

        if (players.Any(
                player => player == null))
        {
            GameLog.Write(
                "MatchStartup",
                "AreAllExpectedPlayersPresent false: list contains a null player");

            return false;
        }

        if (players.Distinct().Count() !=
            players.Count)
        {
            GameLog.Write(
                "MatchStartup",
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
                    "MatchStartup",
                    $"AreAllExpectedPlayersPresent false: player {player.name} has no netIdentity");

                return false;
            }

            NetworkConnectionToClient conn =
                player.netIdentity.connectionToClient;

            if (conn == null)
            {
                GameLog.Write(
                    "MatchStartup",
                    $"AreAllExpectedPlayersPresent false: player {player.netId} has no connectionToClient");

                return false;
            }

            playerConnections.Add(
                conn.connectionId);
        }

        GameLog.Write(
            "MatchStartup",
            $"AreAllExpectedPlayersPresent checking connection sets playerConnections={string.Join(",", playerConnections)} expected={string.Join(",", expectedConnections)}");

        return playerConnections.SetEquals(
            expectedConnections);
    }

    private IEnumerator LoadTimeoutRoutine()
    {
        GameLog.Write(
            "MatchStartup",
            $"LoadTimeoutRoutine start loadTimeout={loadTimeout}");

        yield return new WaitForSeconds(
            loadTimeout);

        timeoutRoutine = null;

        if (HoldStart)
            yield break;

        GameLog.Write(
            "MatchStartup",
            $"LoadTimeoutRoutine timeout elapsed gameStarted={isGameStarted()} startupCancelled={startupCancelled} loaded={loadedConnections.Count} expected={expectedConnections.Count}");

        if (isGameStarted() ||
            startupCancelled)
        {
            GameLog.Write(
                "MatchStartup",
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
                "MatchStartup",
                $"LoadTimeoutRoutine loading timeout, missing connections: {string.Join(", ", missing)}");

            Cancel(
                $"Loading timeout. Missing connections: {string.Join(", ", missing)}");

            yield break;
        }

        GameLog.Write(
            "MatchStartup",
            "LoadTimeoutRoutine all connections loaded, retrying StartWhenReady");

        StartWhenReady();

        yield return null;

        if (!isGameStarted() &&
            !startupCancelled)
        {
            GameLog.Write(
                "MatchStartup",
                "LoadTimeoutRoutine all players loaded but Player objects not ready");

            Cancel(
                "All players reported loaded, but their Player objects were not ready.");
        }

        GameLog.Write(
            "MatchStartup",
            "LoadTimeoutRoutine end");
    }

    public void Cancel(
        string reason)
    {
        GameLog.Write(
            "MatchStartup",
            $"Cancel reason={reason} alreadyCancelled={startupCancelled}");

        if (startupCancelled)
        {
            GameLog.Write(
                "MatchStartup",
                "Cancel skipped: already cancelled");

            return;
        }

        startupCancelled = true;

        GameLog.Write(
            "MatchStartup",
            "Cancel startupCancelled=true");

        if (timeoutRoutine != null)
        {
            runner.StopCoroutine(
                timeoutRoutine);

            timeoutRoutine = null;
        }

        if (startupRoutine != null)
        {
            runner.StopCoroutine(
                startupRoutine);

            startupRoutine = null;
        }

        Debug.LogError(
            $"Game startup cancelled: {reason}");

        if (NetworkManager.singleton == null)
        {
            GameLog.Write(
                "MatchStartup",
                "Cancel: NetworkManager.singleton is null, nothing to stop");

            return;
        }

        if (NetworkManager.singleton.mode ==
            NetworkManagerMode.Host)
        {
            GameLog.Write(
                "MatchStartup",
                "Cancel stopping host");

            NetworkManager.singleton.StopHost();
        }
        else
        {
            GameLog.Write(
                "MatchStartup",
                "Cancel stopping server");

            NetworkManager.singleton.StopServer();
        }
    }
}
