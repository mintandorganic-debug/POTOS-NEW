using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using UnityEngine;

/// <summary>
/// AUTO MODE. Plays the game by itself between bots at high speed and checks
/// every outcome. It walks through a matrix of scenarios:
///
///   for every attack card type  A:
///       Hit              (attack-light cards)         attack lands, no defence
///       StealSuccess     (attack-steal cards)         attack lands, accessory stolen
///       StealTimeout     (attack-steal cards)         attack lands, nobody steals
///       CancelBeforeTarget / CancelAfterTarget / CancelTimeout
///       Defended(A, D)   for every defence card type D
///
/// plus enough extra attacks so that every ordered pair (attacker seat, victim
/// seat) has been used at least once.
///
/// After each scenario the invariants are checked (no stuck state, the turn
/// advanced exactly once, cards consumed or returned, accessories conserved,
/// no Unity error was logged...). Everything goes to GameLog.
/// </summary>
public class AutoTester : MonoBehaviour
{
    public enum RunMode
    {
        /// <summary>Run the whole matrix once, then stop.</summary>
        OnePass,

        /// <summary>Repeat the matrix until stopped (soak test).</summary>
        Soak
    }

    public enum Kind
    {
        Hit,
        StealSuccess,
        StealTimeout,
        Defended,
        CancelBeforeTarget,
        CancelAfterTarget,
        CancelTimeout
    }

    public class Scenario
    {
        public Kind kind;
        public int attackType;
        public int defenseType = -1;
        public string attackName;
        public string defenseName;

        public bool IsCancel =>
            kind == Kind.CancelBeforeTarget ||
            kind == Kind.CancelAfterTarget ||
            kind == Kind.CancelTimeout;

        public string Key =>
            defenseName != null
                ? $"{kind}({attackName} vs {defenseName})"
                : $"{kind}({attackName})";
    }

    public class Result
    {
        public string key;
        public bool pass;
        public string details;
        public float gameSeconds;
        public int attackerSeat;
        public int victimSeat;
    }

    [Header("Settings")]
    public RunMode runMode = RunMode.OnePass;

    [Range(2, 5)]
    public int playerCount = 3;

    [Range(1f, 30f)]
    public float speed = 10f;

    [Header("State (read only)")]
    public bool IsRunning;
    public string Status = "idle";
    public int TotalScenarios;
    public int DoneScenarios;
    public int Passed;
    public int Failed;
    public string ReportPath = string.Empty;

    public readonly List<Result> Results = new List<Result>();

    [System.NonSerialized]
    public int[,] PairCounts = new int[5, 5];

    private Coroutine routine;
    private bool waitOk;
    private float runStart;
    private TestSceneController controller;
    private BotDirector bots;

    // =========================================================
    // CONTROL
    // =========================================================

    public void Begin()
    {
        if (IsRunning)
            return;

        controller = GetComponent<TestSceneController>();
        bots = GetComponent<BotDirector>();

        routine = StartCoroutine(Run());
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        GameLog.Warn(
            LogCat.Auto,
            "Auto test stopped by the user.");

        if (routine != null)
            StopCoroutine(routine);

        routine = null;

        Cleanup("stopped");
    }

    private void Cleanup(string reason)
    {
        IsRunning = false;
        Status = reason;

        Time.timeScale = 1f;

        if (bots != null)
            bots.AIEnabled = true;

        if (NetworkServer.active &&
            AttackSequence.Instance != null &&
            AttackSequence.Instance.IsActive)
        {
            AttackSequence.Instance.ServerForceReset();
        }
    }

    // =========================================================
    // MAIN LOOP
    // =========================================================

    private IEnumerator Run()
    {
        IsRunning = true;
        Results.Clear();
        Array.Clear(PairCounts, 0, PairCounts.Length);

        DoneScenarios = 0;
        Passed = 0;
        Failed = 0;
        ReportPath = string.Empty;

        runStart = Time.realtimeSinceStartup;

        GameLog.Event(
            LogCat.Auto,
            $"=== AUTO TEST START: mode={runMode}, players={playerCount}, " +
            $"speed=x{speed:0}");

        if (bots != null)
            bots.AIEnabled = false;

        Time.timeScale = speed;

        yield return StartCoroutine(PrepareMatch());

        if (!waitOk)
        {
            Finish("could not start a match");
            yield break;
        }

        List<Scenario> scenarios =
            BuildScenarios();

        if (scenarios.Count == 0)
        {
            GameLog.Error(
                LogCat.Auto,
                "No scenarios: the card type list is empty.");

            Finish("no scenarios");
            yield break;
        }

        TotalScenarios = scenarios.Count;

        int pass = 0;

        do
        {
            pass++;

            GameLog.Event(
                LogCat.Auto,
                $"--- pass {pass}: {scenarios.Count} scenarios");

            foreach (Scenario scenario in scenarios)
            {
                if (!IsRunning)
                    yield break;

                yield return StartCoroutine(
                    RunScenario(scenario));

                DoneScenarios++;
            }

            // make sure every attacker/victim seat pair was used
            int extra = 0;

            while (!AllPairsCovered() &&
                   extra < 40 &&
                   IsRunning)
            {
                extra++;

                Scenario filler =
                    scenarios.FirstOrDefault(
                        s => !s.IsCancel &&
                             s.kind != Kind.Defended) ??
                    scenarios[0];

                yield return StartCoroutine(
                    RunScenario(filler));

                DoneScenarios++;
            }
        }
        while (runMode == RunMode.Soak &&
               IsRunning);

        Finish("complete");
    }

    // =========================================================
    // SETUP
    // =========================================================

    private IEnumerator PrepareMatch()
    {
        waitOk = false;

        GameMaster gm =
            GameMaster.Instance;

        if (gm == null ||
            !NetworkServer.active ||
            controller == null ||
            bots == null)
        {
            Status = "start the host first";

            GameLog.Error(
                LogCat.Auto,
                "Auto test needs a running host.");

            yield break;
        }

        if (gm.IsMatchRunning)
            controller.ResetMatch();

        yield return null;

        bots.Clear();

        yield return null;

        int humans =
            controller.CollectPlayers().Count;

        int need =
            Mathf.Clamp(playerCount, 2, 5) - humans;

        for (int i = 0; i < need; i++)
            bots.Spawn(BotMode.Passive);

        yield return new WaitForSeconds(0.5f);

        if (!controller.StartMatch(out string error))
        {
            GameLog.Error(
                LogCat.Auto,
                "Cannot start the match: " + error);

            yield break;
        }

        yield return new WaitForSeconds(1f);

        waitOk = true;
    }

    private List<Scenario> BuildScenarios()
    {
        List<Scenario> list =
            new List<Scenario>();

        Card sample =
            TestTools.AllCards().FirstOrDefault();

        if (sample == null ||
            sample.AvailableTypes == null)
        {
            return list;
        }

        IReadOnlyList<CardData> types =
            sample.AvailableTypes;

        for (int a = 0; a < types.Count; a++)
        {
            CardData attack = types[a];

            if (attack == null ||
                (attack.CardClass != Card.CardClass.AttackLight &&
                 attack.CardClass != Card.CardClass.AttackSteal))
            {
                continue;
            }

            if (attack.CardClass == Card.CardClass.AttackLight)
            {
                list.Add(New(Kind.Hit, a, -1, types));
            }
            else
            {
                list.Add(New(Kind.StealSuccess, a, -1, types));
                list.Add(New(Kind.StealTimeout, a, -1, types));
            }

            list.Add(New(Kind.CancelBeforeTarget, a, -1, types));
            list.Add(New(Kind.CancelAfterTarget, a, -1, types));
            list.Add(New(Kind.CancelTimeout, a, -1, types));

            for (int d = 0; d < types.Count; d++)
            {
                if (types[d] != null &&
                    types[d].CardClass == Card.CardClass.Defence)
                {
                    list.Add(New(Kind.Defended, a, d, types));
                }
            }
        }

        return list;
    }

    private static Scenario New(
        Kind kind,
        int attackType,
        int defenseType,
        IReadOnlyList<CardData> types)
    {
        return new Scenario
        {
            kind = kind,
            attackType = attackType,
            defenseType = defenseType,
            attackName = types[attackType].name,
            defenseName =
                defenseType >= 0
                    ? types[defenseType].name
                    : null
        };
    }

    // =========================================================
    // ONE SCENARIO
    // =========================================================

    private IEnumerator RunScenario(
        Scenario sc)
    {
        GameMaster gm = GameMaster.Instance;
        AttackSequence seq = AttackSequence.Instance;
        RouletteRotator roulette = RouletteRotator.Instance;

        List<string> problems = new List<string>();

        int errorsBefore = GameLog.ErrorCount;
        float started = Time.time;

        Player attacker = null;
        Player victim = null;
        Card attackCard = null;
        Card defenceCard = null;

        int attackerSeat = -1;
        int victimSeat = -1;

        bool stolen = false;
        int stolenIndex = -1;
        int turnBefore = 0;
        int accessoriesBefore = 0;
        int thiefBefore = 0;
        int victimBefore = 0;

        Status = "running " + sc.Key;

        GameLog.Event(
            LogCat.Auto,
            $"--- SCENARIO START: {sc.Key}");

        do
        {
            if (gm == null || seq == null || roulette == null)
            {
                problems.Add("GameMaster / AttackSequence / Roulette missing");
                break;
            }

            // ---------- wait for a calm Attack phase
            yield return StartCoroutine(
                WaitFor(
                    () => gm.CurrentPhase == GameMaster.GamePhase.Attack &&
                          !seq.IsActive,
                    30f));

            if (!waitOk)
            {
                problems.Add(
                    $"never reached an idle Attack phase " +
                    $"(phase={gm.CurrentPhase}, active={seq.IsActive})");

                break;
            }

            // ---------- clean hands
            TestTools.RedealAll(gm.Players);

            yield return new WaitForSeconds(0.4f);

            attacker = gm.Players[gm.CurrentPlayerIndex];
            attackerSeat = attacker.SeatIndex;

            victim = PickVictim(gm, attacker);

            if (victim == null)
            {
                problems.Add("no victim available");
                break;
            }

            victimSeat = victim.SeatIndex;

            // ---------- force the cards of the scenario
            attackCard =
                ForceHand(
                    attacker,
                    sc.attackType,
                    null);

            if (attackCard == null)
            {
                problems.Add("could not give the attacker the attack card");
                break;
            }

            if (sc.defenseType >= 0)
            {
                defenceCard =
                    ForceHand(
                        victim,
                        sc.defenseType,
                        null);

                if (defenceCard == null)
                {
                    problems.Add("could not give the victim the defence card");
                    break;
                }
            }

            // a victim must not own a defence card unless the scenario wants one
            StripDefence(victim, defenceCard);
            StripDefence(attacker, null);

            yield return new WaitForSeconds(0.2f);

            turnBefore = gm.TurnNumber;
            accessoriesBefore = TotalAccessories(gm);
            thiefBefore = TestTools.AccessoryCount(attacker);
            victimBefore = TestTools.AccessoryCount(victim);

            GameLog.Event(
                LogCat.Auto,
                $"attacker {GameLog.P(attacker)}, victim {GameLog.P(victim)}, " +
                $"attack card {GameLog.C(attackCard)}" +
                (defenceCard != null
                    ? $", defence card {GameLog.C(defenceCard)}"
                    : string.Empty));

            // ---------- 1. play the attack card
            yield return StartCoroutine(
                WaitFor(
                    () => attackCard != null && attackCard.IsAvailable,
                    5f));

            if (!waitOk)
            {
                problems.Add("attack card never became available");
                break;
            }

            if (!seq.ServerPlayCard(attackCard, attacker))
            {
                problems.Add("attack card was rejected by the server");
                break;
            }

            // ---------- 2. cancel before / without a target
            if (sc.kind == Kind.CancelBeforeTarget)
            {
                yield return new WaitForSeconds(0.3f);

                if (!seq.ServerCancel(attacker))
                    problems.Add("cancel was rejected");
            }
            else if (sc.kind == Kind.CancelTimeout)
            {
                yield return StartCoroutine(
                    WaitFor(
                        () => !seq.IsActive,
                        seq.TargetSelectionTime + 10f));

                if (!waitOk)
                {
                    problems.Add(
                        "target-selection timeout did not cancel the sequence");
                }
            }
            else
            {
                // ---------- 3. choose the target
                bool targeted = false;
                float until = Time.time + 4f;

                while (Time.time < until)
                {
                    if (seq.ServerSetTarget(
                            attacker,
                            victim.GetComponent<PlayerInteractable>()))
                    {
                        targeted = true;
                        break;
                    }

                    yield return new WaitForSeconds(0.2f);
                }

                if (!targeted)
                {
                    problems.Add("target was rejected");
                    break;
                }

                if (sc.kind == Kind.CancelAfterTarget)
                {
                    yield return new WaitForSeconds(0.5f);

                    if (!seq.ServerCancel(attacker))
                        problems.Add("cancel after target was rejected");
                }
                else
                {
                    // ---------- 4. the roulette flies to the victim
                    yield return StartCoroutine(
                        WaitFor(
                            () => gm.CurrentPhase ==
                                  GameMaster.GamePhase.Defense,
                            40f));

                    if (!waitOk)
                    {
                        problems.Add(
                            "roulette never reached the victim " +
                            $"(phase={gm.CurrentPhase})");

                        break;
                    }

                    if (sc.kind == Kind.Defended)
                    {
                        yield return StartCoroutine(
                            WaitFor(
                                () => defenceCard != null &&
                                      defenceCard.IsAvailable,
                                5f));

                        if (!waitOk)
                        {
                            problems.Add(
                                "defence card is not available in the " +
                                "Defense phase");

                            break;
                        }

                        if (!seq.ServerPlayCard(defenceCard, victim))
                            problems.Add("defence card was rejected");
                    }
                    else if (sc.kind == Kind.StealSuccess ||
                             sc.kind == Kind.StealTimeout)
                    {
                        yield return StartCoroutine(
                            WaitFor(
                                () => gm.CurrentPhase ==
                                      GameMaster.GamePhase.Steal,
                                seq.DefenseWindowTime + 40f));

                        if (!waitOk)
                        {
                            problems.Add(
                                "Steal phase never started " +
                                $"(phase={gm.CurrentPhase})");

                            break;
                        }

                        if (sc.kind == Kind.StealSuccess)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                if (victim.HasAccessory(i))
                                {
                                    stolenIndex = i;
                                    break;
                                }
                            }

                            if (stolenIndex < 0)
                            {
                                problems.Add(
                                    "victim has no accessory to steal");
                            }
                            else
                            {
                                yield return new WaitForSeconds(0.3f);

                                stolen =
                                    gm.TryStealAccesory(
                                        victim,
                                        stolenIndex,
                                        attacker);

                                if (!stolen)
                                    problems.Add("steal was rejected");
                            }
                        }
                    }
                    // Hit: nothing to do, the attack lands by itself
                }
            }

            // ---------- 5. wait until everything is calm again
            float settle =
                seq.TargetSelectionTime +
                seq.DefenseWindowTime +
                seq.StealWindowTime +
                60f;

            yield return StartCoroutine(
                WaitFor(
                    () => gm.TurnNumber > turnBefore &&
                          gm.CurrentPhase == GameMaster.GamePhase.Attack &&
                          !seq.IsActive &&
                          !roulette.IsRunning,
                    settle));

            if (!waitOk)
            {
                problems.Add(
                    "STUCK: " +
                    $"phase={gm.CurrentPhase}, active={seq.IsActive}, " +
                    $"roulette={roulette.IsRunning}, " +
                    $"turn {turnBefore}->{gm.TurnNumber}");

                break;
            }

            // let late side effects (destroys, RPCs) finish
            yield return new WaitForSeconds(0.6f);

            // ---------- 6. invariants
            if (TestTools.AllCards().Any(c => c.IsInRoulette))
                problems.Add("a card is still flagged IsInRoulette");

            if (sc.IsCancel)
            {
                if (attackCard == null)
                {
                    problems.Add("the attack card was destroyed by a cancel");
                }
                else
                {
                    if (attackCard.OwnerNetId != attacker.netId)
                        problems.Add("cancelled card changed its owner");

                    if (attackCard.IsInRoulette)
                        problems.Add("cancelled card is still in the roulette");
                }
            }
            else
            {
                if (attackCard != null)
                    problems.Add("the attack card was not consumed");
            }

            if (sc.kind == Kind.Defended &&
                defenceCard != null)
            {
                problems.Add("the defence card was not consumed");
            }

            int turns = gm.TurnNumber - turnBefore;

            if (turns != 1)
            {
                problems.Add(
                    $"EndTurn effect happened {turns} times (expected 1)");
            }

            int accessoriesAfter = TotalAccessories(gm);

            if (accessoriesAfter != accessoriesBefore)
            {
                problems.Add(
                    $"accessory count changed {accessoriesBefore} -> " +
                    $"{accessoriesAfter}");
            }

            if (stolen)
            {
                if (TestTools.AccessoryCount(attacker) != thiefBefore + 1 ||
                    TestTools.AccessoryCount(victim) != victimBefore - 1)
                {
                    problems.Add(
                        "stolen accessory did not move from victim to thief");
                }
            }
            else if (sc.kind != Kind.StealSuccess)
            {
                if (TestTools.AccessoryCount(attacker) != thiefBefore ||
                    TestTools.AccessoryCount(victim) != victimBefore)
                {
                    problems.Add(
                        "accessories changed although nothing was stolen");
                }
            }
        }
        while (false);

        // ---------- recover if the scenario left a mess
        if (gm != null &&
            seq != null &&
            (seq.IsActive ||
             gm.CurrentPhase != GameMaster.GamePhase.Attack))
        {
            GameLog.Warn(
                LogCat.Auto,
                "Recovering after an unfinished scenario.");

            seq.ServerForceReset();
            gm.EndTurn();

            yield return new WaitForSeconds(0.5f);
        }

        // ---------- unity errors
        int newErrors =
            GameLog.ErrorCount - errorsBefore;

        if (newErrors > 0)
        {
            problems.Add(
                $"{newErrors} Unity error(s) / exception(s) were logged");
        }

        Result result = new Result
        {
            key = sc.Key,
            pass = problems.Count == 0,
            details = string.Join("; ", problems),
            gameSeconds = Time.time - started,
            attackerSeat = attackerSeat,
            victimSeat = victimSeat
        };

        Results.Add(result);

        if (attackerSeat >= 0 &&
            victimSeat >= 0 &&
            attackerSeat < 5 &&
            victimSeat < 5)
        {
            PairCounts[attackerSeat, victimSeat]++;
        }

        if (result.pass)
        {
            Passed++;

            GameLog.Event(
                LogCat.Auto,
                $"--- SCENARIO PASS: {sc.Key} ({result.gameSeconds:0.0}s)");
        }
        else
        {
            Failed++;

            GameLog.Error(
                LogCat.Auto,
                $"--- SCENARIO FAIL: {sc.Key}: {result.details}");
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private IEnumerator WaitFor(
        Func<bool> condition,
        float timeout)
    {
        float end =
            Time.time + timeout;

        while (!condition())
        {
            if (Time.time > end ||
                !IsRunning)
            {
                waitOk = false;
                yield break;
            }

            yield return null;
        }

        waitOk = true;
    }

    // victim = the other seat this attacker has hit least often
    private Player PickVictim(
        GameMaster gm,
        Player attacker)
    {
        List<Player> others =
            gm.Players
                .Where(p => p != null && p != attacker)
                .ToList();

        if (others.Count == 0)
            return null;

        int attackerSeat =
            Mathf.Clamp(attacker.SeatIndex, 0, 4);

        int best = int.MaxValue;
        List<Player> candidates = new List<Player>();

        foreach (Player other in others)
        {
            int count =
                PairCounts[
                    attackerSeat,
                    Mathf.Clamp(other.SeatIndex, 0, 4)];

            if (count < best)
            {
                best = count;
                candidates.Clear();
            }

            if (count == best)
                candidates.Add(other);
        }

        return candidates[
            UnityEngine.Random.Range(0, candidates.Count)];
    }

    // turns one card of the player into the given card type
    private static Card ForceHand(
        Player player,
        int typeIndex,
        Card exclude)
    {
        foreach (Card card in TestTools.HandOf(player))
        {
            if (card == exclude ||
                card.IsInRoulette)
            {
                continue;
            }

            if (card.SetDataByIndex(typeIndex))
                return card;
        }

        return null;
    }

    // changes every defence card of the player (except 'keep') into the
    // first non-defence type
    private static void StripDefence(
        Player player,
        Card keep)
    {
        foreach (Card card in TestTools.HandOf(player))
        {
            if (card == keep ||
                card.Data == null ||
                card.Data.CardClass != Card.CardClass.Defence)
            {
                continue;
            }

            for (int i = 0;
                 i < card.AvailableTypes.Count;
                 i++)
            {
                CardData type =
                    card.AvailableTypes[i];

                if (type != null &&
                    type.CardClass != Card.CardClass.Defence)
                {
                    card.SetDataByIndex(i);
                    break;
                }
            }
        }
    }

    private static int TotalAccessories(
        GameMaster gm)
    {
        return gm.Players
            .Where(p => p != null)
            .Sum(p => TestTools.AccessoryCount(p));
    }

    private bool AllPairsCovered()
    {
        GameMaster gm =
            GameMaster.Instance;

        if (gm == null)
            return true;

        int n = gm.Players.Count;

        for (int a = 0; a < n; a++)
        {
            for (int v = 0; v < n; v++)
            {
                if (a != v &&
                    PairCounts[a, v] == 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // =========================================================
    // REPORT
    // =========================================================

    private void Finish(
        string reason)
    {
        string report =
            BuildReport(reason);

        ReportPath =
            GameLog.SaveTextFile(
                $"autotest_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                report);

        GameLog.Event(
            LogCat.Auto,
            $"=== AUTO TEST END ({reason}): passed {Passed}, " +
            $"failed {Failed}. Report: {ReportPath}");

        Cleanup(
            Failed == 0
                ? $"done: all {Passed} passed"
                : $"done: {Failed} FAILED");
    }

    public string BuildReport(
        string reason)
    {
        StringBuilder text =
            new StringBuilder();

        text.AppendLine("AUTO TEST REPORT");
        text.AppendLine("================");
        text.AppendLine($"Result    : {reason}");
        text.AppendLine($"Mode      : {runMode}");
        text.AppendLine($"Players   : {playerCount}");
        text.AppendLine($"Speed     : x{speed:0}");
        text.AppendLine(
            $"Real time : {Time.realtimeSinceStartup - runStart:0.0}s");
        text.AppendLine($"Scenarios : {Results.Count} " +
                        $"(passed {Passed}, failed {Failed})");
        text.AppendLine($"Log file  : {GameLog.FilePath}");
        text.AppendLine();

        text.AppendLine("FAILED");
        text.AppendLine("------");

        bool anyFail = false;

        foreach (Result r in Results.Where(x => !x.pass))
        {
            anyFail = true;

            text.AppendLine(
                $"[FAIL] {r.key}  seat {r.attackerSeat}->{r.victimSeat}");

            text.AppendLine("       " + r.details);
        }

        if (!anyFail)
            text.AppendLine("(none)");

        text.AppendLine();
        text.AppendLine("ALL SCENARIOS");
        text.AppendLine("-------------");

        foreach (Result r in Results)
        {
            text.AppendLine(
                $"[{(r.pass ? "PASS" : "FAIL")}] {r.key}  " +
                $"seat {r.attackerSeat}->{r.victimSeat}  " +
                $"{r.gameSeconds:0.0}s");
        }

        text.AppendLine();
        text.AppendLine("SEAT PAIR COVERAGE (attacker row -> victim column)");

        int n =
            GameMaster.Instance != null
                ? GameMaster.Instance.Players.Count
                : playerCount;

        for (int a = 0; a < n; a++)
        {
            string row = $"seat {a}: ";

            for (int v = 0; v < n; v++)
            {
                row += a == v
                    ? "  - "
                    : $"{PairCounts[a, v],3} ";
            }

            text.AppendLine(row);
        }

        return text.ToString();
    }
}
