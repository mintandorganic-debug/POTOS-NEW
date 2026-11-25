public interface IAttackCommandContext
{
    void StartSequence(
        Card card,
        Player attacker);

    bool TryPlayDefense(
        Card defenseCard,
        Player defensePlayer);

    void SetTarget(
        PlayerInteractable target);

    void CancelSequence();
}
