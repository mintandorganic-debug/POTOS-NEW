using UnityEngine;

public class SplineRotator : MonoBehaviour
{
    public void LookAtPlayer(Transform player)
    {
        GameLog.Write(
            "SplineRotator",
            $"LookAtPlayer player={(player != null ? player.name : "NULL")} playerYaw={(player != null ? player.eulerAngles.y : 0f)} self={name}");

        transform.rotation = Quaternion.Euler(
            0f,
            player.eulerAngles.y - 90f,
            0f);
    }
}