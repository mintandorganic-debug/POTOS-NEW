public class AttackState
{
    public Card Card { get; set; }

    public Player Attacker { get; set; }

    public PlayerInteractable Target { get; set; }

    public bool IsActive { get; set; }

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

        Card = null;
        Attacker = null;
        Target = null;
        IsActive = false;
    }
}
