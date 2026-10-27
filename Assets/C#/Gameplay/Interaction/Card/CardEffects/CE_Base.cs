using UnityEngine;

public abstract class CE_Base : ScriptableObject
{
    public abstract void ApplyEffectToTarget(Player player);

    public virtual void ApplyEffect(
        Player attacker,
        Player target)
    {
        ApplyEffectToTarget(target);
    }
}