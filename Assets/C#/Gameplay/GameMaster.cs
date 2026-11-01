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
    [SyncVar] private NetworkIdentity selectedTargetIdentity;

    private float turnStartTime;
    private bool lastTurnPressure;
    [SerializeField] private float turnPressureThreshold = 20f;

    private bool gameStarted;
    private bool startupCancelled;

    // ---- test harness support (TestScene) ----

    /// <summary>
    /// While true the match is NOT started automatically when all
    /// connections have loaded. The test scene starts it by hand.
    /// </summary>
    public bool HoldStart { get; set; }

    public bool IsMatchRunning =>
        gameStarted;

    /// <summary>Increments on every BeginTurn. 0 = no turn yet.</summary>
    public int TurnNumber { get; private set; }

    public Card SelectedAttack =>
        selectedAttack;

    private Coroutine timeoutRoutine;
    private Coroutine startupRoutine;
    private Coroutine idleAudioRoutine;

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

    private void SetGamePhase(
        GamePhase phase)
    {
        if (!isServer)
            return;

        if (currentPhase == phase)
            return;

        GameLog.Event(
            LogCat.Phase,
            $"{currentPhase} -> {phase}");

        currentPhase = phase;
    }

    private void Update()
    {
        UpdateMusicTension();
    }

    private void UpdateMusicTension()
    {
        if (!gameStarted || GameAudioManager.Instance == null)
            return;

        if (currentPhase == GamePhase.Attack)
        {
            bool isLowHp = IsAnyPlayerLowHealth();
            bool isThinkingTooLong = turnStartTime > 0f && (Time.time - turnStartTime) >= turnPressureThreshold;
            bool shouldPressure = isLowHp || isThinkingTooLong;

            if (shouldPressure != lastTurnPressure)
            {
                lastTurnPressure = shouldPressure;
                GameAudioManager.Instance.SetGameStateAudio(currentPhase, false, shouldPressure);
            }
        }
        else
        {
            lastTurnPressure = false;
        }
    }

    public bool IsAnyPlayerLowHealth()
    {
        if (_players == null || _players.Count == 0)
            return false;

        for (int i = 0; i < _players.Count; i++)
        {
            Player p = _players[i];
            if (p != null && !p.HasAnyAccessory)
                return true;
        }
        return false;
    }

    private void OnGamePhaseChanged(
        GamePhase oldPhase,
        GamePhase newPhase)
    {
        GamePhaseChanged?.Invoke(
            newPhase);

        bool isLowHp = IsAnyPlayerLowHealth();
        bool isThinkingTooLong = turnStartTime > 0f && (Time.time - turnStartTime) >= turnPressureThreshold;
        bool shouldPressure = isLowHp || isThinkingTooLong;

        GameAudioManager.Instance?.SetGameStateAudio(newPhase, IsLocalPlayerTargeted(), shouldPressure);
    }

    private void OnCurrentPlayerChanged(
        int oldPlayerIndex,
        int newPlayerIndex)
    {
        CurrentPlayerChanged?.Invoke(
            newPlayerIndex);
        // next_sound & coin_toss disabled (misc / coin)
        // coin_toss disabled
    }

    public bool IsLocalPlayerTargeted()
    {
        if (NetworkClient.localPlayer == null)
            return false;

        if (selectedTargetIdentity != null)
            return selectedTargetIdentity == NetworkClient.localPlayer;

        if (selectedTarget != null && selectedTarget.netIdentity != null)
            return selectedTarget.netIdentity == NetworkClient.localPlayer;

        return false;
    }

    [ClientRpc]
    private void RpcOnGameStarted()
    {
        bool isLowHp = IsAnyPlayerLowHealth();
        bool isThinkingTooLong = turnStartTime > 0f && (Time.time - turnStartTime) >= turnPressureThreshold;
        bool shouldPressure = isLowHp || isThinkingTooLong;

        GameAudioManager.Instance?.SetGameStateAudio(currentPhase, IsLocalPlayerTargeted(), shouldPressure);
        // cards_shuffle disabled (misc)
        // next_stage disabled until stage transitions exist in scene
    }

    [ClientRpc]
    private void RpcPlayPlayerReaction(
        NetworkIdentity speakerIdentity,
        int reactionType,
        float delay)
    {
        GameAudioManager.Instance?.PlayPlayerReactionClip(
            speakerIdentity,
            (GameAudioManager.ReactionClipType)reactionType,
            delay);
    }

    [Server]
    private IEnumerator IdleAudioRoutine()
    {
        GameAudioManager.ReactionClipType[] idleClips =
        {
            GameAudioManager.ReactionClipType.Burp,
            GameAudioManager.ReactionClipType.Caugh,
            GameAudioManager.ReactionClipType.CreakingChair,
            GameAudioManager.ReactionClipType.Exhale
        };

        while (gameStarted && !startupCancelled)
        {
            yield return new WaitForSeconds(30f);

            if (!gameStarted || startupCancelled || _players.Count == 0)
                continue;

            if (Random.value > 0.33f)
                continue;

            List<Player> validPlayers = _players.Where(p => p != null && p.netIdentity != null).ToList();
            if (validPlayers.Count == 0)
                continue;

            Player speaker = validPlayers[Random.Range(0, validPlayers.Count)];
            GameAudioManager.ReactionClipType chosenClip = idleClips[Random.Range(0, idleClips.Length)];
            RpcPlayPlayerReaction(speaker.netIdentity, (int)chosenClip, 0f);
        }

        idleAudioRoutine = null;
    }

    [Server]
    public void TryTriggerGoodReaction(Player player, float delay = 0.5f)
    {
        if (player == null || player.netIdentity == null)
            return;

        if (Random.value > 0.30f)
            return;

        GameAudioManager.ReactionClipType[] goodClips =
        {
            GameAudioManager.ReactionClipType.Joyful,
            GameAudioManager.ReactionClipType.Whistle,
            GameAudioManager.ReactionClipType.Wonderment
        };

        GameAudioManager.ReactionClipType chosen = goodClips[Random.Range(0, goodClips.Length)];
        float actualDelay = Mathf.Max(0.1f, delay + Random.Range(-0.05f, 0.05f));
        RpcPlayPlayerReaction(player.netIdentity, (int)chosen, actualDelay);
    }

    [Server]
    public void TryTriggerBadReaction(Player player, float delay = 0.5f)
    {
        if (player == null || player.netIdentity == null)
            return;

        if (Random.value > 0.33f)
            return;

        float actualDelay = Mathf.Max(0.1f, delay + Random.Range(-0.05f, 0.05f));
        RpcPlayPlayerReaction(
            player.netIdentity,
            (int)GameAudioManager.ReactionClipType.Disappointment,
            actualDelay);
    }

    [Server]
    public void TryTriggerOtherPlayerScoff(Player failedPlayer, float delay = 0.5f)
    {
        if (failedPlayer == null || _players.Count <= 1)
            return;

        if (Random.value > 0.33f)
            return;

        List<Player> otherPlayers = _players
            .Where(p => p != null && p != failedPlayer && p.netIdentity != null)
            .ToList();

        if (otherPlayers.Count == 0)
            return;

        Player scoffer = otherPlayers[Random.Range(0, otherPlayers.Count)];
        float actualDelay = Mathf.Max(0.1f, delay + Random.Range(-0.05f, 0.05f));
        RpcPlayPlayerReaction(
            scoffer.netIdentity,
            (int)GameAudioManager.ReactionClipType.Scoff,
            actualDelay);
    }

    private void UnusedMarker()
    {
    }

    public override void OnStopClient()
    {
        GameAudioManager.Instance?.StopAllMusic();
        base.OnStopClient();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            GameAudioManager.Instance?.StopAllMusic();
            Instance = null;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        expectedConnections.Clear();
        loadedConnections.Clear();
        _players.Clear();

        gameStarted = false;
        startupCancelled = false;
        currentPlayerIn = -1;

        selectedAttack = null;
        selectedTarget = null;
        selectedTargetIdentity = null;
        turnStartTime = Time.time;
        lastTurnPressure = false;

        currentPhase = GamePhase.Wait;

        StartCoroutine(
            InitializeLobbyConnections());
    }

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
    }

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

        TryStartGame();
    }

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

        InitializeMatch(
            players);
    }

    [Server]
    private void InitializeMatch(
        List<Player> players)
    {
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

        BeginTurn(
            Random.Range(
                0,
                _players.Count));
    }

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

        TurnNumber++;
        GameLog.Turn = TurnNumber;

        GameLog.Event(
            LogCat.Turn,
            $"TURN {TurnNumber} BEGIN: current = " +
            GameLog.P(_players[playerIndex]));

        selectedAttack = null;
        selectedTarget = null;
        selectedTargetIdentity = null;
        turnStartTime = Time.time;
        lastTurnPressure = false;

        SetGamePhase(
            GamePhase.Attack);

        _players[currentPlayerIn].OnTurnStart();
        RpcOnGameStarted();
        if (idleAudioRoutine == null)
            idleAudioRoutine = StartCoroutine(IdleAudioRoutine());
    }

    [Server]
    public void EndTurn(
        [System.Runtime.CompilerServices.CallerMemberName] string caller = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    {
        if (!gameStarted)
            return;

        GameLog.Event(
            LogCat.Turn,
            $"TURN {TurnNumber} END requested by {caller}:{line}");

        if (_players.Count == 0)
            return;

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            Debug.LogError(
                $"GameMaster: invalid currentPlayerIn {currentPlayerIn}.");

            return;
        }

        selectedAttack = null;
        selectedTarget = null;
        selectedTargetIdentity = null;
        turnStartTime = Time.time;
        lastTurnPressure = false;

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
        if (!gameStarted)
            return false;

        if (currentPhase !=
            GamePhase.Attack)
        {
            return false;
        }

        if (card == null ||
            player == null)
        {
            return false;
        }

        if (!IsCurrentPlayer(player))
            return false;

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            return false;
        }

        if (card.OwnerNetId !=
            player.netId)
        {
            return false;
        }

        if (card.Data == null)
            return false;

        if (card.Data.CardClass !=
                Card.CardClass.AttackLight &&
            card.Data.CardClass !=
                Card.CardClass.AttackSteal)
        {
            return false;
        }

        selectedAttack = card;

        SetGamePhase(
            GamePhase.SelectTarget);

        return true;
    }

    [Server]
    public bool SelectTarget(
        Player player)
    {
        if (!gameStarted)
            return false;

        if (currentPhase !=
            GamePhase.SelectTarget)
        {
            return false;
        }

        if (selectedAttack == null ||
            player == null)
        {
            return false;
        }

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            return false;
        }

        Player attacker =
            _players[currentPlayerIn];

        if (player == attacker)
            return false;

        if (!_players.Contains(player))
            return false;

        selectedTarget = player;
        selectedTargetIdentity = player != null ? player.netIdentity : null;

        SetGamePhase(
            GamePhase.Defense);

        return true;
    }

    [Server]
    public bool SelectDefense(
        Card card,
        Player player)
    {
        if (!gameStarted)
            return false;

        if (currentPhase !=
            GamePhase.Defense)
        {
            return false;
        }

        if (selectedAttack == null ||
            selectedTarget == null)
        {
            return false;
        }

        if (player == null ||
            player != selectedTarget)
        {
            return false;
        }

        if (card == null)
            return false;

        if (card.OwnerNetId !=
            player.netId)
        {
            return false;
        }

        if (card.IsInRoulette ||
            !card.IsAvailable)
        {
            return false;
        }

        if (card.Data == null ||
            card.Data.CardClass !=
                Card.CardClass.Defence)
        {
            return false;
        }

        return true;
    }

    [Server]
    public bool SkipDefense(
        Player player)
    {
        if (!gameStarted)
            return false;

        if (currentPhase !=
            GamePhase.Defense)
        {
            return false;
        }

        if (selectedTarget == null ||
            player == null)
        {
            return false;
        }

        if (player != selectedTarget)
            return false;

        return true;
    }

    [Server]
    public bool BeginStealPhase()
    {
        if (!gameStarted)
            return false;

        if (currentPhase !=
            GamePhase.Defense)
        {
            return false;
        }

        if (selectedAttack == null ||
            selectedTarget == null)
        {
            return false;
        }

        if (selectedAttack.Data == null ||
            selectedAttack.Data.CardClass !=
                Card.CardClass.AttackSteal)
        {
            return false;
        }

        SetGamePhase(
            GamePhase.Steal);

        return true;
    }

    [Server]
    public bool TryStealAccesory(
        Player target,
        int index,
        NetworkConnectionToClient sender)
    {
        if (sender == null || sender.identity == null)
            return false;

        Player requester = sender.identity.GetComponent<Player>();
        return TryStealAccesory(target, index, requester);
    }

    [Server]
    public bool TryStealAccesory(
        Player target,
        int index,
        Player requester)
    {
        if (!gameStarted)
            return false;

        if (currentPhase !=
            GamePhase.Steal)
        {
            return false;
        }

        if (requester == null)
            return false;

        if (selectedTarget == null ||
            target == null ||
            target != selectedTarget)
        {
            return false;
        }

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            return false;
        }

        Player thief =
            _players[currentPlayerIn];

        if (thief == null ||
            thief == target)
        {
            return false;
        }

        if (thief != requester)
        {
            return false;
        }

        if (!target.HasAccessory(index))
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

        GameLog.Event(
            LogCat.Steal,
            $"STEAL OK: accessory {index} " +
            $"{GameLog.P(target)} -> {GameLog.P(thief)}");

        TryTriggerGoodReaction(thief, 0.5f);
        TryTriggerBadReaction(target, 0.65f);
        EndTurn();

        return true;
    }

    // =========================================================
    // TEST SCENE API
    // =========================================================

    /// <summary>
    /// Starts a match with exactly these players (humans and bots),
    /// ignoring network connections. Used by the test scene.
    /// </summary>
    [Server]
    public bool StartTestGame(
        IList<Player> players)
    {
        if (gameStarted)
        {
            GameLog.Warn(
                LogCat.Match,
                "StartTestGame: a match is already running.");

            return false;
        }

        if (players == null ||
            players.Count == 0 ||
            players.Count > 5 ||
            players.Any(p => p == null))
        {
            GameLog.Warn(
                LogCat.Match,
                "StartTestGame: need 1..5 valid players.");

            return false;
        }

        if (spawnpointCalculator == null)
        {
            GameLog.Error(
                LogCat.Match,
                "StartTestGame: SpawnpointCalculator missing.");

            return false;
        }

        spawnpointCalculator.CalculateSpawnPoints(
            players.Count);

        if (spawnpointCalculator.SpawnPoints.Count <
            players.Count)
        {
            GameLog.Error(
                LogCat.Match,
                "StartTestGame: not enough spawn points.");

            return false;
        }

        startupCancelled = false;

        InitializeMatch(
            new List<Player>(players));

        return gameStarted;
    }

    /// <summary>Stops the match and destroys all cards.</summary>
    [Server]
    public void ResetMatch()
    {
        GameLog.Event(
            LogCat.Match,
            "RESET MATCH");

        if (idleAudioRoutine != null)
        {
            StopCoroutine(idleAudioRoutine);
            idleAudioRoutine = null;
        }

        if (AttackSequence.Instance != null)
            AttackSequence.Instance.ServerForceReset();

        foreach (Card card
                 in FindObjectsByType<Card>())
        {
            if (card != null)
                NetworkServer.Destroy(card.gameObject);
        }

        foreach (Player player in _players)
        {
            if (player != null)
                player.OnTurnEnd();
        }

        _players.Clear();

        gameStarted = false;
        currentPlayerIn = -1;

        selectedAttack = null;
        selectedTarget = null;
        selectedTargetIdentity = null;
        turnStartTime = Time.time;
        lastTurnPressure = false;

        TurnNumber = 0;
        GameLog.Turn = 0;

        SetGamePhase(
            GamePhase.Wait);
    }

    /// <summary>Test only: jump to any phase.</summary>
    [Server]
    public void DebugForcePhase(
        GamePhase phase)
    {
        GameLog.Warn(
            LogCat.Test,
            $"DebugForcePhase -> {phase}");

        SetGamePhase(
            phase);
    }

    [Server]
    private bool IsCurrentPlayer(
        Player player)
    {
        if (player == null)
            return false;

        if (currentPlayerIn < 0 ||
            currentPlayerIn >= _players.Count)
        {
            return false;
        }

        return _players[currentPlayerIn] ==
               player;
    }

    [Server]
    private IEnumerator LoadTimeoutRoutine()
    {
        yield return new WaitForSeconds(
            loadTimeout);

        timeoutRoutine = null;

        if (HoldStart)
            yield break;

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

    [Server]
    private void ClearTurnState()
    {
        selectedAttack = null;
        selectedTarget = null;
        selectedTargetIdentity = null;
        turnStartTime = Time.time;
        lastTurnPressure = false;
    }
}