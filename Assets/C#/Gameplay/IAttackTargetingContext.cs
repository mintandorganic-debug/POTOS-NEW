public interface IAttackTargetingContext
{
    void CancelSequence();

    bool HasAttackRoutine { get; }

    bool TryPlayPreparedDefense(Player victim);

    void StopAttackRoutine();

    void StartAttackRoutine();
}
