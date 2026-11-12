#if UNITY_EDITOR
using UnityEngine;

public class PlateSpawner
{
    private const float PlateLift = 0.07f;

    private readonly LiftAnimation lift;

    private GameObject plate;

    public PlateSpawner(
        LiftAnimation lift)
    {
        this.lift = lift;
    }

    public void Spawn()
    {
        Remove();

        GameObject plateAsset =
            Resources.Load<GameObject>(
                "Dice/Plate");

        Transform platform =
            lift.PlatformPart;

        if (plateAsset == null ||
            platform == null)
        {
            GameLog.Write(
                "PlateSpawner",
                $"Spawn skipped: prefab={(plateAsset != null)} platform={(platform != null)}");

            return;
        }

        TryGetPlatformTop(
            platform,
            out Vector3 top);

        plate =
            Object.Instantiate(
                plateAsset,
                top,
                plateAsset.transform.rotation);

        Renderer[] renderers =
            plate.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length > 0)
        {
            Bounds visual =
                renderers[0].bounds;

            foreach (Renderer renderer in renderers)
            {
                visual.Encapsulate(
                    renderer.bounds);
            }

            plate.transform.position +=
                new Vector3(
                    top.x - visual.center.x,
                    top.y - visual.min.y + PlateLift,
                    top.z - visual.center.z);
        }

        Transform lid =
            platform.Find("Cylinder.018");

        plate.transform.SetParent(
            lid,
            true);

        AddDiceCollider();

        GameLog.Write(
            "PlateSpawner",
            $"Spawned inside Cylinder.018 (child of the lid), top {top.ToString("F3")}");
    }

    public void Remove()
    {
        if (plate != null)
        {
            Object.Destroy(
                plate);

            plate = null;
        }
    }

    private void AddDiceCollider()
    {
        int diceLayer =
            DiceCollisionLayers.LayerId;

        foreach (Collider original in
                 plate.GetComponentsInChildren<Collider>(true))
        {
            Object.Destroy(
                original);
        }

        MeshFilter filter =
            plate.GetComponentInChildren<MeshFilter>(true);

        if (filter == null)
        {
            return;
        }

        GameObject colliderObject =
            new GameObject("PlateCollider");

        colliderObject.layer =
            diceLayer >= 0
                ? diceLayer
                : 0;

        colliderObject.transform.SetParent(
            filter.transform,
            false);

        MeshCollider collider =
            colliderObject.AddComponent<MeshCollider>();

        collider.sharedMesh =
            filter.sharedMesh;
    }

    private static bool TryGetPlatformTop(
        Transform platform,
        out Vector3 top)
    {
        Transform part =
            platform.Find("Cylinder.018");

        MeshFilter filter =
            part != null
                ? part.GetComponent<MeshFilter>()
                : null;

        if (filter == null ||
            filter.sharedMesh == null)
        {
            top = platform.position;

            return false;
        }

        Bounds local =
            filter.sharedMesh.bounds;

        Bounds world =
            new Bounds(
                part.TransformPoint(
                    local.center),
                Vector3.zero);

        for (int i = 0;
             i < 8;
             i++)
        {
            Vector3 corner =
                local.center +
                Vector3.Scale(
                    local.extents,
                    new Vector3(
                        (i & 1) == 0 ? -1f : 1f,
                        (i & 2) == 0 ? -1f : 1f,
                        (i & 4) == 0 ? -1f : 1f));

            world.Encapsulate(
                part.TransformPoint(
                    corner));
        }

        top =
            new Vector3(
                world.center.x,
                world.max.y,
                world.center.z);

        return true;
    }
}
#endif
