using Mirror;
using UnityEngine;

public class DealerToken : InteractableObject
{
    [SerializeField] private SkinnedMeshRenderer highlight;

    public override void Interact()
    {
        GameAudioManager.Instance?.PlayAccept();
        GameLog.Write(
            "DealerToken",
            $"Interact netId={netId}: requesting end turn");
        CmdEndTurn();
    }

    [Command(requiresAuthority = false)]
    private void CmdEndTurn(
        NetworkConnectionToClient sender = null)
    {
        GameLog.Write(
            "DealerToken",
            $"CmdEndTurn netId={netId} sender={(sender != null ? sender.connectionId : -1)}");

        if (sender == null ||
            GameMaster.Instance == null)
        {
            GameLog.Write(
                "DealerToken",
                $"CmdEndTurn rejected netId={netId}: sender is NULL or GameMaster is NULL");

            return;
        }

        Player tokenOwner =
            GetComponentInParent<Player>();

        if (tokenOwner == null ||
            tokenOwner.netIdentity == null ||
            tokenOwner.netIdentity.connectionToClient != sender)
        {
            GameLog.Write(
                "DealerToken",
                $"CmdEndTurn rejected netId={netId}: sender={sender.connectionId} is not the token owner (tokenOwner={(tokenOwner != null ? tokenOwner.name : "NULL")})");

            return;
        }

        if (GameMaster.Instance.CurrentPlayerIndex !=
            tokenOwner.SeatIndex)
        {
            GameLog.Write(
                "DealerToken",
                $"CmdEndTurn rejected netId={netId}: not owner turn (currentPlayer={GameMaster.Instance.CurrentPlayerIndex} seat={tokenOwner.SeatIndex})");

            return;
        }

        GameLog.Write(
            "DealerToken",
            $"CmdEndTurn netId={netId}: ending turn of player={(tokenOwner != null ? tokenOwner.name : "NULL")} seat={tokenOwner.SeatIndex}");

        GameMaster.Instance.EndTurn();
    }

    public override void OnLookEnter()
    {
        GameLog.Write(
            "DealerToken",
            $"OnLookEnter netId={netId}: highlight on (highlight={(highlight != null ? "ok" : "NULL")})");

        if (highlight != null)
            highlight.enabled = true;
        GameAudioManager.Instance?.PlayHover();
    }

    public override void OnLookExit()
    {
        GameLog.Write(
            "DealerToken",
            $"OnLookExit netId={netId}: highlight off (highlight={(highlight != null ? "ok" : "NULL")})");

        if (highlight != null)
            highlight.enabled = false;
    }
}
