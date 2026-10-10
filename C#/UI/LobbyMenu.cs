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
        instance = this;

        if (!NetworkServer.active);
            startButton.GetComponentInChildren<TMP_Text>().text = "READY";
    }

    public void RefreshLobby()
    {
        for (int i = playerGroup.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(playerGroup.transform.GetChild(i).gameObject);
        }

        LobbyPlayer[] players = FindObjectsByType<LobbyPlayer>();

        foreach (LobbyPlayer player in players)
        {
            AddPlayerToUI(player);
        }
    }

    private void AddPlayerToUI(LobbyPlayer lobbyPlayer)
    {
        var UI_player = Instantiate(playerUIPrefab, playerGroup.transform);

        UIPlayer UIplayer_Data = UI_player.GetComponent<UIPlayer>();
        lobbyPlayer.player_UIElement = UIplayer_Data;

        var avatarImage = UIplayer_Data.avatar.GetComponent<Image>();
        SteamAvatarExtractor.Instance.GetAvatar(
            new CSteamID(lobbyPlayer.SteamId),
            sprite =>
            {
                avatarImage.sprite = sprite;
            });

        UIplayer_Data.nickname.GetComponent<TMP_Text>().text = lobbyPlayer.Nickname;

        if (NetworkServer.active && lobbyPlayer.index > 0)
        {
            UIplayer_Data.kickBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (NetworkServer.active)
                    lobbyPlayer.Kick();
            });
        }
        else
        {
            UIplayer_Data.kickBtn.SetActive(false);
        }

        if (lobbyPlayer.index > 0)
            UIplayer_Data.hostMarker.SetActive(false);
    }

    public void StartSession()
    {
        NetworkClient.localPlayer.GetComponent<LobbyPlayer>().ToggleReady();

        if (NetworkServer.active)
        {
            NetworkClient.localPlayer.GetComponent<LobbyPlayer>().ToggleReady(true);
            if (((NetManager)NetworkManager.singleton).TryStartGame())
            {
                startButton.GetComponent<Button>().interactable = false;
            }
            else
                NetworkClient.localPlayer.GetComponent<LobbyPlayer>().ToggleReady(false);
        }
    }
}
