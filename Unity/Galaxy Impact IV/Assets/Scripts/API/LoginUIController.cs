using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading;
using System.Collections;
public class LoginUIController : MonoBehaviour
{

    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TMP_Text messageText;
    public AuthService authService;


    public async void OnLoginButton()
    {
        string user = usernameField.text;
        string pass = passwordField.text;


        var response = await authService.Login(user, pass);


        if (response.status == 200)
        {
            PlayerPrefs.SetInt("userId", response.id);
            PlayerPrefs.Save();
            StartCoroutine(LoginCorrecto());
        }
        else
        {
            messageText.text = "Error: Credenciales incorrectas";
        }
    }

    IEnumerator LoginCorrecto()
    {
        if (ColorUtility.TryParseHtmlString("#69F38D", out Color colorTMP))
            messageText.color = colorTMP;
        else
            messageText.color = Color.green;

        messageText.text = "Login Correcto";
        // Espera 2 segundos sin bloquear la UI
        yield return new WaitForSeconds(1.5f);

        SceneManager.LoadScene("MainMenu");
    }

    public void OnRegisterButton()
    {
        SceneManager.LoadScene("RegisterScene");
    }
}