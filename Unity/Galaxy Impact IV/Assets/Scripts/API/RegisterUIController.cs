using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class RegisterUIController : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TMP_Text messageText;
    public AuthService authService;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            SelectNextInputField(usernameField, emailField, passwordField);
    }

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
