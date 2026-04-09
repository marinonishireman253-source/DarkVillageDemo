using UnityEngine;

public static class RuntimeModelSpawner
{
    public static GameObject Spawn(
        string resourcePath,
        string instanceName,
        Transform parent,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        bool keepColliders = false)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[RuntimeModelSpawner] Missing resource: {resourcePath}");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = string.IsNullOrWhiteSpace(instanceName) ? prefab.name : instanceName.Trim();
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        instance.transform.localScale = scale;

        RuntimeImportedMaterialLibrary.Apply(resourcePath, instance);

        if (!keepColliders)
        {
            foreach (Collider colliderComponent in instance.GetComponentsInChildren<Collider>(true))
            {
                colliderComponent.enabled = false;
            }
        }

        return instance;
    }
}
