using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mirror;
using UnityEngine;

/// <summary>Category of a log line (used for filtering in the test panel).</summary>
public enum LogCat
{
    Net,
    Match,
    Turn,
    Phase,
    Attack,
    Defense,
    Steal,
    Card,
    Player,
    Roulette,
    Sound,
    Bot,
    Auto,
    Test,
    Unity
}

public struct LogEntry
{
    public int id;
    public float gameTime;
    public float realTime;
    public int frame;
    public int turn;
    public string role;
    public LogCat cat;
    public LogType level;
    public string message;

    public bool IsError =>
        level == LogType.Error ||
        level == LogType.Exception ||
        level == LogType.Assert;

    public bool IsWarning =>
        level == LogType.Warning;
}

/// <summary>
/// One central log of every game event.
///
///  - kept in memory (last 20 000 lines) and shown in the test panel
///  - written to  &lt;persistentDataPath&gt;/GameLogs/session_*.log
///  - also captures every Unity warning / error / exception
///
/// Line format:  #id  t=game-time  r=real-time  f=frame  ROLE  Tturn  CATEGORY  message
/// </summary>
public static class GameLog
{
    public const int MaxEntries = 20000;

    public static bool Enabled = true;
    public static bool EchoToConsole = true;

    /// <summary>Current turn number (set by GameMaster).</summary>
    public static int Turn;

    public static int ErrorCount;
    public static int WarningCount;

    /// <summary>Increments with every new line (cheap change detection for the UI).</summary>
    public static int Version;

    public static string FilePath { get; private set; }

    private static readonly object sync = new object();
    private static readonly List<LogEntry> entries = new List<LogEntry>(2048);

    private static StreamWriter writer;
    private static bool initialized;
    private static int nextId = 1;

    // =========================================================
    // INIT
    // =========================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (initialized)
            return;

        initialized = true;

        Application.logMessageReceived -= OnUnityLog;
        Application.logMessageReceived += OnUnityLog;

        Application.quitting -= Close;
        Application.quitting += Close;

        OpenFile();

        Event(
            LogCat.Test,
            $"GameLog started. Unity {Application.unityVersion}, " +
            $"scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'. " +
            $"File: {FilePath}");
    }

    private static void OpenFile()
    {
        try
        {
            string dir =
                Path.Combine(
                    Application.persistentDataPath,
                    "GameLogs");

            Directory.CreateDirectory(dir);

            FilePath =
                Path.Combine(
                    dir,
                    $"session_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            writer =
                new StreamWriter(
                    FilePath,
                    false,
                    new UTF8Encoding(false))
                {
                    AutoFlush = true
                };
        }
        catch (Exception exception)
        {
            writer = null;
            FilePath = "(file logging failed: " + exception.Message + ")";
        }
    }

    private static void Close()
    {
        lock (sync)
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Dispose();
                writer = null;
            }
        }
    }

    // =========================================================
    // WRITE API
    // =========================================================

    public static void Write(
        string source,
        string message)
    {
        if (!Enabled)
            return;

        Event(LogCat.Match, $"{source}: {message}");
    }

    public static void Event(
        LogCat cat,
        string message)
    {
        Add(cat, LogType.Log, message, true);
    }

    public static void Warn(
        LogCat cat,
        string message)
    {
        Add(cat, LogType.Warning, message, true);
    }

    public static void Error(
        LogCat cat,
        string message)
    {
        Add(cat, LogType.Error, message, true);
    }

    private static void Add(
        LogCat cat,
        LogType level,
        string message,
        bool echo)
    {
        if (!initialized)
            Init();

        LogEntry entry = new LogEntry
        {
            gameTime = Time.time,
            realTime = Time.realtimeSinceStartup,
            frame = Time.frameCount,
            turn = Turn,
            role = Role(),
            cat = cat,
            level = level,
            message = message ?? string.Empty
        };

        lock (sync)
        {
            entry.id = nextId++;

            entries.Add(entry);

            if (entries.Count > MaxEntries)
                entries.RemoveRange(0, MaxEntries / 10);

            if (entry.IsWarning)
                WarningCount++;

            if (entry.IsError)
                ErrorCount++;

            Version++;

            if (writer != null)
                writer.WriteLine(Format(entry));
        }

        if (!echo || !EchoToConsole)
            return;

        string line = "[GL] " + Format(entry);

        if (entry.IsError)
            Debug.LogError(line);
        else if (entry.IsWarning)
            Debug.LogWarning(line);
        else
            Debug.Log(line);
    }

    private static void OnUnityLog(
        string condition,
        string stackTrace,
        LogType type)
    {
        // our own echo: already counted
        if (condition != null &&
            condition.StartsWith("[GL]"))
        {
            return;
        }

        string message = condition;

        if (type == LogType.Exception &&
            !string.IsNullOrEmpty(stackTrace))
        {
            string[] lines =
                stackTrace.Split('\n');

            int count =
                Mathf.Min(4, lines.Length);

            message +=
                "\n    " +
                string.Join(
                    "\n    ",
                    lines,
                    0,
                    count);
        }

        Add(
            LogCat.Unity,
            type,
            message,
            false);
    }

    // =========================================================
    // READ API
    // =========================================================

    public static string Format(
        LogEntry e)
    {
        string level =
            e.IsError
                ? "ERR "
                : e.IsWarning
                    ? "WARN"
                    : "    ";

        return
            $"#{e.id,6} t={e.gameTime,8:0.00} r={e.realTime,8:0.00} " +
            $"f={e.frame,6} {e.role,-4} T{e.turn,-3} " +
            $"{e.cat,-8} {level} {e.message}";
    }

    /// <summary>Newest 'max' lines accepted by 'filter' (oldest first).</summary>
    public static void Snapshot(
        List<LogEntry> destination,
        int max,
        Func<LogEntry, bool> filter)
    {
        destination.Clear();

        lock (sync)
        {
            for (int i = entries.Count - 1;
                 i >= 0 &&
                 destination.Count < max;
                 i--)
            {
                if (filter == null ||
                    filter(entries[i]))
                {
                    destination.Add(entries[i]);
                }
            }
        }

        destination.Reverse();
    }

    public static string AllText()
    {
        StringBuilder builder =
            new StringBuilder();

        lock (sync)
        {
            foreach (LogEntry entry in entries)
                builder.AppendLine(Format(entry));
        }

        return builder.ToString();
    }

    public static void Clear()
    {
        lock (sync)
        {
            entries.Clear();
            ErrorCount = 0;
            WarningCount = 0;
            Version++;
        }
    }

    /// <summary>Writes a text file next to the session log. Returns its path.</summary>
    public static string SaveTextFile(
        string fileName,
        string content)
    {
        try
        {
            string dir =
                string.IsNullOrEmpty(FilePath)
                    ? Application.persistentDataPath
                    : Path.GetDirectoryName(FilePath);

            string path =
                Path.Combine(
                    dir,
                    fileName);

            File.WriteAllText(
                path,
                content,
                new UTF8Encoding(false));

            return path;
        }
        catch (Exception exception)
        {
            return "(save failed: " + exception.Message + ")";
        }
    }

    // =========================================================
    // FORMAT HELPERS (used by the game scripts)
    // =========================================================

    private static string Role()
    {
        bool server = NetworkServer.active;
        bool client = NetworkClient.active;

        if (server && client)
            return "HOST";

        if (server)
            return "SRV";

        if (client)
            return "CLI";

        return "OFF";
    }

    /// <summary>Player as  nickname#seat.</summary>
    public static string P(
        Player player)
    {
        if (player == null)
            return "null";

        return $"{player.SteamNickname}#{player.SeatIndex}";
    }

    /// <summary>Card as  CardType@netId  (class).</summary>
    public static string C(
        Card card)
    {
        if (card == null)
            return "null";

        string type =
            card.Data != null
                ? card.Data.name
                : "noData";

        string cardClass =
            card.Data != null
                ? card.Data.CardClass.ToString()
                : "?";

        return $"{type}@{card.netId}({cardClass})";
    }
}
