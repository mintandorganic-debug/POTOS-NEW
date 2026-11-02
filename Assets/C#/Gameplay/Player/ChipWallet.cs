using UnityEngine;

public class ChipWallet
{
    private readonly IBankStore store;

    public ChipWallet(
        IBankStore store)
    {
        this.store = store;

        GameLog.Write(
            "ChipWallet",
            "Created");
    }

    public bool AddChips(
        int denomination,
        int amount)
    {
        GameLog.Write(
            "ChipWallet",
            $"AddChips netId={store.NetId} denomination={denomination} amount={amount}");

        if (amount <= 0)
        {
            GameLog.Write(
                "ChipWallet",
                $"AddChips rejected: amount <= 0 netId={store.NetId} denomination={denomination} amount={amount}");
            return false;
        }

        int index =
            GetChipIndex(denomination);

        if (index == -1)
        {
            GameLog.Write(
                "ChipWallet",
                $"AddChips rejected: unknown denomination netId={store.NetId} denomination={denomination}");

            Debug.LogWarning(
                $"Unknown chip denomination: {denomination}");

            return false;
        }

        PlayerBank.ChipAmount chip =
            store.Chips[index];

        chip.amount += amount;

        GameLog.Write(
            "ChipWallet",
            $"AddChips applied netId={store.NetId} denomination={denomination} old={chip.amount - amount} new={chip.amount}");

        store.Chips[index] =
            chip;

        return true;
    }

    public bool RemoveChips(
        int denomination,
        int amount)
    {
        GameLog.Write(
            "ChipWallet",
            $"RemoveChips netId={store.NetId} denomination={denomination} amount={amount}");

        if (amount <= 0)
        {
            GameLog.Write(
                "ChipWallet",
                $"RemoveChips rejected: amount <= 0 netId={store.NetId} denomination={denomination} amount={amount}");
            return false;
        }

        int index =
            GetChipIndex(denomination);

        if (index == -1)
        {
            GameLog.Write(
                "ChipWallet",
                $"RemoveChips rejected: unknown denomination netId={store.NetId} denomination={denomination}");
            return false;
        }

        PlayerBank.ChipAmount chip =
            store.Chips[index];

        if (chip.amount < amount)
        {
            GameLog.Write(
                "ChipWallet",
                $"RemoveChips rejected: not enough chips netId={store.NetId} denomination={denomination} have={chip.amount} amount={amount}");
            return false;
        }

        chip.amount -= amount;

        GameLog.Write(
            "ChipWallet",
            $"RemoveChips applied netId={store.NetId} denomination={denomination} old={chip.amount + amount} new={chip.amount}");

        store.Chips[index] =
            chip;

        return true;
    }

    public bool SpendChips(
        int denomination,
        int amount)
    {
        GameLog.Write(
            "ChipWallet",
            $"SpendChips netId={store.NetId} denomination={denomination} amount={amount}");
        return RemoveChips(
            denomination,
            amount);
    }

    public int GetChips(int denomination)
    {
        GameLog.Write(
            "ChipWallet",
            $"GetChips netId={store.NetId} denomination={denomination}");

        int index =
            GetChipIndex(denomination);

        if (index == -1)
        {
            GameLog.Write(
                "ChipWallet",
                $"GetChips unknown denomination netId={store.NetId} denomination={denomination}");
            return 0;
        }

        return store.Chips[index].amount;
    }

    public bool HasChips(
        int denomination,
        int amount)
    {
        GameLog.Write(
            "ChipWallet",
            $"HasChips netId={store.NetId} denomination={denomination} amount={amount}");

        if (amount < 0)
        {
            GameLog.Write(
                "ChipWallet",
                $"HasChips rejected: amount < 0 netId={store.NetId} denomination={denomination} amount={amount}");
            return false;
        }

        return GetChips(denomination) >= amount;
    }

    public int GetChipIndex(
        int denomination)
    {
        GameLog.Write(
            "ChipWallet",
            $"GetChipIndex netId={store.NetId} denomination={denomination} entries={store.Chips.Count}");

        for (int i = 0;
             i < store.Chips.Count;
             i++)
        {
            if (store.Chips[i].denomination ==
                denomination)
            {
                return i;
            }
        }

        return -1;
    }

    public int GetTotalChipValue()
    {
        GameLog.Write(
            "ChipWallet",
            $"GetTotalChipValue netId={store.NetId} entries={store.Chips.Count}");

        int total = 0;

        foreach (PlayerBank.ChipAmount chip in store.Chips)
        {
            total +=
                chip.denomination *
                chip.amount;
        }

        return total;
    }
}
