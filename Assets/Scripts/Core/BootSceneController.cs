using UnityEngine;

public class BootSceneController : MonoBehaviour
{
    [SerializeField] private float bootDelay = 0.15f;
    [SerializeField] private Color bootBackgroundColor = new Color(0.01f, 0.01f, 0.015f, 1f);

    private void Awake()
    {
        EnsureBootCamera();
    }

    private void Start()
    {
        SceneLoader.LoadTitle(bootDelay);
    }

    private void EnsureBootCamera()
    {
        Camera bootCamera = Camera.main;
        if (bootCamera == null)
        {
            bootCamera = FindFirstObjectByType<Camera>();
        }

        if (bootCamera == null)
        {
            GameObject cameraRoot = new GameObject("Main Camera");
            cameraRoot.tag = "MainCamera";
            bootCamera = cameraRoot.AddComponent<Camera>();
            cameraRoot.AddComponent<AudioListener>();
        }

        bootCamera.enabled = true;
        bootCamera.clearFlags = CameraClearFlags.SolidColor;
        bootCamera.backgroundColor = bootBackgroundColor;
        bootCamera.nearClipPlane = 0.1f;
        bootCamera.farClipPlane = 20f;
        bootCamera.transform.position = new Vector3(0f, 0f, -10f);
        bootCamera.transform.rotation = Quaternion.identity;
    }
}
