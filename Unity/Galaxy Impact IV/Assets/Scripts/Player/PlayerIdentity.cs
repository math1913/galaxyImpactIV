using TMPro;
using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameTagText;
    [SerializeField] private bool applySavedNameOnStart = true;
    [SerializeField] private string fallbackName = "Jugador";

    private void Start()
    {
        if (!applySavedNameOnStart)
            return;

        SetDisplayName(PlayerPrefs.GetString("username", fallbackName));
    }

    public void SetDisplayName(string displayName)
    {
        if (nameTagText == null)
            return;

        string normalized = string.IsNullOrWhiteSpace(displayName) ? fallbackName : displayName.Trim();
        nameTagText.text = normalized;
    }

    public void SetVisible(bool visible)
    {
        if (nameTagText == null)
            return;

        nameTagText.gameObject.SetActive(visible);
    }

    private void LateUpdate()
    {
        if (nameTagText != null)
            nameTagText.transform.rotation = Quaternion.identity;
    }
}
