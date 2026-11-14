#if UNITY_EDITOR
// Test animation state: the test script switches the two flags by hand.
public class ScriptedDiceState : IDiceAnimationState
{
    public bool IsCharging { get; set; }

    public bool IsAttackOrReady { get; set; }
}
#endif
