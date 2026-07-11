using UnityEngine;

/// <summary>
/// Bootstraps the main menu scene and routes to sign-in or hub based on session state.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    void Awake()
    {
        BootTrace.Log("BOOT", "MainMenuController.Awake begin");
        SceneFlow.InitializeMainMenuScene();
        CreateCamera();
        MenuUiFactory.EnsureEventSystem();
        BootTrace.Log("UI", "MainMenuController creating MenuNavigator");
        MenuNavigator.Create();
        BootTrace.Log("BOOT", "MainMenuController.Awake complete");
    }

    void CreateCamera()
    {
        var camGo = new GameObject("Menu Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = MenuUiFactory.Background;
        cam.cullingMask = 0;
        camGo.AddComponent<AudioListener>();
    }

}
