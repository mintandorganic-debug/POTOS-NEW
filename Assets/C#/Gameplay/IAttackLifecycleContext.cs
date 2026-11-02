using Mirror;

public interface IAttackLifecycleContext
{
    bool HasAttackRoutine { get; }

    void StopAttackRoutine();

    void ReleaseAttackRoutine();

    void StartAttackPreparation(
        NetworkIdentity attackerIdentity,
        string actionId);

    void TriggerAttackAnimation(
        NetworkIdentity attackerIdentity);

    void CancelAttackAnimation(
        NetworkIdentity attackerIdentity);
}
