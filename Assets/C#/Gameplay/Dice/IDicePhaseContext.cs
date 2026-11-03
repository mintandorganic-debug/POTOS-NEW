public interface IDicePhaseContext : IAttackSelectionContext
{
    void AdvanceTurn();

    void NotifyDicePhaseStarted(
        uint playerNetId,
        float duration);

    void NotifyDiceRolled(
        uint playerNetId,
        int first,
        int second,
        int third,
        float power);
}
