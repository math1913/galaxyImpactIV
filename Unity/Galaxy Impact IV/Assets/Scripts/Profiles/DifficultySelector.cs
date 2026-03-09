using UnityEngine;
using UnityEngine.UI;

public class DifficultySelector : MonoBehaviour
{
    [Header("Botones de dificultad")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    [Header("Colores")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;

    private string currentDifficulty;

    private void Awake()
    {
        // Cargar dificultad guardada (o Medium por defecto)
        currentDifficulty = PlayerPrefs.GetString("Difficulty", "Medium");

        // Asignar eventos
        easyButton.onClick.AddListener(() => SetDifficulty("Easy"));
        mediumButton.onClick.AddListener(() => SetDifficulty("Medium"));
        hardButton.onClick.AddListener(() => SetDifficulty("Hard"));

        // Actualizar highlight al abrir el menú
        UpdateButtonHighlights();
    }

    private void SetDifficulty(string difficultyName)
    {
        currentDifficulty = difficultyName;

        PlayerPrefs.SetString("Difficulty", difficultyName);
        PlayerPrefs.Save();

        UpdateButtonHighlights();
    }

    private void UpdateButtonHighlights()
    {
        // Reset general
        SetButtonColor(easyButton, normalColor);
        SetButtonColor(mediumButton, normalColor);
        SetButtonColor(hardButton, normalColor);

        // Activar seleccionado
        switch (currentDifficulty)
        {
            case "Easy":   SetButtonColor(easyButton, selectedColor); break;
            case "Medium": SetButtonColor(mediumButton, selectedColor); break;
            case "Hard":   SetButtonColor(hardButton, selectedColor); break;
        }
    }

    private void SetButtonColor(Button button, Color color)
    {
        var colors = button.colors;
        colors.normalColor = color;
        colors.selectedColor = color;
        colors.highlightedColor = color;
        button.colors = colors;
    }
}
