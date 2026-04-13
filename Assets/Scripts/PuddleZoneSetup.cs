using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class PuddleZoneSetup : MonoBehaviour
{
    private const string SliceRootName = "__TowerInteriorSlice";
    private const float DefaultRoomWidth = 42f;
    private const float HorizontalInset = 2.8f;
    private const float MinWidth = 3f;
    private const float MaxWidth = 6f;
    private const float MinDepth = 2f;
    private const float MaxDepth = 4f;

    private static PuddleZoneSetup s_Instance;

    public static void Ensure()
    {
        if (s_Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("__PuddleZoneSetup");
        s_Instance = go.AddComponent<PuddleZoneSetup>();
        s_Instance.Build();
    }

    private void Build()
    {
        GameObject sliceRoot = GameObject.Find(SliceRootName);
        if (sliceRoot == null)
        {
            StartCoroutine(RetryNextFrame());
            return;
        }

        int created = 0;
        foreach (Transform child in sliceRoot.transform)
        {
            if (!child.name.StartsWith("Room_", StringComparison.Ordinal) || child.GetComponentInChildren<PuddleZone>() != null)
            {
                continue;
            }

            created += CreatePuddlesForRoom(child);
        }

        Debug.Log($"[PuddleZoneSetup] Created {created} puddle zones.");
    }

    private IEnumerator RetryNextFrame()
    {
        yield return null;
        Build();
    }

    private int CreatePuddlesForRoom(Transform roomRoot)
    {
        int seed = Animator.StringToHash(roomRoot.name);
        System.Random random = new System.Random(seed);
        int targetCount = 1 + random.Next(0, 2);
        int created = 0;

        Bounds roomBounds = GetRoomBounds(roomRoot);
        List<Bounds> obstacles = CollectObstacleBounds(roomRoot);

        for (int attempts = 0; attempts < 20 && created < targetCount; attempts++)
        {
            float width = Mathf.Lerp(MinWidth, MaxWidth, (float)random.NextDouble());
            float depth = Mathf.Lerp(MinDepth, MaxDepth, (float)random.NextDouble());
            float minX = roomBounds.min.x + HorizontalInset + width * 0.5f;
            float maxX = roomBounds.max.x - HorizontalInset - width * 0.5f;

            if (maxX <= minX)
            {
                break;
            }

            float centerX = Mathf.Lerp(minX, maxX, (float)random.NextDouble());
            float centerZ = Mathf.Lerp(0.9f, 1.55f, (float)random.NextDouble());
            Bounds candidate = new Bounds(
                new Vector3(centerX, 0.55f, centerZ),
                new Vector3(width + 0.45f, 1.25f, depth + 0.45f));

            if (!ContainsWalkLine(candidate))
            {
                continue;
            }

            if (IntersectsObstacle(candidate, obstacles))
            {
                continue;
            }

            CreatePuddle(roomRoot, created, candidate.center, width, depth);
            created++;
        }

        if (created == 0)
        {
            Vector3 fallbackCenter = new Vector3(roomRoot.position.x, 0.55f, 1.15f);
            CreatePuddle(roomRoot, created, fallbackCenter, 4.2f, 2.4f);
            created++;
        }

        return created;
    }

    private static bool ContainsWalkLine(Bounds candidate)
    {
        return candidate.min.z <= TowerInteriorSlice.WalkDepth && candidate.max.z >= TowerInteriorSlice.WalkDepth;
    }

    private void CreatePuddle(Transform roomRoot, int index, Vector3 center, float width, float depth)
    {
        GameObject puddleObject = new GameObject($"PuddleZoneAuto_{index + 1}");
        puddleObject.transform.SetParent(roomRoot, false);

        PuddleZone puddleZone = puddleObject.AddComponent<PuddleZone>();
        puddleZone.Configure(center, width, depth);
    }

    private static Bounds GetRoomBounds(Transform roomRoot)
    {
        Bounds bounds = new Bounds(roomRoot.position, new Vector3(DefaultRoomWidth, 0.1f, 8.4f));
        bool initialized = false;
        Renderer[] renderers = roomRoot.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            string lowerName = renderer.transform.name.ToLowerInvariant();
            if (!lowerName.Contains("floor"))
            {
                continue;
            }

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private static List<Bounds> CollectObstacleBounds(Transform roomRoot)
    {
        List<Bounds> obstacles = new List<Bounds>();
        Renderer[] renderers = roomRoot.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (!IsObstacleRenderer(renderer))
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            bounds.Expand(new Vector3(0.7f, 0.2f, 0.35f));
            obstacles.Add(bounds);
        }

        return obstacles;
    }

    private static bool IntersectsObstacle(Bounds candidate, List<Bounds> obstacles)
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (candidate.Intersects(obstacles[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsObstacleRenderer(Renderer renderer)
    {
        Bounds bounds = renderer.bounds;
        if (bounds.max.y < 0.35f || bounds.min.y > 1.8f)
        {
            return false;
        }

        string lowerName = renderer.transform.name.ToLowerInvariant();
        if (lowerName.Contains("floor") ||
            lowerName.Contains("backdrop") ||
            lowerName.Contains("skirting") ||
            lowerName.Contains("ceiling") ||
            lowerName.Contains("window") ||
            lowerName.Contains("beam") ||
            lowerName.Contains("lamp") ||
            lowerName.Contains("header") ||
            lowerName.Contains("door") ||
            lowerName.Contains("mask") ||
            lowerName.Contains("reveal"))
        {
            return false;
        }

        return true;
    }
}
