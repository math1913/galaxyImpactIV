using UnityEngine;
using UnityEngine.SceneManagement;

public class IrALobby : MonoBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";

    public void CargarLobby()
    {
        LanSessionLifecycle.ShutdownSession();
        SceneManager.LoadScene(lobbySceneName);
    }
}
