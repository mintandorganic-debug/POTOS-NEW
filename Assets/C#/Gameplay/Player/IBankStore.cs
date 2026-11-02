using System.Collections.Generic;

public interface IBankStore
{
    uint NetId { get; }

    int Coins { get; set; }

    int MaxCoins { get; }

    IList<PlayerBank.ChipAmount> Chips { get; }

    void StartCoinsRain(
        int amount,
        float duration,
        bool addCoins = true);
}
