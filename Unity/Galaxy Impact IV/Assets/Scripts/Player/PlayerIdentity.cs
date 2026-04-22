using TMPro;
using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameTagText;
    [SerializeField] private bool applySavedNameOnStart = true;
    [SerializeField] private string fallbackName = "Jugador";
    [SerializeField] private Vector3 nameWorldOffset = new Vector3(0f, 1.35f, 0f);

    private void Awake()
    {
        ResolveNameTagReference();
    }

    private void Start()
    {
        if (!applySavedNameOnStart)
            return;

        SetDisplayName(PlayerPrefs.GetString("username", fallbackName));
    }

    public void SetApplySavedNameOnStart(bool value)
    {
        applySavedNameOnStart = value;
    }

    public void EnsureNameTagReady()
    {
        ResolveNameTagReference();
    }

    public void SetDisplayName(string displayName)
    {
        ResolveNameTagReference();
        if (nameTagText == null)
            return;

        string normalized = string.IsNullOrWhiteSpace(displayName) ? fallbackName : displayName.Trim();
        nameTagText.text = normalized;
    }

    public void SetVisible(bool visible)
    {
        ResolveNameTagReference();
        if (nameTagText == null)
            return;

        nameTagText.gameObject.SetActive(visible);
    }

    private void LateUpdate()
    {
        if (nameTagText != null)
        {
            nameTagText.transform.position = transform.position + nameWorldOffset;
            nameTagText.transform.rotation = Quaternion.identity;
        }
    }

    private void ResolveNameTagReference()
    {
        if (nameTagText != null)
        {
            ConfigureNameTagVisuals(nameTagText);
            return;
        }

        nameTagText = FindNamedNameTag();
        if (nameTagText != null)
        {
            ConfigureNameTagVisuals(nameTagText);
            return;
        }

        nameTagText = GetComponentInChildren<TMP_Text>(true);
        if (nameTagText != null)
        {
            ConfigureNameTagVisuals(nameTagText);
            return;
        }

        nameTagText = CreateFallbackNameTag();
        ConfigureNameTagVisuals(nameTagText);
    }

    private TMP_Text FindNamedNameTag()
    {
        Transform namedTag = transform.Find("PlayerNameText");
        if (namedTag == null)
            return null;

        return namedTag.GetComponent<TMP_Text>();
    }

    private TMP_Text CreateFallbackNameTag()
    {
        Debug.LogWarning($"[{nameof(PlayerIdentity)}] Name tag missing on '{name}'. Creating runtime fallback.");

        GameObject nameTag = new GameObject("PlayerNameText");
        nameTag.layer = gameObject.layer;

        Transform nameTagTransform = nameTag.transform;
        nameTagTransform.SetParent(transform, false);
        nameTagTransform.position = transform.position + nameWorldOffset;

        TextMeshPro text = nameTag.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 12f;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
        text.color = Color.white;
        text.rectTransform.sizeDelta = new Vector2(24f, 4f);
        text.text = fallbackName;

        return text;
    }

    private void ConfigureNameTagVisuals(TMP_Text text)
    {
        if (text == null)
            return;

        text.transform.SetParent(transform, true);
        text.gameObject.layer = gameObject.layer;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = Mathf.Max(text.fontSize, 12f);

        RectTransform rectTransform = text.rectTransform;
        if (rectTransform != null)
            rectTransform.sizeDelta = new Vector2(Mathf.Max(rectTransform.sizeDelta.x, 24f), Mathf.Max(rectTransform.sizeDelta.y, 4f));

        TextoFijo oldFollower = text.GetComponent<TextoFijo>();
        if (oldFollower != null)
            oldFollower.enabled = false;

        if (text is TextMeshPro worldText)
        {
            SpriteRenderer parentRenderer = GetComponent<SpriteRenderer>();
            Renderer meshRenderer = worldText.GetComponent<Renderer>();
            if (meshRenderer != null && parentRenderer != null)
            {
                meshRenderer.sortingLayerID = parentRenderer.sortingLayerID;
                meshRenderer.sortingOrder = parentRenderer.sortingOrder + 40;
            }
            else
            {
                worldText.sortingOrder = Mathf.Max(worldText.sortingOrder, 50);
            }
        }
    }
}
