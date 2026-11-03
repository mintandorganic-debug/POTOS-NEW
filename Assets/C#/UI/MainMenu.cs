using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static MainMenu instance;

    [SerializeField] InputField ipInput;

    private SteamLobbySession steamSession;

    private void Start()
    {
        GameLog.Write(
            "MainMenu",
            $"Start ipInput={(ipInput != null ? ipInput.name : "NULL")}");

        Cursor.visible = true;

        instance = this;

        steamSession = new SteamLobbySession();
    }

    private void OnDestroy()
    {
        GameLog.Write(
            "MainMenu",
            $"OnDestroy isCurrentInstance={instance == this}");

        steamSession?.Dispose();
        steamSession = null;

        if (instance == this)
            instance = null;
    }

    // =========================================================
    // HOST / CLIENT
    // =========================================================

    public void HostSteam()
    {
        GameLog.Write(
            "MainMenu",
            "HostSteam button pressed");

        if (!NetTransportSelector.Set(NetTransportSelector.GetSteam()))
        {
            GameLog.Write(
                "MainMenu",
                "HostSteam aborted: Steam transport could not be set");

            return;
        }

        steamSession.CreateLobby();
    }

    public void HostIp()
    {
        GameLog.Write(
            "MainMenu",
            "HostIp button pressed");

        if (!NetTransportSelector.Set(NetTransportSelector.GetIp()))
        {
            GameLog.Write(
                "MainMenu",
                "HostIp aborted: IP transport could not be set");

            return;
        }

        GameLog.Write(
            "MainMenu",
            "HostIp StartHost");

        NetworkManager.singleton.StartHost();
    }

    public void Client()
    {
        GameLog.Write(
            "MainMenu",
            $"Client button pressed address={(ipInput != null ? ipInput.text : "NULL")}");

        if (!NetTransportSelector.Set(NetTransportSelector.GetIp()))
        {
            GameLog.Write(
                "MainMenu",
                "Client aborted: IP transport could not be set");

            return;
        }

        NetworkManager.singleton.networkAddress = ipInput.text;

        GameLog.Write(
            "MainMenu",
            $"Client StartClient address={NetworkManager.singleton.networkAddress}");

        NetworkManager.singleton.StartClient();
    }

    public void Server()
    {
        GameLog.Write(
            "MainMenu",
            "Server button pressed, StartServer");

        NetworkManager.singleton.StartServer();
    }

    public void Stop()
    {
        GameLog.Write(
            "MainMenu",
            $"Stop button pressed serverActive={NetworkServer.active} clientConnected={NetworkClient.isConnected}");

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            GameLog.Write(
                "MainMenu",
                "Stop StopHost");

            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            GameLog.Write(
                "MainMenu",
                "Stop StopClient");

            NetworkManager.singleton.StopClient();
        }
        else if (NetworkServer.active)
        {
            GameLog.Write(
                "MainMenu",
                "Stop StopServer");

            NetworkManager.singleton.StopServer();
        }
        else
        {
            GameLog.Write(
                "MainMenu",
                "Stop nothing to stop");
        }
    }

    public void CloseApp()
    {
        GameLog.Write(
            "MainMenu",
            "CloseApp Application.Quit");

        Application.Quit();
    }
}
