using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using kcp2k;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The test scene panel (IMGUI, no prefabs needed). Press F1 to show / hide it.
///
///   SESSION  host / join, bots, start / reset the match, speed, timers
///   MANUAL   call any game function as any player
///   AUTO     automatic test: bots play by themselves at high speed
///   SOUND    trigger every character sound
///   LOG      live event log (also saved to a file)
///
/// Manual mode lets you play alone in an empty room, with bots (dumb or smart),
/// or with other testers who join with the IP of the host.
/// </summary>
public class TestSceneController : MonoBehaviour
{
    private enum Tab
    {
        Session,
        Manual,
        Auto,
        Sound,
        Log
    }

    [SerializeField] private bool autoHostOnStart = true;
    [SerializeField] private bool panelOpenOnStart = true;

    private TestNetworkManager net;
    private BotDirector bots;
    private AutoTester auto;

    private Tab tab = Tab.Session;
    private bool panelOpen;
    private Vector2 scroll;
    private Vector2 logScroll;
    private Vector2 autoScroll;

    private string ipText = "localhost";
    private string portText = "7777";
    private string message = string.Empty;

    private int actIndex;
    private int targetIndex;

    private float selectTime = 10f;
    private float defenseTime = 5f;
    private float stealTime = 5f;

    // sound tab - manual music layers
    private float ambientLayerVol = 0.45f;
    private float pressureLayerVol = 0.5f;
    private float riskLayerVol = 0.5f;
    private float dangerLayerVol = 0.6f;
    private float heartbeatLayerVol = 0.65f;
    private bool manualLayersInit = false;

    // log tab
    private bool[] categoryOn;
    private bool errorsOnly;
    private bool logAutoScroll = true;
    private string logSearch = string.Empty;
    private readonly List<LogEntry> logCache = new List<LogEntry>();
    private int logCacheVersion = -1;
    private bool logDirty = true;

    private GUIStyle lineStyle;
    private GUIStyle warnStyle;
    private GUIStyle errorStyle;
    private GUIStyle boldStyle;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        net = GetComponent<TestNetworkManager>();

        bots = GetComponent<BotDirector>();

        if (bots == null)
            bots = gameObject.AddComponent<BotDirector>();

        auto = GetComponent<AutoTester>();

        if (auto == null)
            auto = gameObject.AddComponent<AutoTester>();

        categoryOn =
            Enumerable
                .Repeat(true, Enum.GetValues(typeof(LogCat)).Length)
                .ToArray();
    }

    private void Start()
    {
        if (net == null)
        {
            GameLog.Error(
                LogCat.Test,
                "TestNetworkManager is missing on the TestHarness object. " +
                "Run Tools > Testing > Create Test Scene again.");
        }

        if (GameMaster.Instance != null)
        {
            // The match is started from this panel, not automatically.
            GameMaster.Instance.HoldStart = true;
        }
        else
        {
            GameLog.Error(
                LogCat.Test,
                "No GameMaster in the scene.");
        }

        GameLog.Event(
            LogCat.Test,
            "Test scene ready. F1 = panel.");

        SetPanel(panelOpenOnStart);

        if (autoHostOnStart &&
            net != null)
        {
            StartCoroutine(HostNextFrame());
        }
    }

    private IEnumerator HostNextFrame()
    {
        yield return null;

        if (!NetworkServer.active &&
            !NetworkClient.active)
        {
            HostNow();
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.f1Key.wasPressedThisFrame)
                SetPanel(!panelOpen);

            if (keyboard.f2Key.wasPressedThisFrame &&
                !auto.IsRunning)
            {
                Time.timeScale =
                    Time.timeScale > 0f ? 0f : 1f;

                GameLog.Event(
                    LogCat.Test,
                    $"timeScale = {Time.timeScale}");
            }
        }

        if (panelOpen)
            ApplyLocalControls(false);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    // =========================================================
    // PANEL / CURSOR
    // =========================================================

    private void SetPanel(bool open)
    {
        bool changed = open != panelOpen;

        panelOpen = open;

        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (NetworkClient.localPlayer != null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (changed &&
            !open)
        {
            ApplyLocalControls(true);
        }
    }

    // While the panel is open the camera must not rotate and the world
    // must not react to clicks.
    private void ApplyLocalControls(bool enabledState)
    {
        NetworkIdentity identity =
            NetworkClient.localPlayer;

        if (identity == null)
            return;

        if (identity.TryGetComponent(
                out Player player) &&
            player.cameraController != null)
        {
            player.cameraController.enabled = enabledState;
        }

        foreach (Interactor interactor
                 in identity.GetComponentsInChildren<Interactor>(true))
        {
            interactor.enabled = enabledState;
        }
    }

    // =========================================================
    // NETWORK ACTIONS
    // =========================================================

    private void SetPort()
    {
        if (net == null ||
            !ushort.TryParse(portText, out ushort port))
        {
            return;
        }

        KcpTransport kcp =
            net.transport as KcpTransport;

        if (kcp != null)
            kcp.Port = port;
    }

    private void HostNow()
    {
        if (net == null)
            return;

        SetPort();

        GameLog.Event(
            LogCat.Net,
            "Starting host...");

        net.StartHost();
    }

    private void JoinNow()
    {
        if (net == null)
            return;

        SetPort();

        net.networkAddress = ipText;

        GameLog.Event(
            LogCat.Net,
            $"Joining {ipText}:{portText}...");

        net.StartClient();
    }

    private void StopNetwork()
    {
        if (auto.IsRunning)
            auto.Stop();

        if (net == null)
            return;

        GameLog.Event(
            LogCat.Net,
            "Stopping network...");

        if (NetworkServer.active &&
            NetworkClient.isConnected)
        {
            net.StopHost();
        }
        else if (NetworkServer.active)
        {
            net.StopServer();
        }
        else
        {
            net.StopClient();
        }

        bots.Bots.Clear();
        Time.timeScale = 1f;
    }

    private IEnumerator QuickStart(
        int botCount,
        BotMode mode)
    {
        if (!NetworkServer.active)
        {
            HostNow();
            yield return new WaitForSeconds(1f);
        }

        if (GameMaster.Instance != null &&
            GameMaster.Instance.IsMatchRunning)
        {
            ResetMatch();
            yield return null;
        }

        bots.Clear();
        yield return null;

        for (int i = 0; i < botCount; i++)
            bots.Spawn(mode);

        yield return new WaitForSeconds(0.5f);

        if (!StartMatch(out string error))
            message = "Quick start failed: " + error;
    }

    // =========================================================
    // MATCH (also used by AutoTester)
    // =========================================================

    public List<Player> CollectPlayers()
    {
        List<Player> result =
            TestTools.AllPlayers()
                .Where(
                    p => !bots.IsBot(p) &&
                         p.netIdentity != null &&
                         p.netIdentity.connectionToClient != null)
                .OrderBy(
                    p => p.netIdentity.connectionToClient.connectionId)
                .ToList();

        foreach (BotDirector.Bot bot in bots.Bots)
        {
            if (bot.player != null &&
                !result.Contains(bot.player))
            {
                result.Add(bot.player);
            }
        }

        return result;
    }

    public bool StartMatch(
        out string error)
    {
        error = null;

        GameMaster gm =
            GameMaster.Instance;

        if (!NetworkServer.active ||
            gm == null)
        {
            error = "the host is not running";
            return false;
        }

        if (gm.IsMatchRunning)
        {
            error = "a match is already running (reset it first)";
            return false;
        }

        List<Player> players =
            CollectPlayers();

        if (players.Count == 0)
        {
            error = "no players";
            return false;
        }

        if (players.Count > 5)
        {
            error = "at most 5 players";
            return false;
        }

        if (!gm.StartTestGame(players))
        {
            error = "GameMaster refused to start (see the log)";
            return false;
        }

        ApplyTimers();

        return true;
    }

    public void ResetMatch()
    {
        if (NetworkServer.active &&
            GameMaster.Instance != null)
        {
            GameMaster.Instance.ResetMatch();
        }
    }

    private void ApplyTimers()
    {
        if (AttackSequence.Instance != null)
        {
            AttackSequence.Instance.ConfigureTimers(
                selectTime,
                defenseTime,
                stealTime);
        }
    }

    // =========================================================
    // GUI ROOT
    // =========================================================

    private void EnsureStyles()
    {
        if (lineStyle != null)
            return;

        lineStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = false,
            richText = false
        };

        lineStyle.margin = new RectOffset(0, 0, 0, 0);
        lineStyle.padding = new RectOffset(2, 2, 0, 0);

        warnStyle = new GUIStyle(lineStyle);
        warnStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);

        errorStyle = new GUIStyle(lineStyle);
        errorStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

        boldStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        };
    }

    private void OnGUI()
    {
        EnsureStyles();

        float scale =
            Mathf.Max(1f, Screen.height / 900f);

        GUI.matrix =
            Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

        float screenW = Screen.width / scale;
        float screenH = Screen.height / scale;

        DrawStatusBar(screenW);

        if (!panelOpen)
        {
            GUI.Label(
                new Rect(10, 6, 300, 22),
                "F1 - test panel   F2 - pause");

            return;
        }

        GUILayout.BeginArea(
            new Rect(10, 10, 480, screenH - 20),
            GUI.skin.box);

        tab = (Tab)GUILayout.Toolbar(
            (int)tab,
            new[] { "Session", "Manual", "Auto", "Sound", "Log" });

        if (!string.IsNullOrEmpty(message))
        {
            GUILayout.Label(
                message,
                warnStyle);
        }

        if (tab == Tab.Log)
        {
            DrawLogTab();
        }
        else
        {
            scroll = GUILayout.BeginScrollView(scroll);

            switch (tab)
            {
                case Tab.Session:
                    DrawSessionTab();
                    break;

                case Tab.Manual:
                    DrawManualTab();
                    break;

                case Tab.Auto:
                    DrawAutoTab();
                    break;

                case Tab.Sound:
                    DrawSoundTab();
                    break;
            }

            GUILayout.EndScrollView();
        }

        GUILayout.EndArea();
    }

    private string NetState()
    {
        if (NetworkServer.active && NetworkClient.active)
            return "HOST";

        if (NetworkServer.active)
            return "SERVER";

        if (NetworkClient.isConnected)
            return "CLIENT";

        if (NetworkClient.active)
            return "CONNECTING";

        return "OFFLINE";
    }

    private void DrawStatusBar(float screenW)
    {
        GameMaster gm = GameMaster.Instance;

        string match = "no match";

        if (gm != null &&
            gm.IsMatchRunning &&
            gm.CurrentPlayerIndex >= 0 &&
            gm.CurrentPlayerIndex < gm.Players.Count)
        {
            match =
                $"turn {gm.TurnNumber}  {gm.CurrentPhase}  " +
                $"cur: {GameLog.P(gm.Players[gm.CurrentPlayerIndex])}";
        }

        string text =
            $"{NetState()}  |  {match}  |  x{Time.timeScale:0.#}  |  " +
            $"errors {GameLog.ErrorCount}  warn {GameLog.WarningCount}" +
            (auto != null && auto.IsRunning ? "  |  AUTO RUNNING" : string.Empty);

        GUI.Box(
            new Rect(screenW - 640, 6, 630, 24),
            text);
    }

    private void Section(string title)
    {
        GUILayout.Space(6);
        GUILayout.Label(title, boldStyle);
    }

    // =========================================================
    // SESSION TAB
    // =========================================================

    private void DrawSessionTab()
    {
        bool server = NetworkServer.active;
        bool anyNet = server || NetworkClient.active;
        GameMaster gm = GameMaster.Instance;
        bool matchRunning = gm != null && gm.IsMatchRunning;

        Section("NETWORK  (" + NetState() + ")");

        if (!anyNet)
        {
            if (GUILayout.Button("HOST (server + your player)"))
                HostNow();

            GUILayout.BeginHorizontal();
            GUILayout.Label("IP", GUILayout.Width(20));
            ipText = GUILayout.TextField(ipText, GUILayout.Width(150));
            GUILayout.Label("port", GUILayout.Width(30));
            portText = GUILayout.TextField(portText, GUILayout.Width(60));

            if (GUILayout.Button("JOIN"))
                JoinNow();

            GUILayout.EndHorizontal();
        }
        else if (GUILayout.Button("STOP network"))
        {
            StopNetwork();
        }

        if (!server)
        {
            GUILayout.Label(
                "Bots, match and manual actions are available on the host only.");

            return;
        }

        Section("QUICK START");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("alone (void)"))
            StartCoroutine(QuickStart(0, BotMode.Passive));

        if (GUILayout.Button("+2 dumb bots"))
            StartCoroutine(QuickStart(2, BotMode.Random));

        if (GUILayout.Button("+2 smart bots"))
            StartCoroutine(QuickStart(2, BotMode.Smart));

        GUILayout.EndHorizontal();

        Section("PLAYERS");

        foreach (Player player in TestTools.AllPlayers())
        {
            BotDirector.Bot bot = bots.Find(player);

            string kind =
                bot != null
                    ? "BOT " + bot.mode
                    : player.isLocalPlayer
                        ? "YOU"
                        : "HUMAN";

            GUILayout.BeginHorizontal();

            GUILayout.Label(
                $"[{player.SeatIndex}] {player.SteamNickname}  <{kind}>  " +
                $"acc:{TestTools.AccessoryString(player)}",
                GUILayout.Width(300));

            if (bot != null)
            {
                if (GUILayout.Button(
                        bot.mode.ToString(),
                        GUILayout.Width(70)))
                {
                    bot.mode =
                        (BotMode)(((int)bot.mode + 1) %
                                  Enum.GetValues(typeof(BotMode)).Length);
                }

                if (!matchRunning &&
                    GUILayout.Button(
                        "X",
                        GUILayout.Width(26)))
                {
                    bots.Remove(bot);
                }
            }

            GUILayout.EndHorizontal();
        }

        GUI.enabled = !matchRunning;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Passive (dummy)"))
            bots.Spawn(BotMode.Passive);

        if (GUILayout.Button("+ Random (dumb)"))
            bots.Spawn(BotMode.Random);

        if (GUILayout.Button("+ Smart"))
            bots.Spawn(BotMode.Smart);

        GUILayout.EndHorizontal();

        GUI.enabled = true;

        Section("MATCH");

        GUILayout.BeginHorizontal();

        GUI.enabled = !matchRunning;

        if (GUILayout.Button("START MATCH"))
        {
            if (!StartMatch(out string error))
                message = "Cannot start: " + error;
            else
                message = string.Empty;
        }

        GUI.enabled = matchRunning;

        if (GUILayout.Button("RESET MATCH"))
            ResetMatch();

        GUI.enabled = true;

        GUILayout.EndHorizontal();

        Section("SPEED  (x" + Time.timeScale.ToString("0.0") + ")");

        if (auto.IsRunning)
        {
            GUILayout.Label("controlled by the auto test");
        }
        else
        {
            float newScale =
                GUILayout.HorizontalSlider(
                    Time.timeScale,
                    0f,
                    20f);

            if (!Mathf.Approximately(newScale, Time.timeScale))
                Time.timeScale = newScale;

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("x1")) Time.timeScale = 1f;
            if (GUILayout.Button("x3")) Time.timeScale = 3f;
            if (GUILayout.Button("x10")) Time.timeScale = 10f;
            if (GUILayout.Button("pause")) Time.timeScale = 0f;

            GUILayout.EndHorizontal();
        }

        Section("TIMERS (seconds)");

        selectTime = Slider("target selection", selectTime, 1f, 30f);
        defenseTime = Slider("defence window", defenseTime, 1f, 30f);
        stealTime = Slider("steal window", stealTime, 1f, 30f);

        if (GUILayout.Button("apply timers"))
            ApplyTimers();

        Section("BOT AI");

        bots.AIEnabled =
            GUILayout.Toggle(
                bots.AIEnabled,
                "bots play by themselves");

        bots.autoRedeal =
            GUILayout.Toggle(
                bots.autoRedeal,
                "give bots new cards when they have no attack card");

        bots.chaos =
            GUILayout.Toggle(
                bots.chaos,
                "CHAOS: bots try illegal actions (validation test)");

        if (bots.chaos)
        {
            bots.chaosChance =
                Slider(
                    "chaos chance",
                    bots.chaosChance,
                    0f,
                    1f);
        }

        bots.thinkTime.x =
            Slider("think min", bots.thinkTime.x, 0f, 5f);

        bots.thinkTime.y =
            Mathf.Max(
                bots.thinkTime.x,
                Slider("think max", bots.thinkTime.y, 0f, 5f));
    }

    private float Slider(
        string label,
        float value,
        float min,
        float max)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(
            $"{label}: {value:0.0}",
            GUILayout.Width(190));

        value =
            GUILayout.HorizontalSlider(
                value,
                min,
                max);

        GUILayout.EndHorizontal();

        return value;
    }

    // =========================================================
    // MANUAL TAB
    // =========================================================

    private List<Player> SeatedPlayers()
    {
        GameMaster gm = GameMaster.Instance;

        if (gm != null &&
            gm.IsMatchRunning)
        {
            return gm.Players
                .Where(p => p != null)
                .ToList();
        }

        return TestTools.AllPlayers();
    }

    // runs an action, logs it and shows a refusal
    private void Do(
        string name,
        Func<bool> action)
    {
        bool ok;

        try
        {
            ok = action();
        }
        catch (Exception exception)
        {
            GameLog.Error(
                LogCat.Test,
                $"MANUAL {name} threw: {exception}");

            message = name + " threw an exception (see the log)";
            return;
        }

        GameLog.Event(
            LogCat.Test,
            $"MANUAL {name} -> {(ok ? "ok" : "REFUSED")}");

        message = ok ? string.Empty : name + " was refused (see the log)";
    }

    private void DrawManualTab()
    {
        if (!NetworkServer.active)
        {
            GUILayout.Label("Start the host first (Session tab).");
            return;
        }

        List<Player> players = SeatedPlayers();

        if (players.Count == 0)
        {
            GUILayout.Label("No players.");
            return;
        }

        GameMaster gm = GameMaster.Instance;
        AttackSequence seq = AttackSequence.Instance;

        actIndex = Mathf.Clamp(actIndex, 0, players.Count - 1);

        Section("ACT AS");

        actIndex =
            GUILayout.SelectionGrid(
                actIndex,
                players
                    .Select(p => $"{p.SeatIndex}: {p.SteamNickname}")
                    .ToArray(),
                3);

        Player actor = players[actIndex];

        GUILayout.Label(
            $"phase: {gm.CurrentPhase}   turn: {gm.TurnNumber}   " +
            $"sequence active: {(seq != null && seq.IsActive)}   " +
            $"accessories: {TestTools.AccessoryString(actor)}");

        // ---------------- hand
        Section("HAND");

        List<Card> hand = TestTools.HandOf(actor);

        if (hand.Count == 0)
            GUILayout.Label("(no cards)");

        foreach (Card card in hand)
        {
            if (card == null)
                continue;

            GUILayout.BeginHorizontal();

            GUILayout.Label(
                $"{card.SocketIndex}: {GameLog.C(card)}  " +
                (card.IsInRoulette
                    ? "IN ROULETTE"
                    : card.IsAvailable
                        ? "available"
                        : "blocked/" + card.Reason),
                GUILayout.Width(300));

            if (GUILayout.Button("PLAY", GUILayout.Width(50)))
            {
                Card captured = card;

                Do(
                    $"play {GameLog.C(captured)} as {GameLog.P(actor)}",
                    () => seq.ServerPlayCard(captured, actor));
            }

            if (GUILayout.Button("type>", GUILayout.Width(50)))
            {
                int index = TestTools.TypeIndex(card, card.Data);
                int count = card.AvailableTypes.Count;

                if (count > 0)
                    card.SetDataByIndex((index + 1 + count) % count);
            }

            GUILayout.EndHorizontal();
        }

        // ---------------- attack flow
        Section("ATTACK FLOW");

        List<Player> others =
            players.Where(p => p != actor).ToList();

        if (others.Count > 0)
        {
            targetIndex = Mathf.Clamp(targetIndex, 0, others.Count - 1);

            targetIndex =
                GUILayout.SelectionGrid(
                    targetIndex,
                    others
                        .Select(p => $"-> {p.SeatIndex}: {p.SteamNickname}")
                        .ToArray(),
                    3);

            Player victim = others[targetIndex];

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("SET TARGET"))
            {
                Do(
                    $"target {GameLog.P(victim)} as {GameLog.P(actor)}",
                    () => seq.ServerSetTarget(
                        actor,
                        victim.GetComponent<PlayerInteractable>()));
            }

            if (GUILayout.Button("CANCEL ATTACK"))
            {
                Do(
                    $"cancel as {GameLog.P(actor)}",
                    () => seq.ServerCancel(actor));
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("steal from target:", GUILayout.Width(120));

            for (int i = 0; i < 5; i++)
            {
                if (!victim.HasAccessory(i))
                    continue;

                int captured = i;

                if (GUILayout.Button($"#{i}", GUILayout.Width(40)))
                {
                    Do(
                        $"steal {captured} from {GameLog.P(victim)} " +
                        $"as {GameLog.P(actor)}",
                        () => gm.TryStealAccesory(
                            victim,
                            captured,
                            actor));
                }
            }

            GUILayout.EndHorizontal();
        }

        // ---------------- game flow
        Section("GAME FLOW");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("END TURN"))
        {
            Do(
                "EndTurn",
                () =>
                {
                    gm.EndTurn();
                    return true;
                });
        }

        if (GUILayout.Button("redeal actor"))
        {
            TestTools.Redeal(actor);
        }

        if (GUILayout.Button("redeal all"))
        {
            TestTools.RedealAll(players);
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("force phase:", GUILayout.Width(90));

        foreach (GameMaster.GamePhase phase
                 in Enum.GetValues(typeof(GameMaster.GamePhase)))
        {
            if (GUILayout.Button(phase.ToString()))
                gm.DebugForcePhase(phase);
        }

        GUILayout.EndHorizontal();

        // ---------------- effects
        Section("EFFECTS ON ACTOR");

        Card sample = TestTools.AllCards().FirstOrDefault();

        if (sample != null &&
            sample.AvailableTypes != null)
        {
            foreach (CardData data in sample.AvailableTypes)
            {
                if (data == null ||
                    data.cardEffect == null)
                {
                    continue;
                }

                CardData captured = data;

                if (GUILayout.Button(
                        $"apply effect of {data.name} ({data.cardEffect.name})"))
                {
                    Do(
                        $"effect {captured.name} on {GameLog.P(actor)}",
                        () =>
                        {
                            captured.cardEffect.ApplyEffectToTarget(actor);
                            return true;
                        });
                }
            }
        }

        if (GUILayout.Button("damage pass (red flash)"))
            actor.StartDamagePass();

        // ---------------- accessories
        GUILayout.BeginHorizontal();
        GUILayout.Label("accessories:", GUILayout.Width(90));

        for (int i = 0; i < 5; i++)
        {
            bool has = actor.HasAccessory(i);
            bool now = GUILayout.Toggle(has, i.ToString(), "Button");

            if (now != has)
                actor.SetAccesory(i, now);
        }

        GUILayout.EndHorizontal();

        // ---------------- bank
        if (actor.TryGetComponent(out PlayerBank bank))
        {
            Section("BANK  (coins: " + bank.Coins + ")");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("+100 coins"))
                bank.AddCoins(100);

            if (GUILayout.Button("-100 coins"))
                bank.RemoveCoins(100);

            if (GUILayout.Button("+1 chip of each"))
            {
                foreach (int denomination in bank.AvailableDenominations)
                    bank.AddChips(denomination, 1);
            }

            GUILayout.EndHorizontal();
        }
    }

    // =========================================================
    // SOUND TAB
    // =========================================================

    private void DrawSoundTab()
    {
        if (!NetworkServer.active)
        {
            GUILayout.Label("Start the host first (Session tab).");
            return;
        }

        List<Player> players = SeatedPlayers();

        if (players.Count == 0)
        {
            GUILayout.Label("No players.");
            return;
        }

        actIndex = Mathf.Clamp(actIndex, 0, players.Count - 1);

        Section("PLAYER");

        actIndex =
            GUILayout.SelectionGrid(
                actIndex,
                players
                    .Select(p => $"{p.SeatIndex}: {p.SteamNickname}")
                    .ToArray(),
                3);

        Player actor = players[actIndex];
        bool isLocalActor = actor != null && (actor.isLocalPlayer || (NetworkClient.localPlayer != null && actor.netId == NetworkClient.localPlayer.netId));

        string actorTypeStr = isLocalActor
            ? "<color=#55ff55>[LOCAL PLAYER: 2D Audio, UI Allowed]</color>"
            : "<color=#ffdd55>[BOT / REMOTE: 3D Spatial Audio, UI Blocked]</color>";
        GUILayout.Label($"Active Emitter: {actor.SteamNickname} (Seat {actor.SeatIndex}) {actorTypeStr}", lineStyle);

        if (GameAudioManager.Instance == null)
        {
            Section("AUDIO MANAGER");
            GUILayout.Label("GameAudioManager.Instance is not in scene.");
        }
        else
        {
            Section("MUSIC & AMBIENT LAYERS (GameAudioManager)");

            // Mute / Stop / Play
            GUILayout.BeginHorizontal();
            string muteText = GameAudioManager.Instance.IsMusicMuted ? "[ UNMUTE ALL MUSIC ]" : "[ MUTE ALL MUSIC ]";
            if (GUILayout.Button(muteText))
            {
                GameAudioManager.Instance.ToggleMusicMute();
            }
            if (GUILayout.Button("Stop Music"))
            {
                GameAudioManager.Instance.StopAllMusic();
                SyncLayerVolumesFromAudio();
            }
            if (GUILayout.Button("Start Continuous Ambient"))
            {
                GameAudioManager.Instance.StartContinuousAmbient();
                SyncLayerVolumesFromAudio();
            }
            GUILayout.EndHorizontal();

            // Preset simulation
            GUILayout.Label("Simulate Phase Music Layers (Strict Single Mix):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ambient (Start/Wait)"))
            {
                GameAudioManager.Instance.SetMusicState(GameAudioManager.MusicLayerState.AmbientOnly);
                SyncLayerVolumesFromAudio();
            }
            if (GUILayout.Button("+ Pressure (Low HP / Timer)"))
            {
                GameAudioManager.Instance.SetMusicState(GameAudioManager.MusicLayerState.AmbientPressure);
                SyncLayerVolumesFromAudio();
            }
            if (GUILayout.Button("+ Risk (Target Selection)"))
            {
                GameAudioManager.Instance.SetMusicState(GameAudioManager.MusicLayerState.AmbientRisk);
                SyncLayerVolumesFromAudio();
            }
            if (GUILayout.Button("+ Danger (Attack Bystander)"))
            {
                GameAudioManager.Instance.SetMusicState(GameAudioManager.MusicLayerState.AmbientDanger);
                SyncLayerVolumesFromAudio();
            }
            if (GUILayout.Button("+ Danger + Heart (Victim)"))
            {
                GameAudioManager.Instance.SetMusicState(GameAudioManager.MusicLayerState.AmbientDangerHeartbeat);
                SyncLayerVolumesFromAudio();
            }
            GUILayout.EndHorizontal();

            // Individual Layer sliders
            GUILayout.Label("Manual Layer Volume & Toggle:");
            if (!manualLayersInit)
            {
                SyncLayerVolumesFromAudio();
                manualLayersInit = true;
            }

            DrawLayerControl("Ambient", ref ambientLayerVol);
            DrawLayerControl("Pressure", ref pressureLayerVol);
            DrawLayerControl("Risk", ref riskLayerVol);
            DrawLayerControl("Danger", ref dangerLayerVol);
            DrawLayerControl("Heartbeat", ref heartbeatLayerVol);

            Section("CHARACTER VOICE & REACTIONS (Spatial on Bot, 2D on Local)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Burp"))
                GameAudioManager.Instance.PlayPlayerReactionClip(actor.netIdentity, GameAudioManager.ReactionClipType.Burp, 0f);
            if (GUILayout.Button("Cough"))
                GameAudioManager.Instance.PlayPlayerReactionClip(actor.netIdentity, GameAudioManager.ReactionClipType.Caugh, 0f);
            if (GUILayout.Button("Chair"))
                GameAudioManager.Instance.PlayPlayerReactionClip(actor.netIdentity, GameAudioManager.ReactionClipType.CreakingChair, 0f);
            if (GUILayout.Button("Exhale"))
                GameAudioManager.Instance.PlayPlayerReactionClip(actor.netIdentity, GameAudioManager.ReactionClipType.Exhale, 0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Joyful"))
                GameAudioManager.Instance.PlayPlayerReactionClip(actor.netIdentity, GameAudioManager.ReactionClipType.Joyful, 0f);
            if (GUILayout.Button("Whistle"))
                GameAudioManager.Instance.PlayPlayerReactionClip(actor.netIdentity, GameAudioManager.ReactionClipType.Whistle, 0f);
            if (GUILayout.Button("Wonderment"))
                GameAudioManager.Instance.PlayPlayerReactionClip(actor.netIdentity, GameAudioManager.ReactionClipType.Wonderment, 0f);
            if (GUILayout.Button("Scoff"))
                GameAudioManager.Instance.PlayPlayerReactionClip(actor.netIdentity, GameAudioManager.ReactionClipType.Scoff, 0f);
            if (GUILayout.Button("Disappointment"))
                GameAudioManager.Instance.PlayPlayerReactionClip(actor.netIdentity, GameAudioManager.ReactionClipType.Disappointment, 0f);
            GUILayout.EndHorizontal();

            Section("SOUNDBOARD: WEAPONS & AFTERMATH (Emits at Selected Actor)");
            DrawWeaponSoundRow(actor, "pistol", "Revolver / Pistol", true);
            DrawWeaponSoundRow(actor, "knife", "Knife", true, GameAudioManager.Instance.knifeHitsHand, "Hits Hand", GameAudioManager.Instance.atkKnifeVicHitSound, "Vic Hit");
            DrawWeaponSoundRow(actor, "shears", "Shears", true, GameAudioManager.Instance.atkShearsVicHitSound, "Vic Hit");
            DrawWeaponSoundRow(actor, "bucket", "Bucket", true, GameAudioManager.Instance.atkBucketVicHitSound, "Vic Hit");
            DrawWeaponSoundRow(actor, "fork", "Fork", true, GameAudioManager.Instance.atkForkVicHitSound, "Vic Hit");
            DrawWeaponSoundRow(actor, "grenade", "Grenade", true, GameAudioManager.Instance.grenadeBoom, "Boom SFX");
            DrawWeaponSoundRow(actor, "shotgun", "Shotgun", true);
            DrawWeaponSoundRow(actor, "whiskey", "Whiskey", true, GameAudioManager.Instance.atkWhiskyVicHitSound, "Vic Hit");
            DrawWeaponSoundRow(actor, "flame", "Flame (No Aftermath)", false);

            Section("SOUNDBOARD: CARDS & GAMEPLAY (Emits at Selected Actor)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hands Pick"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.handsPick);
            if (GUILayout.Button("Hands Roll"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.handsRoll);
            if (GUILayout.Button("Cards Shuffle"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.cardsShuffle);
            if (GUILayout.Button("Card Burn"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.cardBurnSound);
            if (GUILayout.Button("Card Destroy"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.cardDestroy);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Deal Hole 1"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.cardsGivingHole1);
            if (GUILayout.Button("Deal Hole 2"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.cardsGivingHole2);
            if (GUILayout.Button("Deal Hole 3"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.cardsGivingHole3);
            if (GUILayout.Button("Accessory Add"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.accessoryAdd);
            if (GUILayout.Button("Accessory Disappear"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.accessoryDisappearSound);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Coin Toss"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.coinToss);
            if (GUILayout.Button("Points Add"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.pointsAdd);
            if (GUILayout.Button("10 Seconds Left"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.tenLeft);
            if (GUILayout.Button("Next Stage"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.nextStageSound);
            if (GUILayout.Button("Next Turn"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.nextSound);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Attack Success"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.attackSuccessful);
            if (GUILayout.Button("Attack Unsuccess"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.attackUnsuccessful);
            if (GUILayout.Button("Victory Win"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.winSound);
            if (GUILayout.Button("Game Lost"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.lostSound);
            GUILayout.EndHorizontal();

            Section("SOUNDBOARD: PLATFORM & SLOT MACHINE (Emits at Selected Actor)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Platform Base"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.platformBase);
            if (GUILayout.Button("Plate Down"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.platformPlateDown);
            if (GUILayout.Button("Plate Up"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.platformPlateUp);
            if (GUILayout.Button("SM Down"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.platformSmDown);
            if (GUILayout.Button("SM Up"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.platformSmUp);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Slot Windup"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.slotMachineWindup);
            if (GUILayout.Button("Slot Spin"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.slotMachineSpin);
            if (GUILayout.Button("Slot Stop"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.slotMachineStop);
            if (GUILayout.Button("Slot Card Give"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.slotMachineCardGive);
            if (GUILayout.Button("Slot Pick 1"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.slotMachinePick1);
            if (GUILayout.Button("Slot Pick 2"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.slotMachinePick2);
            if (GUILayout.Button("Slot Pick 3"))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor.netIdentity, GameAudioManager.Instance.slotMachinePick3);
            GUILayout.EndHorizontal();

            Section("SOUNDBOARD: UI SOUNDS (Local Player Only)");
            if (!isLocalActor)
            {
                GUI.enabled = false;
                GUILayout.Label("<color=#ffaa33>DISABLED: A bot or other player cannot emit UI sounds.</color>", warnStyle);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("UI Accept"))
                GameAudioManager.Instance.PlayAccept();
            if (GUILayout.Button("UI Cancel"))
                GameAudioManager.Instance.PlayCancel();
            if (GUILayout.Button("UI Unavailable"))
                GameAudioManager.Instance.PlayUnavailable();
            if (GUILayout.Button("UI Menu Press"))
                GameAudioManager.Instance.PlayMenuPress();
            if (GUILayout.Button("UI Menu Open"))
                GameAudioManager.Instance.PlayMenuOpen();
            if (GUILayout.Button("UI Menu Close"))
                GameAudioManager.Instance.PlayMenuClose();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("UI Hover (Random)"))
                GameAudioManager.Instance.PlayHover();
            if (GUILayout.Button("Hover 1"))
                GameAudioManager.Instance.PlayUi(GameAudioManager.Instance.uiHover1, 0.7f);
            if (GUILayout.Button("Hover 2"))
                GameAudioManager.Instance.PlayUi(GameAudioManager.Instance.uiHover2, 0.7f);
            if (GUILayout.Button("Hover 3"))
                GameAudioManager.Instance.PlayUi(GameAudioManager.Instance.uiHover3, 0.7f);
            if (GUILayout.Button("Hover 4"))
                GameAudioManager.Instance.PlayUi(GameAudioManager.Instance.uiHover4, 0.7f);
            if (GUILayout.Button("Hover 5"))
                GameAudioManager.Instance.PlayUi(GameAudioManager.Instance.uiHover5, 0.7f);
            GUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        Section("RECENT SOUND LOG");

        List<LogEntry> recent = new List<LogEntry>();

        GameLog.Snapshot(
            recent,
            12,
            e => e.cat == LogCat.Sound);

        foreach (LogEntry entry in recent)
        {
            GUILayout.Label(
                $"t={entry.gameTime:0.0}  {entry.message}",
                lineStyle);
        }
    }

    private void DrawWeaponSoundRow(
        Player actor,
        string weaponKey,
        string displayName,
        bool hasAftermath,
        AudioClip extraClip1 = null,
        string extra1Name = "",
        AudioClip extraClip2 = null,
        string extra2Name = "")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(displayName, GUILayout.Width(135));

        if (GUILayout.Button("Ready"))
            GameAudioManager.Instance.PlayWeaponReady(weaponKey, actor != null ? actor.netIdentity : null);

        if (GUILayout.Button("Attack"))
            GameAudioManager.Instance.PlayWeaponAttackAndHit(weaponKey, true, actor != null ? actor.netIdentity : null);

        if (GUILayout.Button("Defended"))
            GameAudioManager.Instance.PlayWeaponAttackAndHit(weaponKey, false, actor != null ? actor.netIdentity : null);

        if (GUILayout.Button("Cancel"))
            GameAudioManager.Instance.PlayWeaponCancel(weaponKey, actor != null ? actor.netIdentity : null);

        if (hasAftermath)
        {
            if (GUILayout.Button("Aftermath"))
                GameAudioManager.Instance.PlayWeaponAftermath(weaponKey, actor != null ? actor.netIdentity : null);
        }

        if (extraClip1 != null && !string.IsNullOrEmpty(extra1Name))
        {
            if (GUILayout.Button(extra1Name))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor != null ? actor.netIdentity : null, extraClip1);
        }

        if (extraClip2 != null && !string.IsNullOrEmpty(extra2Name))
        {
            if (GUILayout.Button(extra2Name))
                GameAudioManager.Instance.PlaySoundAtPlayer(actor != null ? actor.netIdentity : null, extraClip2);
        }

        GUILayout.EndHorizontal();
    }

    private void DrawLayerControl(string name, ref float volume)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, GUILayout.Width(75));

        bool isAudible = volume > 0.01f;
        if (GUILayout.Button(isAudible ? "Mute" : "Unmute", GUILayout.Width(60)))
        {
            volume = isAudible ? 0f : 0.5f;
            GameAudioManager.Instance.SetLayerVolumeDirect(name, volume);
            SyncLayerVolumesFromAudio();
        }

        float newVol = GUILayout.HorizontalSlider(volume, 0f, 1f, GUILayout.Width(170));
        if (Mathf.Abs(newVol - volume) > 0.005f)
        {
            volume = newVol;
            GameAudioManager.Instance.SetLayerVolumeDirect(name, volume);
            SyncLayerVolumesFromAudio();
        }

        GUILayout.Label($"{volume:0.00}", GUILayout.Width(35));
        GUILayout.EndHorizontal();
    }

    private void SyncLayerVolumesFromAudio()
    {
        if (GameAudioManager.Instance == null) return;
        ambientLayerVol = GameAudioManager.Instance.GetLayerVolume("ambient");
        pressureLayerVol = GameAudioManager.Instance.GetLayerVolume("pressure");
        riskLayerVol = GameAudioManager.Instance.GetLayerVolume("risk");
        dangerLayerVol = GameAudioManager.Instance.GetLayerVolume("danger");
        heartbeatLayerVol = GameAudioManager.Instance.GetLayerVolume("heartbeat");
    }

    // =========================================================
    // AUTO TAB
    // =========================================================

    private void DrawAutoTab()
    {
        Section("AUTO TEST");

        GUILayout.Label(
            "Bots play by themselves at high speed. Every scenario " +
            "(each attack card x hit / steal / cancel / each defence) " +
            "is checked. Results go to the log and to a report file.");

        GUI.enabled = !auto.IsRunning;

        auto.runMode =
            (AutoTester.RunMode)GUILayout.Toolbar(
                (int)auto.runMode,
                new[] { "one pass", "soak (repeat)" });

        auto.playerCount =
            Mathf.RoundToInt(
                Slider(
                    "players",
                    auto.playerCount,
                    2,
                    5));

        auto.speed =
            Slider(
                "speed x",
                auto.speed,
                1f,
                30f);

        GUI.enabled = true;

        GUILayout.BeginHorizontal();

        GUI.enabled = !auto.IsRunning && NetworkServer.active;

        if (GUILayout.Button("START AUTO TEST"))
            auto.Begin();

        GUI.enabled = auto.IsRunning;

        if (GUILayout.Button("STOP"))
            auto.Stop();

        GUI.enabled = true;

        GUILayout.EndHorizontal();

        if (!NetworkServer.active)
            GUILayout.Label("Start the host first (Session tab).");

        GUILayout.Label($"status: {auto.Status}");

        GUILayout.Label(
            $"scenarios: {auto.DoneScenarios}/{auto.TotalScenarios}   " +
            $"passed {auto.Passed}   failed {auto.Failed}");

        if (!string.IsNullOrEmpty(auto.ReportPath))
        {
            GUILayout.Label("report: " + auto.ReportPath);

            if (GUILayout.Button("copy report to clipboard"))
                GUIUtility.systemCopyBuffer = auto.BuildReport("copy");
        }

        Section("RESULTS");

        autoScroll =
            GUILayout.BeginScrollView(
                autoScroll,
                GUILayout.Height(260));

        foreach (AutoTester.Result result
                 in auto.Results.Skip(Mathf.Max(0, auto.Results.Count - 200)))
        {
            GUILayout.Label(
                $"{(result.pass ? "PASS" : "FAIL")}  {result.key}  " +
                $"{result.attackerSeat}->{result.victimSeat}",
                result.pass ? lineStyle : errorStyle);

            if (!result.pass)
            {
                GUILayout.Label(
                    "      " + result.details,
                    errorStyle);
            }
        }

        GUILayout.EndScrollView();

        Section("SEAT PAIRS HIT (attacker row -> victim column)");

        int n =
            GameMaster.Instance != null &&
            GameMaster.Instance.IsMatchRunning
                ? GameMaster.Instance.Players.Count
                : auto.playerCount;

        for (int a = 0; a < n; a++)
        {
            string row = $"seat {a}:";

            for (int v = 0; v < n; v++)
            {
                row += a == v
                    ? "   -"
                    : $" {auto.PairCounts[a, v],3}";
            }

            GUILayout.Label(row, lineStyle);
        }
    }

    // =========================================================
    // LOG TAB
    // =========================================================

    private bool PassesFilter(LogEntry e)
    {
        if (!categoryOn[(int)e.cat])
            return false;

        if (errorsOnly &&
            !e.IsError &&
            !e.IsWarning)
        {
            return false;
        }

        if (logSearch.Length > 0 &&
            e.message.IndexOf(
                logSearch,
                StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        return true;
    }

    private void DrawLogTab()
    {
        GUILayout.Label(
            $"lines: {GameLog.Version}   errors: {GameLog.ErrorCount}   " +
            $"warnings: {GameLog.WarningCount}",
            lineStyle);

        GUILayout.Label(
            "file: " + GameLog.FilePath,
            lineStyle);

        // category toggles
        string[] names = Enum.GetNames(typeof(LogCat));

        for (int row = 0; row * 5 < names.Length; row++)
        {
            GUILayout.BeginHorizontal();

            for (int i = row * 5;
                 i < Mathf.Min(names.Length, row * 5 + 5);
                 i++)
            {
                bool now =
                    GUILayout.Toggle(
                        categoryOn[i],
                        names[i],
                        "Button");

                if (now != categoryOn[i])
                {
                    categoryOn[i] = now;
                    logDirty = true;
                }
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();

        bool errors =
            GUILayout.Toggle(
                errorsOnly,
                "errors + warnings only");

        if (errors != errorsOnly)
        {
            errorsOnly = errors;
            logDirty = true;
        }

        logAutoScroll =
            GUILayout.Toggle(
                logAutoScroll,
                "auto-scroll");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("search", GUILayout.Width(50));

        string search =
            GUILayout.TextField(logSearch);

        if (search != logSearch)
        {
            logSearch = search;
            logDirty = true;
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("copy visible"))
        {
            GUIUtility.systemCopyBuffer =
                string.Join(
                    "\n",
                    logCache.Select(e => GameLog.Format(e)));

            message = "copied " + logCache.Count + " lines";
        }

        if (GUILayout.Button("save all"))
        {
            message =
                "saved: " +
                GameLog.SaveTextFile(
                    $"manual_dump_{DateTime.Now:HHmmss}.log",
                    GameLog.AllText());
        }

        if (GUILayout.Button("clear"))
        {
            GameLog.Clear();
            logDirty = true;
        }

        GUILayout.EndHorizontal();

        // Refresh only in the Layout event, so that Layout and Repaint
        // always draw the same number of controls.
        if (Event.current.type == EventType.Layout &&
            (logDirty ||
             logCacheVersion != GameLog.Version))
        {
            GameLog.Snapshot(
                logCache,
                150,
                PassesFilter);

            logCacheVersion = GameLog.Version;
            logDirty = false;

            if (logAutoScroll)
                logScroll.y = float.MaxValue;
        }

        logScroll =
            GUILayout.BeginScrollView(
                logScroll,
                GUILayout.ExpandHeight(true));

        foreach (LogEntry entry in logCache)
        {
            string line =
                $"t={entry.gameTime,7:0.0} T{entry.turn,-3} " +
                $"{entry.cat,-8} {entry.message}";

            if (line.Length > 170)
                line = line.Substring(0, 170) + "...";

            GUILayout.Label(
                line,
                entry.IsError
                    ? errorStyle
                    : entry.IsWarning
                        ? warnStyle
                        : lineStyle);
        }

        GUILayout.EndScrollView();
    }
}
