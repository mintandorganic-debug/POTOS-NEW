using Mirror;
using System.Collections.Generic;
using UnityEngine;

public static class DebugDeal
{
    public static bool Enabled = true;

    public const int ColtIndex = 0;

    public static float AccessoryScale = 2f;

    public static Color AccessoryGlow =
        new Color(0.5f, 1.5f, 2f);

    private static readonly int EmissiveColor =
        Shader.PropertyToID("_EmissiveColor");

    private static readonly HashSet<GameObject> styledAccessories = new();

    public static int ExcludedIndex =>
        Enabled
            ? ColtIndex
            : -1;

    public static bool IsColtPlayer(
        Player player)
    {
        bool result =
            Enabled &&
            player != null &&
            NetworkServer.localConnection != null &&
            player.connectionToClient == NetworkServer.localConnection;

        GameLog.Write(
            "DebugDeal",
            $"IsColtPlayer player={(player != null ? player.netId.ToString() : "NULL")} enabled={Enabled} result={result}");

        return result;
    }

    public static void StyleAccessories(
        IReadOnlyList<GameObject> accessories)
    {
        if (!Enabled ||
            accessories == null)
        {
            return;
        }

        foreach (GameObject accessory in accessories)
        {
            if (accessory == null ||
                !styledAccessories.Add(accessory))
            {
                continue;
            }

            accessory.transform.localScale *=
                AccessoryScale;

            Renderer[] renderers =
                accessory.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                MaterialPropertyBlock block =
                    new MaterialPropertyBlock();

                renderer.GetPropertyBlock(block);

                block.SetColor(
                    EmissiveColor,
                    AccessoryGlow);

                renderer.SetPropertyBlock(block);
            }

            GameLog.Write(
                "DebugDeal",
                $"StyleAccessories accessory={accessory.name} scale={AccessoryScale} renderers={renderers.Length}");
        }
    }
}
