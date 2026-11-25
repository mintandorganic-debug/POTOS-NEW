using System.Collections.Generic;

public class AttackState
{
    private readonly Dictionary<uint, Card> preparedDefenses = new();

    public Card Card { get; set; }

    public Card ReflectedAttack { get; set; }

    public Player Attacker { get; set; }

    public PlayerInteractable Target { get; set; }

    public bool IsActive { get; set; }

    public bool DefenseOpen { get; set; }

    public bool PrepareDefense(Player player, Card card)
    {
        if (preparedDefenses.TryGetValue(player.netId, out Card prepared) &&
            prepared != null && prepared.IsDefensePrepared && prepared.OwnerNetId == player.netId)
        {
            return prepared == card;
        }

        preparedDefenses[player.netId] = card;
        card.SetDefensePrepared(true);
        return true;
    }

    public Card GetPreparedDefense(Player player)
    {
        return preparedDefenses.TryGetValue(player.netId, out Card card) &&
               card != null && card.IsDefensePrepared
            ? card
            : null;
    }

    public void ClearPreparedDefenses()
    {
        foreach (Card card in preparedDefenses.Values)
        {
            if (card != null)
                card.SetDefensePrepared(false);
        }

        preparedDefenses.Clear();
    }

    public void Begin(
        Card card,
        Player attacker)
    {
        GameLog.Write(
            "AttackState",
            $"Begin card={(card != null && card.Data != null ? card.Data.name : "NULL")} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

        Card = card;
        Attacker = attacker;
        Target = null;
        IsActive = true;
    }

    public void Reset()
    {
        GameLog.Write(
            "AttackState",
            $"Reset card={(Card != null && Card.Data != null ? Card.Data.name : "NULL")} attacker={(Attacker != null ? Attacker.netId.ToString() : "NULL")} target={(Target != null ? Target.name : "NULL")} wasActive={IsActive}");

        ClearPreparedDefenses();
        Card = null;
        ReflectedAttack = null;
        Attacker = null;
        Target = null;
        IsActive = false;
        DefenseOpen = false;
    }
}
