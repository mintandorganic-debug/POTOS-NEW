using System;

// Tuning values of the dice roll. Defaults are the values of the original LoadedDice.
[Serializable]
public class DiceRollSettings
{
    public int[] score = { 1, 2, 3 };
    public float DieRadius = 0.04f;
    public float FollowWeight = 1.0f;
    public float TweakForce = 5.0f;
    public float GatherForce = 10f;
    public float PlateRadius = 0.5f;
    public float CatchRadius = 3f;
    public float MinVelocity = 0.2f;
    public float DiceAppearSpeed = 1.5f;
    public float DiceDisappearSpeed = 0.5f;
    public float RotFrameRate = 0.1f;
    public float ScalePulse = 0.15f;
    public float FlashDuration = 0.33f;
    public float ForceVariation = 0.33f;
    public float VerticalLimit = 2f;
    public float ChargeMin = 0.75f;
    public float ChargeMax = 1.5f;
}
