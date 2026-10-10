using UnityEngine;

public class SplineRotator : MonoBehaviour
{
    public void LookAtPlayer(Transform player)
    {
        transform.rotation = Quaternion.Euler(
            0f,
            player.eulerAngles.y - 90f,
            0f);
    }
}