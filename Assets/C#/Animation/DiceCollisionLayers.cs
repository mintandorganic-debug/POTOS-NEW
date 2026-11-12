#if UNITY_EDITOR
using UnityEngine;

public static class DiceCollisionLayers
{
    public const string LayerName = "DiceSimulation";

    public static int LayerId =>
        LayerMask.NameToLayer(
            LayerName);

    public static void Apply()
    {
        int diceLayer =
            LayerId;

        if (diceLayer < 0)
        {
            return;
        }

        for (int i = 0;
             i < 32;
             i++)
        {
            if (i != diceLayer)
            {
                Physics.IgnoreLayerCollision(
                    diceLayer,
                    i,
                    true);
            }
        }

        GameLog.Write(
            "DiceCollisionLayers",
            "Apply: dice collide only with the plate and each other");
    }
}
#endif
