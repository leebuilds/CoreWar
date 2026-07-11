using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

/// <summary>
/// Temporary Editor-vs-Player divergence tracer.
/// Grep Player.log / Editor Console for tags: [BOOT] [SERVICES] [SCENES] [NETWORK] [PLAYER] [MAP] [VOXELS] [UI]
/// </summary>
public static class BootTrace
{
    static readonly Stopwatch Clock = Stopwatch.StartNew();
    static bool _hooked;
    static int _step;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void OnSubsystemRegistration()
    {
        Clock.Restart();
        _step = 0;
        _hooked = false;
        Log("BOOT", "SubsystemRegistration (domain/subsystem init)");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void OnBeforeSplashScreen()
    {
        Log("BOOT", "BeforeSplashScreen");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoad()
    {
        EnsureHooks();
        LogEnvironment("BeforeSceneLoad");
        ProbeCriticalShaders("BeforeSceneLoad");
        ProbeCriticalResources("BeforeSceneLoad");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad()
    {
        var scene = SceneManager.GetActiveScene();
        Log(
            "SCENES",
            $"AfterSceneLoad active='{scene.name}' buildIndex={scene.buildIndex} " +
            $"path='{scene.path}' isLoaded={scene.isLoaded} rootCount={scene.rootCount}");
    }

    static void EnsureHooks()
    {
        if (_hooked)
        {
            return;
        }

        _hooked = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
        Application.logMessageReceived += HandleLogMessage;
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log(
            "SCENES",
            $"sceneLoaded name='{scene.name}' buildIndex={scene.buildIndex} mode={mode} " +
            $"rootCount={scene.rootCount}");
    }

    static void HandleSceneUnloaded(Scene scene)
    {
        Log("SCENES", $"sceneUnloaded name='{scene.name}' buildIndex={scene.buildIndex}");
    }

    static void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error)
        {
            return;
        }

        // Avoid recursive noise from our own error channel; still capture foreign failures.
        if (condition != null && condition.StartsWith("[BOOTTRACE]", StringComparison.Ordinal))
        {
            return;
        }

        Log(
            "BOOT",
            $"CAPTURED {type}: {condition}");
    }

    public static void Log(string tag, string message)
    {
        _step++;
        Debug.Log($"[{tag}] #{_step} t={Clock.Elapsed.TotalSeconds:F3}s | {message}");
    }

    public static void LogError(string tag, string message)
    {
        _step++;
        Debug.LogError($"[{tag}] #{_step} t={Clock.Elapsed.TotalSeconds:F3}s | {message}");
    }

    public static void LogEnvironment(string context)
    {
        var sb = new StringBuilder(512);
        sb.Append(context);
        sb.Append(" isEditor=").Append(Application.isEditor);
        sb.Append(" isPlaying=").Append(Application.isPlaying);
        sb.Append(" platform=").Append(Application.platform);
        sb.Append(" version=").Append(Application.version);
        sb.Append(" unity=").Append(Application.unityVersion);
        sb.Append(" product='").Append(Application.productName).Append('\'');
        sb.Append(" company='").Append(Application.companyName).Append('\'');
        sb.Append(" dataPath='").Append(Application.dataPath).Append('\'');
        sb.Append(" persistentDataPath='").Append(Application.persistentDataPath).Append('\'');
        sb.Append(" streamingAssetsPath='").Append(Application.streamingAssetsPath).Append('\'');
        sb.Append(" systemLanguage=").Append(Application.systemLanguage);
        sb.Append(" internetReachability=").Append(Application.internetReachability);
#if DEVELOPMENT_BUILD
        sb.Append(" DEVELOPMENT_BUILD=1");
#else
        sb.Append(" DEVELOPMENT_BUILD=0");
#endif
#if UNITY_EDITOR
        sb.Append(" UNITY_EDITOR=1");
#else
        sb.Append(" UNITY_EDITOR=0");
#endif
        sb.Append(" sceneCountInBuildSettings=").Append(SceneManager.sceneCountInBuildSettings);
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            sb.Append(" [").Append(i).Append("]=")
                .Append(SceneUtility.GetScenePathByBuildIndex(i));
        }

        Log("BOOT", sb.ToString());
    }

    public static void ProbeCriticalShaders(string context)
    {
        string[] names =
        {
            "Standard",
            "Unlit/Color",
            "Universal Render Pipeline/Lit",
            "CoreWar/VoxelFaceLit",
            "Hidden/CoreWar/SniperScopePost",
            "Hidden/CoreWar/FullScreenBlur",
            "Hidden/CoreWar/PenInkShadowPost",
            "UI/Default",
            "Sprites/Default",
            "Legacy Shaders/Diffuse"
        };

        var sb = new StringBuilder(256);
        sb.Append(context).Append(" Shader.Find probes:");
        foreach (var name in names)
        {
            var shader = Shader.Find(name);
            sb.Append(" | '").Append(name).Append("'=")
                .Append(shader == null ? "NULL" : "ok");
        }

        Log("VOXELS", sb.ToString());
    }

    public static void ProbeCriticalResources(string context)
    {
        var networkManager = Resources.Load<GameObject>("NetworkManager");
        var networkPlayer = Resources.Load<GameObject>("NetworkPlayer");
        Log(
            "NETWORK",
            $"{context} Resources.Load NetworkManager={(networkManager == null ? "NULL" : networkManager.name)} " +
            $"NetworkPlayer={(networkPlayer == null ? "NULL" : networkPlayer.name)}");
    }

    public static string DescribeShaderFind(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        return shader == null ? "NULL" : $"ok name='{shader.name}'";
    }
}
