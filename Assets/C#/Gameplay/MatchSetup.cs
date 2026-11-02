using System.Collections.Generic;
using UnityEngine;

public class MatchSetup
{
    private readonly SpawnpointCalculator spawnpointCalculator;

    private List<int> accessoryPool;

    public MatchSetup(
        SpawnpointCalculator spawnpointCalculator)
    {
        this.spawnpointCalculator = spawnpointCalculator;

        GameLog.Write(
            "MatchSetup",
            $"Created spawnpointCalculator={(spawnpointCalculator != null ? spawnpointCalculator.name : "NULL")}");
    }

    public string Prepare(
        int playerCount)
    {
        GameLog.Write(
            "MatchSetup",
            $"Prepare playerCount={playerCount}");

        if (spawnpointCalculator == null)
        {
            GameLog.Write(
                "MatchSetup",
                "Prepare failed: spawnpointCalculator is null");

            return "SpawnpointCalculator missing.";
        }

        spawnpointCalculator.CalculateSpawnPoints(
            playerCount);

        GameLog.Write(
            "MatchSetup",
            $"Prepare spawn points calculated spawnPoints={spawnpointCalculator.SpawnPoints.Count} players={playerCount}");

        if (spawnpointCalculator.SpawnPoints.Count <
            playerCount)
        {
            GameLog.Write(
                "MatchSetup",
                "Prepare failed: not enough spawn points");

            return "Not enough spawn points.";
        }

        accessoryPool =
            new List<int>
            {
                0,
                1,
                2,
                3,
                4
            };

        if (accessoryPool.Count <
            playerCount)
        {
            GameLog.Write(
                "MatchSetup",
                $"Prepare failed: not enough accessories pool={accessoryPool.Count} players={playerCount}");

            return "Not enough accessories for all players.";
        }

        GameLog.Write(
            "MatchSetup",
            $"Prepare ok spawnPoints={spawnpointCalculator.SpawnPoints.Count} accessoryPool={accessoryPool.Count} players={playerCount}");

        return null;
    }

    public void SeatPlayers(
        IReadOnlyList<Player> players)
    {
        GameLog.Write(
            "MatchSetup",
            $"SeatPlayers players={players.Count} accessoryPool={accessoryPool.Count}");

        for (int i = 0;
             i < players.Count;
             i++)
        {
            Player player =
                players[i];

            Pose pose =
                spawnpointCalculator.SpawnPoints[i];

            GameLog.Write(
                "MatchSetup",
                $"SeatPlayers setting up seat={i} player={(player != null ? player.netId.ToString() : "NULL")} position={pose.position}");

            player.SetSeat(i);

            player.SummonCards();

            int accessoryIndex =
                Random.Range(
                    0,
                    accessoryPool.Count);

            GameLog.Write(
                "MatchSetup",
                $"SeatPlayers seat={i} accessoryIndex={accessoryIndex} poolCount={accessoryPool.Count}");

            for (int j = 0;
                 j < 5;
                 j++)
            {
                player.SetAccesory(
                    j,
                    false);
            }

            player.SetAccesory(
                accessoryIndex,
                true);

            accessoryPool.Remove(
                accessoryIndex);

            player.transform.SetPositionAndRotation(
                pose.position,
                pose.rotation);
        }

        GameLog.Write(
            "MatchSetup",
            $"SeatPlayers done players={players.Count} accessoryPoolLeft={accessoryPool.Count}");
    }
}
