using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayerDeathHandler : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (health != null)
            health.OnDeath.AddListener(OnPlayerDeath);
    }

    private void OnPlayerDeath()
    {
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
