using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class RegisterUIController : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TMP_Text messageText;
    public AuthService authService;


    public async void OnRegisterButton()
    {
        var response = await authService.Register(usernameField.text, emailField.text, passwordField.text);
        if (response.idUsuario > 0)
        {
        if (ColorUtility.TryParseHtmlString("#69F38D", out Color colorTMP))
            messageText.color = colorTMP;
        else
            messageText.color = Color.green;
        messageText.text = "Cuenta creada. Volviendo al login...";
        await System.Threading.Tasks.Task.Delay(1500);
        SceneManager.LoadScene("LoginScene");
        }
        else
        {

        messageText.text = "Error al registrar usuario";
        }
    }
    public void OnBackToLogin()
    {
    SceneManager.LoadScene("LoginScene");
    }
}