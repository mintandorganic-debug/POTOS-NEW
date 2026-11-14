public interface IDiceStageContext
{
    void SetPhase(
        GameMaster.GamePhase phase);

    void NotifyDiceStarted(
        uint playerNetId,
        float duration);

    void NotifyDiceRolled(
        uint playerNetId,
        int first,
        int second,
        int third,
        string combination,
        int chips,
        int coins,
        float power);
}
