using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [Header("Panel de Pausa")]
    public GameObject pausePanel;

    private bool isPaused = false;

    private void Start()
    {
        // Asegurar que arranca desactivado
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // Por si tienes un botón "Volver al menú"
    public void GoToMainMenu(string mainMenuSceneName)
    {
        Time.timeScale = 1f; // importante para no dejar el tiempo congelado
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
