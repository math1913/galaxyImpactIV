using UnityEngine;
using TMPro;

public class PlayerIdentity : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text nameTagText;

    void Start()
    {
        // 1. Recuperamos el nombre guardado en PlayerPrefs
        // Si por alguna razón no hay nada, pondrá "Jugador" por defecto
        string savedName = PlayerPrefs.GetString("username", "Jugador");

        // 2. Lo asignamos al componente de texto
        if (nameTagText != null)
        {
            nameTagText.text = savedName;
        }
    }

    void LateUpdate()
    {
        // Esto evita que el texto gire cuando el jugador rota para apuntar
        // Mantiene el texto siempre derecho para que sea legible
        if (nameTagText != null)
        {
            nameTagText.transform.rotation = Quaternion.identity;
        }
    }
}