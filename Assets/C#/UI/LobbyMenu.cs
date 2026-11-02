using Mirror;
using Steamworks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour
{
    public static LobbyMenu instance;

    [SerializeField] GameObject playerGroup;
    [SerializeField] GameObject startButton;

    [SerializeField] GameObject playerUIPrefab;

    private void Start()
    {
        GameLog.Write(
            "LobbyMenu",
            $"Start serverActive={NetworkServer.active} clientActive={NetworkClient.active}");

        instance = this;

        if (!NetworkServer.active);
            startButton.GetComponentInChildren<TMP_Text>().text = "READY";
    }

    public void RefreshLobby()
    {
        GameLog.Write(
            "LobbyMenu",
            $"RefreshLobby existingEntries={(playerGroup != null ? playerGroup.transform.childCount.ToString() : "NULL")}");

        for (int i = playerGroup.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(playerGroup.transform.GetChild(i).gameObject);
        }

        LobbyPlayer[] players = FindObjectsByType<LobbyPlayer>();

        GameLog.Write(
            "LobbyMenu",
            $"RefreshLobby lobbyPlayers={players.Length}");

        foreach (LobbyPlayer player in players)
        {
            AddPlayerToUI(player);
        }
    }

    private void AddPlayerToUI(LobbyPlayer lobbyPlayer)
    {
        GameLog.Write(
            "LobbyMenu",
            $"AddPlayerToUI steamId={lobbyPlayer.SteamId} nickname={lobbyPlayer.Nickname} index={lobbyPlayer.index} ready={lobbyPlayer.readyToBegin} serverActive={NetworkServer.active}");

        var UI_player = Instantiate(playerUIPrefab, playerGroup.transform);

        UIPlayer UIplayer_Data = UI_player.GetComponent<UIPlayer>();
        lobbyPlayer.player_UIElement = UIplayer_Data;

        var avatarImage = UIplayer_Data.avatar.GetComponent<Image>();
        SteamAvatarExtractor.Instance.GetAvatar(
            new CSteamID(lobbyPlayer.SteamId),
            sprite =>
            {
                GameLog.Write(
                    "LobbyMenu",
                    $"AddPlayerToUI avatar callback steamId={lobbyPlayer.SteamId} sprite={(sprite != null ? sprite.name : "NULL")}");

                avatarImage.sprite = sprite;
            });

        UIplayer_Data.nickname.GetComponent<TMP_Text>().text = lobbyPlayer.Nickname;

        if (NetworkServer.active && lobbyPlayer.index > 0)
        {
            GameLog.Write(
                "LobbyMenu",
                $"AddPlayerToUI kick button enabled for nickname={lobbyPlayer.Nickname} index={lobbyPlayer.index}");

            UIplayer_Data.kickBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                GameLog.Write(
                    "LobbyMenu",
                    $"Kick button clicked nickname={lobbyPlayer.Nickname} index={lobbyPlayer.index} serverActive={NetworkServer.active}");

                if (NetworkServer.active)
                    lobbyPlayer.Kick();
            });
        }
        else
        {
            GameLog.Write(
                "LobbyMenu",
                $"AddPlayerToUI kick button hidden for nickname={lobbyPlayer.Nickname} index={lobbyPlayer.index}");

            UIplayer_Data.kickBtn.SetActive(false);
        }

        if (lobbyPlayer.index > 0)
        {
            GameLog.Write(
                "LobbyMenu",
                $"AddPlayerToUI host marker hidden for nickname={lobbyPlayer.Nickname}");

            UIplayer_Data.hostMarker.SetActive(false);
        }
    }

    public void StartSession()
    {
        GameLog.Write(
            "LobbyMenu",
            $"StartSession button pressed serverActive={NetworkServer.active}");

        NetworkClient.localPlayer.GetComponent<LobbyPlayer>().ToggleReady();

        if (NetworkServer.active)
        {
            NetworkClient.localPlayer.GetComponent<LobbyPlayer>().ToggleReady(true);
            if (((NetManager)NetworkManager.singleton).TryStartGame())
            {
                GameLog.Write(
                    "LobbyMenu",
                    "StartSession game start accepted, start button disabled");

                startButton.GetComponent<Button>().interactable = false;
            }
            else
            {
                GameLog.Write(
                    "LobbyMenu",
                    "StartSession game start refused, host ready reset to false");

                NetworkClient.localPlayer.GetComponent<LobbyPlayer>().ToggleReady(false);
            }
        }
    }
}
