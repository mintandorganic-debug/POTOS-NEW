using Mirror;
using Mirror.FizzySteam;
using UnityEngine;

public static class NetTransportSelector
{
    // Transports are resolved from the persistent NetworkManager at runtime.
    // Inspector references would point at the scene copy that Mirror destroys
    // when the menu scene is reloaded.
    public static Transport GetSteam()
    {
        NetworkManager manager = NetworkManager.singleton;

        GameLog.Write(
            "NetTransportSelector",
            $"GetSteam manager={(manager != null ? manager.name : "NULL")}");

        return manager != null
            ? manager.GetComponentInChildren<FizzySteamworks>(true)
            : null;
    }

    public static Transport GetIp()
    {
        NetworkManager manager = NetworkManager.singleton;

        GameLog.Write(
            "NetTransportSelector",
            $"GetIp manager={(manager != null ? manager.name : "NULL")}");

        if (manager == null)
        {
            GameLog.Write(
                "NetTransportSelector",
                "GetIp no NetworkManager, returning NULL");

            return null;
        }

        foreach (Transport transport in
                 manager.GetComponentsInChildren<Transport>(true))
        {
            if (transport is not FizzySteamworks)
            {
                GameLog.Write(
                    "NetTransportSelector",
                    $"GetIp found transport={transport.GetType().Name}");

                return transport;
            }
        }

        GameLog.Write(
            "NetTransportSelector",
            "GetIp no non-Steam transport found, returning NULL");

        return null;
    }

    public static bool Set(Transport transport)
    {
        GameLog.Write(
            "NetTransportSelector",
            $"Set transport={(transport != null ? transport.GetType().Name : "NULL")} clientActive={NetworkClient.active} serverActive={NetworkServer.active}");

        if (NetworkManager.singleton is not NetManager manager)
        {
            GameLog.Write(
                "NetTransportSelector",
                "Set failed: NetManager is missing");

            Debug.LogError("MainMenu: NetManager is missing.");
            return false;
        }

        if (transport == null)
        {
            GameLog.Write(
                "NetTransportSelector",
                "Set failed: transport is missing on the persistent NetworkManager");

            Debug.LogError("MainMenu: transport is missing on the persistent NetworkManager.");
            return false;
        }

        if (NetworkClient.active || NetworkServer.active)
        {
            GameLog.Write(
                "NetTransportSelector",
                $"Set failed: network already active clientActive={NetworkClient.active} serverActive={NetworkServer.active}");

            Debug.LogWarning("MainMenu: network is already active, transport not changed.");
            return false;
        }

        manager.ChangeTransport(transport);
        Transport.active = transport;

        GameLog.Write(
            "NetTransportSelector",
            $"Set transport set to {transport.GetType().Name}");

        return true;
    }
}
