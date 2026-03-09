using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreenController : MonoBehaviour
{
    [Header("Nombre de la escena a cargar")]
    public string nextScene = "LoginScene";

    private bool isTransitioning = false;

    void Update()
    {
        if (isTransitioning) return;

        // Cualquier clic de mouse o toque en pantalla
        if (Input.GetMouseButtonDown(0))
        {
            LoadNextScene();
        }

        // Cualquier tecla
        if (Input.anyKeyDown)
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        isTransitioning = true;
        SceneManager.LoadScene(nextScene);
    }
}
