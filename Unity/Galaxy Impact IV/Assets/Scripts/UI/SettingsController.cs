using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsController : MonoBehaviour
{
    [Header("Botón Exit")]
    public Button exitButton;

    [Header("Nombre de la escena del menú principal")]
    public string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        exitButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
