using UnityEngine;

/// <summary>
/// DontDestroyOnLoad carrier for match authorization across scene loads.
/// Static <see cref="GameSession"/> fields normally persist too; this object is the
/// authoritative entry token when scene transitions occur.
/// </summary>
public class GameSessionLifetime : MonoBehaviour
{
    public static GameSessionLifetime Instance { get; private set; }

    public bool matchActive;
    public string gameModeId;
    public bool inPrepPhase;
    public int entryToken;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
