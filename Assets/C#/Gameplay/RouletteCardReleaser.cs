using Mirror;

public static class RouletteCardReleaser
{
    public static void Release(
        Card card)
    {
        GameLog.Write(
            "RouletteCardReleaser",
            $"Release card={(card != null && card.Data != null ? card.Data.name : "NULL")}");

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (card != null)
        {
            card.SetInRoulette(false);
        }

        if (roulette != null)
        {
            roulette.StopRoulette();

            if (card != null)
            {
                roulette.DetachCard(
                    card.transform);
            }
        }
    }

    public static void Destroy(
        Card card)
    {
        GameLog.Write(
            "RouletteCardReleaser",
            $"DestroyCard card={(card != null && card.Data != null ? card.Data.name : "NULL")}");

        if (card == null)
        {
            GameLog.Write(
                "RouletteCardReleaser",
                "DestroyCard skipped: card is null");

            return;
        }

        card.SetInRoulette(false);

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette != null)
        {
            roulette.DetachCard(
                card.transform);
        }

        if (card.gameObject != null)
        {
            GameLog.Write(
                "RouletteCardReleaser",
                $"DestroyCard NetworkServer.Destroy card={(card.Data != null ? card.Data.name : "NULL")}");

            NetworkServer.Destroy(
                card.gameObject);
        }
    }
}
