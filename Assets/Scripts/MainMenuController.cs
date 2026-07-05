using UnityEngine;

/// <summary>
/// Bootstraps the main menu scene and routes to sign-in or hub based on session state.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    void Awake()
    {
        SceneFlow.InitializeMainMenuScene();
        CreateCamera();
        MenuUiFactory.EnsureEventSystem();
        MenuNavigator.Create();
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
