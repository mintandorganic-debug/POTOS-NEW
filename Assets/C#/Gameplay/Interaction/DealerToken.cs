using Mirror;
using UnityEngine;

public class DealerToken : InteractableObject
{
    [SerializeField] private SkinnedMeshRenderer highlight;

    public override void Interact()
    {
        GameAudioManager.Instance?.PlayAccept();
        CmdEndTurn();
    }

    [Command(requiresAuthority = false)]
    private void CmdEndTurn(
        NetworkConnectionToClient sender = null)
    {
        if (sender == null ||
            GameMaster.Instance == null)
        {
            return;
        }

        Player tokenOwner =
            GetComponentInParent<Player>();

        if (tokenOwner == null ||
            tokenOwner.netIdentity == null ||
            tokenOwner.netIdentity.connectionToClient != sender)
        {
            return;
        }

        if (GameMaster.Instance.CurrentPlayerIndex !=
            tokenOwner.SeatIndex)
        {
            return;
        }

        GameMaster.Instance.EndTurn();
    }

    public override void OnLookEnter()
    {
        if (highlight != null)
            highlight.enabled = true;
        GameAudioManager.Instance?.PlayHover();
    }

    public override void OnLookExit()
    {
        if (highlight != null)
            highlight.enabled = false;
    }
}
