using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameMaster : NetworkBehaviour, IAttackSelectionContext, IAccessoryTheftContext, ITurnContext, ITurnStageContext, IDiceStageContext
{
    public enum GamePhase
    {
        Wait,
        Attack,
        SelectTarget,
        Defense,
        Steal,
        Dice
    }

    [SerializeField] private SpawnpointCalculator spawnpointCalculator;
    [SerializeField] private float loadTimeout = 30f;

    public static GameMaster Instance;

    public event Action<GamePhase> GamePhaseChanged;
    public event Action<int> CurrentPlayerChanged;
    public event Action<TurnStage> StageChanged;

    public static event Action<uint, float> DiceStarted;
    public static event Action<uint, int, int, int, string, int, int, float> DiceRolled;

    [SyncVar(hook = nameof(OnStageHookChanged))]
    [SerializeField] private TurnStage currentStage = TurnStage.CardAttack;

    public TurnStage CurrentStage =>
        currentStage;

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

    public Card SelectedAttack =>
        selection != null ? selection.Attack : null;

    public bool IsMatchRunning =>
        gameStarted;

    public bool HoldStart
    {
        get => startup != null && startup.HoldStart;
        set { if (startup != null) startup.HoldStart = value; }
    }

    public int TurnNumber { get; private set; }

    private readonly List<Player> _players = new();

    public IReadOnlyList<Player> Players =>
        _players;

    private AttackSelection selection;
    private AccessoryTheft theft;
    private TurnController turns;
    private TurnStageSequence stages;
    private IDiceThrowHandler diceThrow;

    private bool gameStarted;

    private MatchStartup startup;
    private MatchSetup setup;

    private Coroutine idleAudioRoutine;
    [SerializeField] private float turnPressureThreshold = 20f;
    private float turnStartTime;
    private bool lastTurnPressure;

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

        gameObject.AddComponent<DiceStageView>();

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

        float elapsed = Time.time - turnStartTime;
        bool shouldPressure = elapsed >= turnPressureThreshold;
        GameAudioManager.Instance?.SetGameStateAudio(newPhase, IsLocalPlayerTargeted(), shouldPressure);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        GameAudioManager.Instance?.StopAllMusic();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        GameAudioManager.Instance?.StopAllMusic();
    }

    private void Update()
    {
        UpdateTurnAudio();
    }

    private void UpdateTurnAudio()
    {
        if (!gameStarted || GameAudioManager.Instance == null)
            return;

        if (currentPhase == GamePhase.Wait)
            return;

        float elapsed = Time.time - turnStartTime;
        bool shouldPressure = elapsed >= turnPressureThreshold;

        if (shouldPressure != lastTurnPressure)
        {
            lastTurnPressure = shouldPressure;
            GameAudioManager.Instance.SetGameStateAudio(currentPhase, IsLocalPlayerTargeted(), shouldPressure);
        }
    }

    private bool IsLocalPlayerTargeted()
    {
        Player target = SelectedTarget;
        if (target == null)
            return false;

        Player local = Player.LocalPlayer;
        if (local == null)
            return false;

        return target == local;
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

        turns =
            new TurnController(
                this,
                selection);

        DiceStage dice =
            new DiceStage(
                this,
                this,
                DiceCombinationEvaluator.CreateDefault(),
                new PlayerBankPayout(),
                5f,
                3f);

        diceThrow = dice;

        stages =
            new TurnStageSequence(
                this,
                new ITurnStage[]
                {
                    new CardAttackStage(),
                    dice,
                    new SlotStage()
                });

        turns.TurnBegan +=
            player =>
            {
                stages.Begin(
                    player);

                turnStartTime = Time.time;
                lastTurnPressure = false;
                TurnNumber++;
                GameLog.Turn = TurnNumber;

                if (idleAudioRoutine == null)
                    idleAudioRoutine = StartCoroutine(IdleAudioRoutine());
            };

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
        turns.BeginTurn(
            playerIndex);
    }

    [Server]
    public void EndTurn()
    {
        if (idleAudioRoutine != null)
        {
            StopCoroutine(idleAudioRoutine);
            idleAudioRoutine = null;
        }

        if (stages != null &&
            stages.IsActive)
        {
            stages.TrySkipCurrent();

            return;
        }

        turns.EndTurn();
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

        if (currentPhase != GamePhase.Steal)
            return false;

        if (requester == null || target == null || target != SelectedTarget)
            return false;

        if (currentPlayerIn < 0 || currentPlayerIn >= _players.Count)
            return false;

        Player thief = _players[currentPlayerIn];
        if (thief == null || thief != requester || thief == target)
            return false;

        if (!target.HasAccessory(index))
            return false;

        if (!target.SetAccesory(index, false))
            return false;

        if (!thief.SetAccesory(index, true))
        {
            target.SetAccesory(index, true);
            return false;
        }

        TryTriggerGoodReaction(thief, 0.5f);
        TryTriggerBadReaction(target, 0.65f);

        EndTurn();
        return true;
    }

    [ClientRpc]
    private void RpcPlayPlayerReaction(
        NetworkIdentity playerNetIdentity,
        int reactionType,
        float delay)
    {
        GameAudioManager.Instance?.PlayPlayerReactionClip(
            (GameAudioManager.ReactionClipType)reactionType,
            playerNetIdentity,
            delay);
    }

    private IEnumerator IdleAudioRoutine()
    {
        GameAudioManager.ReactionClipType[] idleClips = new[]
        {
            GameAudioManager.ReactionClipType.Burp,
            GameAudioManager.ReactionClipType.Caugh,
            GameAudioManager.ReactionClipType.CreakingChair,
            GameAudioManager.ReactionClipType.Exhale
        };

        while (gameStarted)
        {
            yield return new WaitForSeconds(Random.Range(20f, 40f));

            if (!gameStarted || _players.Count == 0)
                break;

            Player speaker = _players[Random.Range(0, _players.Count)];
            if (speaker != null && speaker.netIdentity != null)
            {
                GameAudioManager.ReactionClipType chosenClip = idleClips[Random.Range(0, idleClips.Length)];
                RpcPlayPlayerReaction(speaker.netIdentity, (int)chosenClip, 0f);
            }
        }

        idleAudioRoutine = null;
    }

    public void TryTriggerGoodReaction(Player player, float delay = 0.5f)
    {
        if (player == null || player.netIdentity == null) return;
        GameAudioManager.ReactionClipType[] goodClips = new[]
        {
            GameAudioManager.ReactionClipType.Joyful,
            GameAudioManager.ReactionClipType.Whistle,
            GameAudioManager.ReactionClipType.Wonderment
        };
        GameAudioManager.ReactionClipType chosen = goodClips[Random.Range(0, goodClips.Length)];
        RpcPlayPlayerReaction(player.netIdentity, (int)chosen, delay);
    }

    public void TryTriggerBadReaction(Player player, float delay = 0.5f)
    {
        if (player == null || player.netIdentity == null) return;
        RpcPlayPlayerReaction(player.netIdentity, (int)GameAudioManager.ReactionClipType.Disappointment, delay);
    }

    public void TryTriggerOtherPlayerScoff(Player failedPlayer, float delay = 0.5f)
    {
        if (_players.Count <= 1) return;
        List<Player> otherPlayers = _players.Where(p => p != null && p != failedPlayer && p.netIdentity != null).ToList();
        if (otherPlayers.Count == 0) return;
        Player scoffer = otherPlayers[Random.Range(0, otherPlayers.Count)];
        RpcPlayPlayerReaction(scoffer.netIdentity, (int)GameAudioManager.ReactionClipType.Scoff, delay);
    }

    public void TryTriggerDisappointmentSound(Player player, float delay = 0.5f)
    {
        TryTriggerBadReaction(player, delay);
    }

    // =========================================================
    // TEST HARNESS / BOT METHODS
    // =========================================================

    [Server]
    public bool StartTestGame(List<Player> players)
    {
        if (gameStarted) return false;
        StartGame(players);
        return gameStarted;
    }

    [Server]
    public void DebugForcePhase(GamePhase phase)
    {
        SetGamePhase(phase);
    }

    [Server]
    public void ResetMatch()
    {
        gameStarted = false;
        currentPlayerIn = -1;
        currentPhase = GamePhase.Wait;
        if (idleAudioRoutine != null)
        {
            StopCoroutine(idleAudioRoutine);
            idleAudioRoutine = null;
        }
    }

    bool IAttackSelectionContext.GameStarted =>
        gameStarted;

    GamePhase IAttackSelectionContext.Phase =>
        currentPhase;

    bool IAttackSelectionContext.IsCurrentPlayer(
        Player player)
    {
        return turns.IsCurrentPlayer(
            player);
    }

    void IAttackSelectionContext.SetPhase(
        GamePhase phase)
    {
        SetGamePhase(
            phase);
    }

    void ITurnContext.SetCurrentPlayer(
        int index)
    {
        currentPlayerIn = index;
    }

    private void OnStageHookChanged(
        TurnStage oldStage,
        TurnStage newStage)
    {
        GameLog.Write(
            "GameMaster",
            $"OnStageHookChanged {oldStage} -> {newStage} isServer={isServer} isClient={isClient}");

        StageChanged?.Invoke(
            newStage);
    }

    [Command(requiresAuthority = false)]
    public void CmdThrowDice(
        float power,
        NetworkConnectionToClient sender = null)
    {
        GameLog.Write(
            "GameMaster",
            $"CmdThrowDice sender={(sender != null ? sender.connectionId : -1)} power={power}");

        if (sender == null ||
            diceThrow == null)
        {
            return;
        }

        Player thrower = null;

        foreach (Player player in _players)
        {
            if (player != null &&
                player.netIdentity != null &&
                player.netIdentity.connectionToClient == sender)
            {
                thrower = player;
                break;
            }
        }

        if (thrower == null)
        {
            GameLog.Write(
                "GameMaster",
                $"CmdThrowDice rejected: no player for connection={sender.connectionId}");

            return;
        }

        diceThrow.RequestThrow(
            thrower,
            power);
    }

    [ClientRpc]
    private void RpcDiceStarted(
        uint playerNetId,
        float duration)
    {
        DiceStarted?.Invoke(
            playerNetId,
            duration);
    }

    [ClientRpc]
    private void RpcDiceRolled(
        uint playerNetId,
        int first,
        int second,
        int third,
        string combination,
        int chips,
        int coins,
        float power)
    {
        DiceRolled?.Invoke(
            playerNetId,
            first,
            second,
            third,
            combination,
            chips,
            coins,
            power);
    }

    void ITurnStageContext.SetStage(
        TurnStage stage)
    {
        currentStage = stage;
    }

    void ITurnStageContext.AdvanceTurn()
    {
        turns.EndTurn();
    }

    void IDiceStageContext.SetPhase(
        GamePhase phase)
    {
        SetGamePhase(
            phase);
    }

    void IDiceStageContext.NotifyDiceStarted(
        uint playerNetId,
        float duration)
    {
        RpcDiceStarted(
            playerNetId,
            duration);
    }

    void IDiceStageContext.NotifyDiceRolled(
        uint playerNetId,
        int first,
        int second,
        int third,
        string combination,
        int chips,
        int coins,
        float power)
    {
        RpcDiceRolled(
            playerNetId,
            first,
            second,
            third,
            combination,
            chips,
            coins,
            power);
    }
}
