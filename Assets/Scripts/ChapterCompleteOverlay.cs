using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ChapterCompleteOverlay : MonoBehaviour
{
    public static bool IsVisible { get; private set; }

    private void Update()
    {
        IsVisible = ShouldShow();
        if (!IsVisible)
        {
            HideCanvasView();
            return;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ReturnToMain();
                return;
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                ReloadMain();
            }
        }

        SyncCanvasView();
    }

    private bool ShouldShow()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == SceneLoader.MainSceneName && ChapterState.GetFlag("chapter01_complete");
    }

    private void ReturnToMain()
    {
        ChapterState.ResetRuntime();
        DialogueEventSystem.ClearFlags();
        SceneLoader.LoadMain();
    }

    private void ReloadMain()
    {
        ChapterState.ResetRuntime();
        DialogueEventSystem.ClearFlags();
        SceneLoader.ReloadCurrent();
    }

    private void OnDisable()
    {
        HideCanvasView();
    }

    private void SyncCanvasView()
    {
        if (!UiBootstrap.TryGetModalView(out ModalCanvasView modalView))
        {
            return;
        }

        modalView.ShowChapterComplete(
            "当前段落完成",
            "这个项目现在只保留 Main 单场景。按 Enter 返回主场景，或按 R 重新载入当前房屋切片。",
            "Enter / Esc 回到主场景    R 重新载入",
            "回到主场景",
            "重新载入",
            ReturnToMain,
            ReloadMain);
    }

    private void HideCanvasView()
    {
        if (UiBootstrap.TryGetModalView(out ModalCanvasView modalView))
        {
            modalView.HideChapterComplete();
        }
    }
}
