using UnityEngine;

public class CoinWallet
{
    private readonly IBankStore store;

    public CoinWallet(
        IBankStore store)
    {
        this.store = store;

        GameLog.Write(
            "CoinWallet",
            "Created");
    }

    private static bool IsMissing(
        IBankStore bank)
    {
        return bank == null ||
               (bank is Object unityObject &&
                unityObject == null);
    }

    public bool AddCoins(int amount)
    {
        GameLog.Write(
            "CoinWallet",
            $"AddCoins netId={store.NetId} amount={amount} coins={store.Coins}");

        if (amount <= 0)
        {
            GameLog.Write(
                "CoinWallet",
                $"AddCoins rejected: amount <= 0 netId={store.NetId} amount={amount}");
            return false;
        }

        int newCoins =
            Mathf.Min(
                store.Coins + amount,
                store.MaxCoins);

        if (newCoins == store.Coins)
        {
            GameLog.Write(
                "CoinWallet",
                $"AddCoins rejected: no change (max reached) netId={store.NetId} coins={store.Coins} max={store.MaxCoins} amount={amount}");
            return false;
        }

        GameLog.Write(
            "CoinWallet",
            $"AddCoins applied netId={store.NetId} old={store.Coins} new={newCoins} added={newCoins - store.Coins}");

        store.Coins =
            newCoins;

        return true;
    }

    public bool RemoveCoins(int amount)
    {
        GameLog.Write(
            "CoinWallet",
            $"RemoveCoins netId={store.NetId} amount={amount} coins={store.Coins}");

        if (amount <= 0)
        {
            GameLog.Write(
                "CoinWallet",
                $"RemoveCoins rejected: amount <= 0 netId={store.NetId} amount={amount}");
            return false;
        }

        if (store.Coins < amount)
        {
            GameLog.Write(
                "CoinWallet",
                $"RemoveCoins rejected: not enough coins netId={store.NetId} coins={store.Coins} amount={amount}");
            return false;
        }

        GameLog.Write(
            "CoinWallet",
            $"RemoveCoins applied netId={store.NetId} old={store.Coins} new={store.Coins - amount}");

        store.Coins -= amount;

        return true;
    }

    public bool TransferCoinsTo(
        IBankStore recipient,
        int amount)
    {
        GameLog.Write(
            "CoinWallet",
            $"TransferCoinsTo netId={store.NetId} recipient={(recipient != null ? recipient.NetId.ToString() : "NULL")} amount={amount} coins={store.Coins} recipientCoins={(recipient != null ? recipient.Coins.ToString() : "NULL")}");

        if (IsMissing(recipient) ||
            recipient == store ||
            amount <= 0 ||
            store.Coins < amount ||
            recipient.Coins > store.MaxCoins - amount)
        {
            GameLog.Write(
                "CoinWallet",
                $"TransferCoinsTo rejected netId={store.NetId} recipientNull={recipient == null} self={recipient == store} amount={amount} coins={store.Coins} recipientCoins={(recipient != null ? recipient.Coins.ToString() : "NULL")} max={store.MaxCoins}");
            return false;
        }

        GameLog.Write(
            "CoinWallet",
            $"TransferCoinsTo applied netId={store.NetId} recipient={recipient.NetId} amount={amount} senderOld={store.Coins} senderNew={store.Coins - amount} recipientOld={recipient.Coins} recipientNew={recipient.Coins + amount}");

        store.Coins -= amount;
        recipient.Coins += amount;

        return true;
    }

    public bool StealCoinsTo(
        IBankStore recipient,
        int amount,
        float duration)
    {
        GameLog.Write(
            "CoinWallet",
            $"StealCoinsTo netId={store.NetId} recipient={(recipient != null ? recipient.NetId.ToString() : "NULL")} amount={amount} duration={duration} coins={store.Coins} recipientCoins={(recipient != null ? recipient.Coins.ToString() : "NULL")}");

        if (IsMissing(recipient) ||
            recipient == store ||
            amount <= 0 ||
            store.Coins < amount ||
            recipient.Coins > store.MaxCoins - amount)
        {
            GameLog.Write(
                "CoinWallet",
                $"StealCoinsTo rejected netId={store.NetId} recipientNull={recipient == null} self={recipient == store} amount={amount} coins={store.Coins} recipientCoins={(recipient != null ? recipient.Coins.ToString() : "NULL")} max={store.MaxCoins}");
            return false;
        }

        GameLog.Write(
            "CoinWallet",
            $"StealCoinsTo applied netId={store.NetId} recipient={recipient.NetId} amount={amount} victimOld={store.Coins} victimNew={store.Coins - amount} duration={duration}");

        store.Coins -= amount;
        recipient.StartCoinsRain(
            amount,
            duration);

        return true;
    }

    public bool SpendCoins(int amount)
    {
        GameLog.Write(
            "CoinWallet",
            $"SpendCoins netId={store.NetId} amount={amount} coins={store.Coins}");
        return RemoveCoins(amount);
    }

    public bool HasCoins(int amount)
    {
        GameLog.Write(
            "CoinWallet",
            $"HasCoins netId={store.NetId} amount={amount} coins={store.Coins}");
        return amount >= 0 &&
               store.Coins >= amount;
    }
}
