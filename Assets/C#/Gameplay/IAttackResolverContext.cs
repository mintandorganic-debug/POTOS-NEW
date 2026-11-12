using Mirror;

public interface IAttackResolverContext
{
    void ShowDefenseTimer(
        NetworkConnectionToClient connection,
        float time);

    void SetDefenseTimer(
        NetworkConnectionToClient connection,
        float time);

    void HideDefenseTimer(
        NetworkConnectionToClient connection);

    void TriggerAttackAnimation(
        NetworkIdentity attackerIdentity);

    void StartDamagePass(
        NetworkConnectionToClient connection,
        NetworkIdentity victimIdentity);

    void StartAttackVortex(
        Card card,
        Player attackPlayer);

    void CancelSequence();

    void ReleaseAttackRoutine();
}
