using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Bootstraps the main menu scene and routes to sign-in or hub based on session state.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    void Awake()
    {
        CreateCamera();
        CreateEventSystem();
        MenuNavigator.Create();
    }

    void CreateCamera()
    {
        var camGo = new GameObject("Menu Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = MenuUiFactory.Background;
        cam.cullingMask = 0;
        camGo.AddComponent<AudioListener>();
    }

    void CreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
