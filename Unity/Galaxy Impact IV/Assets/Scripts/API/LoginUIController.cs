using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using System.Threading;
using System.Collections;
public class LoginUIController : MonoBehaviour
{

    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TMP_Text messageText;
    public AuthService authService;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            SelectNextInputField(usernameField, passwordField);
    }

    public async void OnLoginButton()
    {
        string user = usernameField.text;
        string pass = passwordField.text;


        var response = await authService.Login(user, pass);


        if (response.status == 200)
        {
            PlayerPrefs.SetInt("userId", response.id);
            PlayerPrefs.SetString("username", response.user);
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

    private static void SelectNextInputField(params TMP_InputField[] fields)
    {
        if (EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
            return;

        for (int i = 0; i < fields.Length; i++)
        {
            TMP_InputField field = fields[i];
            if (field == null || field.gameObject != selectedObject)
                continue;

            TMP_InputField nextField = FindNextValidField(fields, i);
            if (nextField == null)
                return;

            EventSystem.current.SetSelectedGameObject(nextField.gameObject);
            nextField.ActivateInputField();
            return;
        }
    }

    private static TMP_InputField FindNextValidField(TMP_InputField[] fields, int currentIndex)
    {
        for (int offset = 1; offset <= fields.Length; offset++)
        {
            TMP_InputField nextField = fields[(currentIndex + offset) % fields.Length];
            if (nextField != null && nextField.interactable)
                return nextField;
        }

        return null;
    }
}
