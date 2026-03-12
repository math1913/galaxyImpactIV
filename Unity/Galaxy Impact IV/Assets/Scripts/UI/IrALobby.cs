using UnityEngine;
using UnityEngine.SceneManagement;

public class IrALobby : MonoBehaviour
{
    public void CargarLobby()
    {
        SceneManager.LoadScene("Lobby");
    }
}