using UnityEngine;

/// <summary>
/// Spawns a wet-floor reflection quad above each room's floor after TowerInteriorSlice generates rooms.
/// Attach to the bootstrap root or call Setup() manually after rooms exist.
/// </summary>
public sealed class WetFloorSetup : MonoBehaviour
{
    private const string ShaderName = "Custom/WetFloor";
    private const float QuadY = 0.26f; // slightly above floor surface (floor Y ~0.24 top)
    private const float QuadThickness = 0.005f;
    private const float ReflectionStrength = 0.22f;
    private const float RippleDistortion = 0.02f;

    private static WetFloorSetup s_Instance;
    private static Material s_WetMaterial;

    public static void Ensure()
    {
        if (s_Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("__WetFloorSetup");
        s_Instance = go.AddComponent<WetFloorSetup>();
        s_Instance.Build();
    }

    private void Build()
    {
        Material mat = GetOrCreateMaterial();
        if (mat == null)
        {
            Debug.LogWarning("[WetFloorSetup] WetFloor shader not found — skipping wet floor.");
            return;
        }

        // Find all root objects named "__TowerInteriorSlice" (the main room root)
        GameObject sliceRoot = GameObject.Find("__TowerInteriorSlice");
        if (sliceRoot == null)
        {
            Debug.LogWarning("[WetFloorSetup] __TowerInteriorSlice not found — retrying next frame.");
            StartCoroutine(RetryNextFrame());
            return;
        }

        // Each room is named "Room_N_..."
        // We look for the floor cube in each room's Structure group
        Transform[] allChildren = sliceRoot.GetComponentsInChildren<Transform>();
        int quadsCreated = 0;

        foreach (Transform child in allChildren)
        {
            if (!child.name.StartsWith("Room_"))
            {
                continue;
            }

            // Skip non-root Room_ objects
            if (child.parent != sliceRoot.transform)
            {
                continue;
            }

            // Find the floor block inside Structure
            Transform structure = child.Find("Structure");
            if (structure == null)
            {
                continue;
            }

            // Look for floor-related blocks
            // The room floor is a large cube with localScale = (width, 0.24, depth)
            // We find the biggest horizontal block near Y=0 in Structure
            foreach (Transform block in structure)
            {
                if (block.localScale.y > 0.1f && block.localScale.y < 0.5f && block.localPosition.y < 0.3f)
                {
                    CreateWetQuad(child, block, mat);
                    quadsCreated++;
                    break; // one quad per room
                }
            }
        }

        // Also create a wet quad for the main "InteriorFloor" shell block
        Transform interiorFloor = FindDeep(sliceRoot.transform, "InteriorFloor");
        if (interiorFloor != null)
        {
            CreateWetQuad(interiorFloor.parent, interiorFloor, mat);
            quadsCreated++;
        }

        // Create wet quads for FloorBand_Front, FloorBand_Mid across all rooms
        foreach (Transform child in allChildren)
        {
            if (child.name.Contains("FloorBand_Front") || child.name.Contains("FloorBand_Mid") || child.name.Contains("FloorRunner"))
            {
                CreateWetQuad(child.parent, child, mat);
                quadsCreated++;
            }
        }

        Debug.Log($"[WetFloorSetup] Created {quadsCreated} wet-floor quads.");
    }

    private System.Collections.IEnumerator RetryNextFrame()
    {
        yield return null;
        Build();
    }

    private void CreateWetQuad(Transform parent, Transform referenceBlock, Material material)
    {
        // Get the world-space bounds of the reference block
        Vector3 blockPos = referenceBlock.position;
        Vector3 blockScale = referenceBlock.lossyScale;

        // Create a thin Quad above the floor
        GameObject wetQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wetQuad.name = $"WetFloor_{referenceBlock.name}";
        wetQuad.transform.SetParent(parent, false);

        // Position: slightly above the block in world space, then convert to local
        Vector3 worldPos = blockPos + Vector3.up * QuadY;
        wetQuad.transform.position = worldPos;

        // Scale to cover the floor area (X and Z of the block)
        wetQuad.transform.localScale = new Vector3(blockScale.x, blockScale.z, 1f);

        // Rotate to lie flat (face up)
        wetQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Remove collider immediately to avoid blocking player
        Collider col = wetQuad.GetComponent<Collider>();
        if (col != null)
        {
            DestroyImmediate(col);
        }

        // Apply wet floor material
        Renderer renderer = wetQuad.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Try to set render queue slightly above transparent
            renderer.material.renderQueue = 3001;
        }
    }

    private static Transform FindDeep(Transform root, string targetName)
    {
        if (root.name == targetName)
        {
            return root;
        }
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private static Material GetOrCreateMaterial()
    {
        if (s_WetMaterial != null)
        {
            return s_WetMaterial;
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError("[WetFloorSetup] Shader 'Custom/WetFloor' not found!");
            return null;
        }

        s_WetMaterial = new Material(shader);
        s_WetMaterial.SetFloat("_ReflectionStrength", ReflectionStrength);
        s_WetMaterial.SetFloat("_RippleStrength", RippleDistortion);
        s_WetMaterial.SetFloat("_RippleScale", 8f);
        s_WetMaterial.SetFloat("_RippleSpeed", 0.4f);
        s_WetMaterial.SetFloat("_FresnelPower", 3f);
        s_WetMaterial.SetFloat("_SpecularStrength", 0.6f);
        s_WetMaterial.SetColor("_WaterColor", new Color(0.15f, 0.18f, 0.22f, 0.35f));

        return s_WetMaterial;
    }
}
