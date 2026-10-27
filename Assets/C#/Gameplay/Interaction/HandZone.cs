using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class HandZone : MonoBehaviour
{
    private BoxCollider box;

    public Player Owner { get; private set; }

    public void Initialize(
        Player owner,
        List<GameObject> sockets,
        Vector3 padding)
    {
        Owner = owner;

        if (box == null)
            box = GetComponent<BoxCollider>();

        box.isTrigger = true;

        if (sockets == null ||
            sockets.Count == 0)
        {
            return;
        }

        Bounds bounds =
            new Bounds(
                transform.InverseTransformPoint(
                    sockets[0].transform.position),
                Vector3.zero);

        foreach (GameObject socket in sockets)
        {
            if (socket == null)
                continue;

            bounds.Encapsulate(
                transform.InverseTransformPoint(
                    socket.transform.position));
        }

        bounds.Expand(padding * 2f);

        box.center = bounds.center;
        box.size = bounds.size;
    }
}
