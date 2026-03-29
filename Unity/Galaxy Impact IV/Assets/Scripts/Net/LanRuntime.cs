using Unity.Netcode;
using UnityEngine;

public static class LanRuntime
{
    public static NetworkManager NetworkManager => NetworkManager.Singleton;

    public static bool IsActive => NetworkManager != null && NetworkManager.IsListening;

    public static bool IsServer => IsActive && NetworkManager.IsServer;

    public static bool IsClientOnly => IsActive && NetworkManager.IsClient && !NetworkManager.IsServer;

    public static bool IsNetworkedObject(GameObject gameObject)
    {
        return gameObject != null && gameObject.TryGetComponent<NetworkObject>(out _);
    }

    public static bool IsClientReplica(GameObject gameObject)
    {
        return IsActive && IsNetworkedObject(gameObject) && !IsServer;
    }
}
