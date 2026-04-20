using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    private Health health;
    private LanPlayerAvatar lanPlayerAvatar;

    private void Awake()
    {
        health = GetComponent<Health>();
        lanPlayerAvatar = GetComponent<LanPlayerAvatar>();
        if (health != null)
            health.OnDeath.AddListener(OnPlayerDeath);
    }

    private void OnPlayerDeath()
    {
        if (LanRuntime.IsActive)
        {
            if (!LanRuntime.IsServer || lanPlayerAvatar == null)
                return;

            Debug.Log("El jugador ha muerto en LAN. Evaluando si todos los jugadores estan caidos.");
            LanPlayerAvatar.ServerHandlePlayerDeath(lanPlayerAvatar);
            return;
        }

        Debug.Log("El jugador ha muerto.");

        if (GameStatsManager.Instance != null)
        {
            Debug.Log("Enviando stats a la API (background)...");
            _ = GameStatsManager.Instance.EndRunAndSendToApi();
        }
        else
        {
            Debug.LogWarning("GameStatsManager.Instance es null");
            SceneManager.LoadScene("GameOver");
        }
    }
}
