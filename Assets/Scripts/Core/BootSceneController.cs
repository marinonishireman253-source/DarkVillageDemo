using UnityEngine;

public class BootSceneController : MonoBehaviour
{
    [SerializeField] private float bootDelay = 0.15f;

    private void Start()
    {
        SceneLoader.LoadTitle(bootDelay);
    }
}
