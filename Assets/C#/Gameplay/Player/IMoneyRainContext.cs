public interface IMoneyRainContext
{
    uint NetId { get; }

    int Coins { get; }

    int MaxCoins { get; }

    bool AddCoins(
        int amount);

    bool AddChips(
        int denomination,
        int amount);

    int GetChipIndex(
        int denomination);

    void PlayCoinsRain(
        int count);

    void PlayChipsRain(
        int count);
}
