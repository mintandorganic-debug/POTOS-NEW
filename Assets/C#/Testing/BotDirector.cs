using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public enum BotMode
{
    /// <summary>Never acts (a target dummy). Its turn is skipped after a pause.</summary>
    Passive,

    /// <summary>No intelligence: random legal choices.</summary>
    Random,

    /// <summary>Simple heuristics: defends, picks the richest victim.</summary>
    Smart
}

/// <summary>
/// Spawns bot players and plays for them. Server only.
///
/// Bots are real Player objects without a network connection. They act through
/// the same server entry points the clients use (AttackSequence.ServerPlayCard /
/// ServerSetTarget / ServerCancel, GameMaster.TryStealAccesory).
/// </summary>
public class BotDirector : MonoBehaviour
{
    public class Bot
    {
        public Player player;
        public BotMode mode;

        // thinking state
        public string key;
        public float actAt;
        public bool done;
        public int attempts;
    }

    public readonly List<Bot> Bots = new List<Bot>();

    [Header("AI")]
    public bool AIEnabled = true;

    [Tooltip("Thinking time in game seconds (min, max).")]
    public Vector2 thinkTime = new Vector2(0.4f, 1.2f);

    [Tooltip("A Passive bot skips its own turn after this many seconds.")]
    public float passiveTurnSeconds = 2f;

    [Tooltip("Give a bot new cards when it has no attack card.")]
    public bool autoRedeal = true;

    [Header("Chaos (validation fuzzing)")]
    [Tooltip("Bots sometimes try ILLEGAL actions. Every accepted illegal " +
             "action is logged as an ERROR (= a hole in server validation).")]
    public bool chaos;

    [Range(0f, 1f)]
    public float chaosChance = 0.15f;

    private int botCounter;

    // =========================================================
    // BOT LIST
    // =========================================================

    public Bot Find(
        Player player)
    {
        if (player == null)
            return null;

        for (int i = 0; i < Bots.Count; i++)
        {
            if (Bots[i].player == player)
                return Bots[i];
        }

        return null;
    }

    public bool IsBot(
        Player player)
    {
        return Find(player) != null;
    }

    public Player Spawn(
        BotMode mode)
    {
        if (!NetworkServer.active ||
            NetworkManager.singleton == null ||
            NetworkManager.singleton.playerPrefab == null)
        {
            GameLog.Warn(
                LogCat.Bot,
                "Cannot spawn a bot: the host is not running.");

            return null;
        }

        if (GameMaster.Instance != null &&
            GameMaster.Instance.IsMatchRunning)
        {
            GameLog.Warn(
                LogCat.Bot,
                "Cannot spawn a bot during a match.");

            return null;
        }

        GameObject bodyObject =
            Instantiate(
                NetworkManager.singleton.playerPrefab);

        if (!bodyObject.TryGetComponent(
                out Player player))
        {
            Destroy(bodyObject);
            return null;
        }

        botCounter++;

        string nickname =
            $"BOT-{botCounter}";

        player.SetSteamData(
            0UL,
            nickname);

        bodyObject.name =
            $"Player [{nickname}]";

        NetworkServer.Spawn(
            bodyObject);

        Bots.Add(
            new Bot
            {
                player = player,
                mode = mode
            });

        GameLog.Event(
            LogCat.Bot,
            $"Bot spawned: {nickname} ({mode})");

        return player;
    }

    public void Remove(
        Bot bot)
    {
        if (bot == null)
            return;

        Bots.Remove(bot);

        if (bot.player != null)
        {
            GameLog.Event(
                LogCat.Bot,
                $"Bot removed: {bot.player.SteamNickname}");

            NetworkServer.Destroy(
                bot.player.gameObject);
        }
    }

    public void Clear()
    {
        foreach (Bot bot in Bots.ToList())
            Remove(bot);
    }

    /// <summary>Forget bots whose objects have been destroyed.</summary>
    private void Prune()
    {
        Bots.RemoveAll(b => b.player == null);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!NetworkServer.active ||
            !AIEnabled ||
            Bots.Count == 0)
        {
            return;
        }

        GameMaster gm =
            GameMaster.Instance;

        AttackSequence seq =
            AttackSequence.Instance;

        if (gm == null ||
            seq == null ||
            !gm.IsMatchRunning)
        {
            return;
        }

        Prune();

        IReadOnlyList<Player> players =
            gm.Players;

        if (gm.CurrentPlayerIndex < 0 ||
            gm.CurrentPlayerIndex >= players.Count)
        {
            return;
        }

        Player current =
            players[gm.CurrentPlayerIndex];

        Bot actor = null;

        switch (gm.CurrentPhase)
        {
            case GameMaster.GamePhase.Attack:
                actor = Find(current);
                break;

            case GameMaster.GamePhase.SelectTarget:
                actor = seq.IsActive
                    ? Find(seq.Attacker)
                    : null;
                break;

            case GameMaster.GamePhase.Defense:
                actor = Find(gm.SelectedTarget);
                break;

            case GameMaster.GamePhase.Steal:
                actor = Find(current);
                break;
        }

        if (actor == null ||
            actor.player == null)
        {
            return;
        }

        string key =
            $"{gm.TurnNumber}:{gm.CurrentPhase}";

        if (actor.key != key)
        {
            actor.key = key;
            actor.done = false;
            actor.attempts = 0;

            float wait =
                actor.mode == BotMode.Passive
                    ? passiveTurnSeconds
                    : Random.Range(
                        thinkTime.x,
                        thinkTime.y);

            actor.actAt =
                Time.time + wait;

            GameLog.Event(
                LogCat.Bot,
                $"{actor.player.SteamNickname} ({actor.mode}) " +
                $"thinks {wait:0.0}s in {gm.CurrentPhase}");
        }

        if (actor.done ||
            Time.time < actor.actAt)
        {
            return;
        }

        switch (gm.CurrentPhase)
        {
            case GameMaster.GamePhase.Attack:
                ActAttack(actor, gm, seq);
                break;

            case GameMaster.GamePhase.SelectTarget:
                ActSelectTarget(actor, gm, seq);
                break;

            case GameMaster.GamePhase.Defense:
                ActDefense(actor, seq);
                break;

            case GameMaster.GamePhase.Steal:
                ActSteal(actor, gm);
                break;
        }
    }

    private void Retry(
        Bot bot)
    {
        bot.attempts++;
        bot.actAt = Time.time + 0.3f;
    }

    // =========================================================
    // ATTACK PHASE
    // =========================================================

    private void ActAttack(
        Bot bot,
        GameMaster gm,
        AttackSequence seq)
    {
        if (bot.mode == BotMode.Passive)
        {
            bot.done = true;

            GameLog.Event(
                LogCat.Bot,
                $"{bot.player.SteamNickname} (Passive) skips its turn");

            gm.EndTurn();
            return;
        }

        List<Card> hand =
            TestTools.HandOf(bot.player);

        if (chaos &&
            bot.attempts == 0 &&
            Random.value < chaosChance)
        {
            DoChaos(bot, hand, gm, seq);
        }

        List<Card> attackCards =
            hand
                .Where(
                    c => c.Data != null &&
                         c.IsAvailable &&
                         !c.IsInRoulette &&
                         (c.Data.CardClass == Card.CardClass.AttackLight ||
                          c.Data.CardClass == Card.CardClass.AttackSteal))
                .ToList();

        if (attackCards.Count == 0)
        {
            if (autoRedeal &&
                bot.attempts < 3)
            {
                GameLog.Event(
                    LogCat.Bot,
                    $"{bot.player.SteamNickname} has no attack card: redeal");

                TestTools.Redeal(bot.player);
                Retry(bot);
                bot.actAt = Time.time + 0.5f;
                return;
            }

            GameLog.Warn(
                LogCat.Bot,
                $"{bot.player.SteamNickname} has no attack card: skips turn");

            bot.done = true;
            gm.EndTurn();
            return;
        }

        Card pick;

        if (bot.mode == BotMode.Smart)
        {
            // prefer steal cards while somebody has an accessory
            pick =
                attackCards.FirstOrDefault(
                    c => c.Data.CardClass ==
                         Card.CardClass.AttackSteal) ??
                attackCards[0];
        }
        else
        {
            pick =
                attackCards[Random.Range(0, attackCards.Count)];
        }

        bool ok =
            seq.ServerPlayCard(
                pick,
                bot.player);

        GameLog.Event(
            LogCat.Bot,
            $"{bot.player.SteamNickname} plays {GameLog.C(pick)}: " +
            (ok ? "accepted" : "REFUSED"));

        if (ok)
        {
            bot.done = true;
            return;
        }

        Retry(bot);

        if (bot.attempts >= 5)
        {
            bot.done = true;

            GameLog.Warn(
                LogCat.Bot,
                $"{bot.player.SteamNickname} gave up attacking");
        }
    }

    // =========================================================
    // TARGET SELECTION
    // =========================================================

    private void ActSelectTarget(
        Bot bot,
        GameMaster gm,
        AttackSequence seq)
    {
        if (bot.mode == BotMode.Passive)
        {
            bot.done = true;
            return;
        }

        if (seq.Target != null)
        {
            bot.done = true;
            return;
        }

        List<Player> others =
            gm.Players
                .Where(p => p != null && p != bot.player)
                .ToList();

        if (others.Count == 0)
        {
            GameLog.Event(
                LogCat.Bot,
                $"{bot.player.SteamNickname} has nobody to attack: cancels");

            seq.ServerCancel(bot.player);
            bot.done = true;
            return;
        }

        if (bot.mode == BotMode.Random &&
            Random.value < 0.08f)
        {
            GameLog.Event(
                LogCat.Bot,
                $"{bot.player.SteamNickname} changes its mind: cancels");

            seq.ServerCancel(bot.player);
            bot.done = true;
            return;
        }

        Player victim;

        if (bot.mode == BotMode.Smart)
        {
            victim =
                others
                    .OrderByDescending(
                        p => TestTools.AccessoryCount(p))
                    .First();
        }
        else
        {
            victim =
                others[Random.Range(0, others.Count)];
        }

        bool ok =
            seq.ServerSetTarget(
                bot.player,
                victim.GetComponent<PlayerInteractable>());

        GameLog.Event(
            LogCat.Bot,
            $"{bot.player.SteamNickname} targets {GameLog.P(victim)}: " +
            (ok ? "accepted" : "refused"));

        if (ok)
        {
            bot.done = true;
            return;
        }

        Retry(bot);

        if (bot.attempts >= 6)
        {
            seq.ServerCancel(bot.player);
            bot.done = true;
        }
    }

    // =========================================================
    // DEFENSE
    // =========================================================

    private void ActDefense(
        Bot bot,
        AttackSequence seq)
    {
        bot.done = true;

        if (bot.mode == BotMode.Passive)
            return;

        float chance =
            bot.mode == BotMode.Smart
                ? 1f
                : 0.5f;

        if (Random.value > chance)
        {
            GameLog.Event(
                LogCat.Bot,
                $"{bot.player.SteamNickname} does not defend");

            return;
        }

        Card defence =
            TestTools.HandOf(bot.player)
                .FirstOrDefault(
                    c => c.Data != null &&
                         c.Data.CardClass == Card.CardClass.Defence &&
                         c.IsAvailable &&
                         !c.IsInRoulette);

        if (defence == null)
        {
            GameLog.Event(
                LogCat.Bot,
                $"{bot.player.SteamNickname} has no usable defence card");

            return;
        }

        bool ok =
            seq.ServerPlayCard(
                defence,
                bot.player);

        GameLog.Event(
            LogCat.Bot,
            $"{bot.player.SteamNickname} defends with " +
            $"{GameLog.C(defence)}: " +
            (ok ? "accepted" : "REFUSED"));
    }

    // =========================================================
    // STEAL
    // =========================================================

    private void ActSteal(
        Bot bot,
        GameMaster gm)
    {
        bot.done = true;

        if (bot.mode == BotMode.Passive)
            return;

        Player victim =
            gm.SelectedTarget;

        if (victim == null)
            return;

        List<int> owned = new List<int>();

        for (int i = 0; i < 5; i++)
        {
            if (victim.HasAccessory(i))
                owned.Add(i);
        }

        if (owned.Count == 0)
        {
            GameLog.Event(
                LogCat.Bot,
                $"{bot.player.SteamNickname}: {GameLog.P(victim)} " +
                "has nothing to steal");

            return;
        }

        int index =
            bot.mode == BotMode.Smart
                ? owned[0]
                : owned[Random.Range(0, owned.Count)];

        bool ok =
            gm.TryStealAccesory(
                victim,
                index,
                bot.player);

        GameLog.Event(
            LogCat.Bot,
            $"{bot.player.SteamNickname} steals accessory {index} " +
            $"from {GameLog.P(victim)}: " +
            (ok ? "accepted" : "REFUSED"));
    }

    // =========================================================
    // CHAOS
    // =========================================================

    private void DoChaos(
        Bot bot,
        List<Card> hand,
        GameMaster gm,
        AttackSequence seq)
    {
        string description;
        bool accepted;

        switch (Random.Range(0, 5))
        {
            case 0:
            {
                Card defence =
                    hand.FirstOrDefault(
                        c => c.Data != null &&
                             c.Data.CardClass == Card.CardClass.Defence);

                if (defence == null)
                    return;

                description = "play a DEFENCE card in the Attack phase";

                accepted =
                    seq.ServerPlayCard(
                        defence,
                        bot.player);

                break;
            }

            case 1:
            {
                Card foreign =
                    TestTools.AllCards()
                        .FirstOrDefault(
                            c => c.OwnerNetId != bot.player.netId);

                if (foreign == null)
                    return;

                description = "play SOMEBODY ELSE'S card";

                accepted =
                    seq.ServerPlayCard(
                        foreign,
                        bot.player);

                break;
            }

            case 2:
            {
                Player other =
                    gm.Players.FirstOrDefault(
                        p => p != null && p != bot.player);

                if (other == null)
                    return;

                description = "steal an accessory outside the Steal phase";

                accepted =
                    gm.TryStealAccesory(
                        other,
                        0,
                        bot.player);

                break;
            }

            case 3:
            {
                description = "target MYSELF";

                accepted =
                    seq.ServerSetTarget(
                        bot.player,
                        bot.player.GetComponent<PlayerInteractable>());

                break;
            }

            default:
            {
                description = "cancel when no attack is running";

                accepted =
                    seq.ServerCancel(
                        bot.player);

                break;
            }
        }

        if (accepted)
        {
            GameLog.Error(
                LogCat.Bot,
                $"CHAOS: illegal action ACCEPTED by the server: " +
                $"{bot.player.SteamNickname} tried to {description}");
        }
        else
        {
            GameLog.Event(
                LogCat.Bot,
                $"CHAOS rejected (ok): {bot.player.SteamNickname} " +
                $"tried to {description}");
        }
    }
}
