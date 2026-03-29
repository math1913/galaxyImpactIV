using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LanSessionLifecycle
{
    public static bool IsExitingToLobby { get; private set; }
    public static bool LastClosedSessionWasLan { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        IsExitingToLobby = false;
        LastClosedSessionWasLan = false;
    }

    public static void MarkLobbyReady()
    {
        Time.timeScale = 1f;
        IsExitingToLobby = false;
        LastClosedSessionWasLan = false;
    }

    public static void MarkLanRunEnded()
    {
        Time.timeScale = 1f;
        LastClosedSessionWasLan = true;
    }

    public static void ShutdownSession()
    {
        Time.timeScale = 1f;
        LanPlayerAvatar.ResetRuntimeState();

        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        LastClosedSessionWasLan = networkManager.IsListening || networkManager.IsClient || networkManager.IsServer;

        if (networkManager.IsListening)
            networkManager.Shutdown();

        if (networkManager.gameObject != null)
            Object.Destroy(networkManager.gameObject);
    }

    public static void ExitToLobby(string lobbySceneName = "Lobby")
    {
        if (IsExitingToLobby)
            return;

        IsExitingToLobby = true;
        ShutdownSession();
        SceneManager.LoadScene(lobbySceneName);
    }
}
