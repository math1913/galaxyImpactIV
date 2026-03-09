using UnityEngine;
using UnityEngine.SceneManagement;

public class AchivementController : MonoBehaviour
{
    // Nombre de la escena del menú principal
    public string menuSceneName = "MainMenu";

    public void GoBackToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}
