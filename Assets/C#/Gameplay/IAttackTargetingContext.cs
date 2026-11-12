public interface IAttackTargetingContext
{
    void CancelSequence();

    bool HasAttackRoutine { get; }

    void StopAttackRoutine();

    void StartAttackRoutine();
}
