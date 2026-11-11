public class PlayerBankPayout : IDicePayout
{
    private static readonly int[] Denominations =
    {
        100,
        50,
        10,
        1
    };

    public void Pay(
        Player player,
        int chips,
        int coins)
    {
        if (player == null)
        {
            GameLog.Write(
                "PlayerBankPayout",
                "Pay skipped: player is NULL");

            return;
        }

        PlayerBank bank =
            player.GetComponentInChildren<PlayerBank>(true);

        if (bank == null)
        {
            GameLog.Write(
                "PlayerBankPayout",
                $"Pay skipped: PlayerBank is missing player={player.netId}");

            return;
        }

        int rest = chips;

        foreach (int denomination in Denominations)
        {
            int count =
                rest / denomination;

            if (count > 0)
            {
                bank.AddChips(
                    denomination,
                    count);

                rest -=
                    count * denomination;
            }
        }

        if (coins > 0)
        {
            bank.AddCoins(
                coins);
        }

        GameLog.Write(
            "PlayerBankPayout",
            $"Pay done player={player.netId} chips={chips} coins={coins}");
    }
}
