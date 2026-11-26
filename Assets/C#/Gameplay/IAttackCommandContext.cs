public interface IAttackCommandContext
{
    void StartSequence(
        Card card,
        Player attacker);

    void StartDefenseSequence(
        Card defenseCard,
        Player defensePlayer);

    void SetTarget(
        PlayerInteractable target);

    void CancelSequence();
}
