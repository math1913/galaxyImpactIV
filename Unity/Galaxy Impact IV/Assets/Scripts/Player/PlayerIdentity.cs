using TMPro;
using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    private const string NameTagObjectName = "PlayerNameText";
    private const string OverlayRootObjectName = "PlayerIdentityOverlay";
    private const string HealthBarRootObjectName = "PlayerHealthBar";
    private const string HealthBarBackgroundObjectName = "HealthBarBackground";
    private const string HealthBarFillObjectName = "HealthBarFill";

    private static Sprite barSprite;

    [Header("UI")]
    [SerializeField] private TMP_Text nameTagText;
    [SerializeField] private bool applySavedNameOnStart = true;
    [SerializeField] private bool showHealthBarInLanOnly = true;
    [SerializeField] private bool forceShowHealthBarForTesting = false;
    [SerializeField] private string fallbackName = "Jugador";
    [SerializeField] private Vector3 nameWorldOffset = new Vector3(0f, 1.15f, 0f);
    [SerializeField] private Vector3 healthBarWorldOffset = new Vector3(0f, 0.92f, 0f);
    [SerializeField] private Vector2 healthBarSize = new Vector2(1.55f, 0.2f);

    private Health health;
    private Transform overlayRoot;
    private Transform healthBarRoot;
    private SpriteRenderer healthBarBackground;
    private SpriteRenderer healthBarFill;
    private bool visible = true;
    private bool showHealthBarForThisPlayer = true;
    private bool healthSubscribed;
    private Vector3 nameTagOverlayScale = Vector3.one;
    private bool hasNameTagOverlayScale;

    private void Awake()
    {
        health = GetComponent<Health>();
        EnsureOverlayReady();
        ResolveNameTagReference();
        EnsureHealthBarReady();
        SubscribeHealth();
    }

    private void OnEnable()
    {
        SubscribeHealth();
        RefreshHealthBar();
    }

    private void Start()
    {
        if (applySavedNameOnStart)
            SetDisplayName(PlayerPrefs.GetString("username", fallbackName));

        RefreshHealthBar();
    }

    private void OnDisable()
    {
        UnsubscribeHealth();
    }

    private void OnDestroy()
    {
        if (overlayRoot != null)
            Destroy(overlayRoot.gameObject);
    }

    public void SetApplySavedNameOnStart(bool value)
    {
        applySavedNameOnStart = value;
    }

    public void SetShowHealthBar(bool value)
    {
        showHealthBarForThisPlayer = value;
        RefreshHealthBar();
    }

    public void EnsureNameTagReady()
    {
        EnsureOverlayReady();
        ResolveNameTagReference();
        EnsureHealthBarReady();
        RefreshHealthBar();
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
        this.visible = visible;

        if (nameTagText != null)
            nameTagText.gameObject.SetActive(visible);

        RefreshHealthBar();
    }

    private void LateUpdate()
    {
        EnsureOverlayReady();
        overlayRoot.SetPositionAndRotation(transform.position, Quaternion.identity);

        if (nameTagText != null)
        {
            nameTagText.transform.localPosition = nameWorldOffset;
            nameTagText.transform.localRotation = Quaternion.identity;
            nameTagText.transform.localScale = nameTagOverlayScale;
        }

        if (healthBarRoot != null)
        {
            healthBarRoot.localPosition = healthBarWorldOffset;
            healthBarRoot.localRotation = Quaternion.identity;
        }

        if (forceShowHealthBarForTesting)
            RefreshHealthBar();
    }

    private void EnsureOverlayReady()
    {
        if (overlayRoot != null)
            return;

        GameObject overlay = new GameObject($"{OverlayRootObjectName}_{GetInstanceID()}");
        overlay.layer = gameObject.layer;
        overlay.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
        overlayRoot = overlay.transform;
    }

    private void ResolveNameTagReference()
    {
        EnsureOverlayReady();

        if (nameTagText != null)
        {
            ConfigureNameTagVisuals(nameTagText);
            return;
        }

        nameTagText = FindNamedText(NameTagObjectName);
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

    private TMP_Text FindNamedText(string objectName)
    {
        Transform namedTag = transform.Find(objectName);
        if (namedTag == null)
            return null;

        return namedTag.GetComponent<TMP_Text>();
    }

    private TMP_Text CreateFallbackNameTag()
    {
        Debug.LogWarning($"[{nameof(PlayerIdentity)}] Name tag missing on '{name}'. Creating runtime fallback.");

        GameObject nameTag = new GameObject(NameTagObjectName);
        nameTag.layer = gameObject.layer;

        TextMeshPro text = nameTag.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 12f;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = Color.white;
        text.rectTransform.sizeDelta = new Vector2(24f, 4f);
        text.text = fallbackName;

        return text;
    }

    private void ConfigureNameTagVisuals(TMP_Text text)
    {
        if (text == null)
            return;

        EnsureOverlayReady();

        CaptureNameTagOverlayScale(text.transform);
        text.transform.SetParent(overlayRoot, false);
        text.gameObject.layer = gameObject.layer;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.transform.localPosition = nameWorldOffset;
        text.transform.localRotation = Quaternion.identity;
        text.transform.localScale = nameTagOverlayScale;

        TextoFijo oldFollower = text.GetComponent<TextoFijo>();
        if (oldFollower != null)
            oldFollower.enabled = false;

        if (text is TextMeshPro worldText)
            ConfigureTextSorting(worldText, 40);
    }

    private void CaptureNameTagOverlayScale(Transform textTransform)
    {
        if (hasNameTagOverlayScale || textTransform == null)
            return;

        Vector3 inheritedScale = Vector3.one;
        Transform parent = textTransform.parent;
        if (parent != null && parent != overlayRoot)
            inheritedScale = parent.lossyScale;

        nameTagOverlayScale = new Vector3(
            NormalizeScale(textTransform.localScale.x * inheritedScale.x),
            NormalizeScale(textTransform.localScale.y * inheritedScale.y),
            NormalizeScale(textTransform.localScale.z * inheritedScale.z));
        hasNameTagOverlayScale = true;
    }

    private static float NormalizeScale(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : Mathf.Abs(value);
    }

    private void EnsureHealthBarReady()
    {
        EnsureOverlayReady();

        if (healthBarRoot == null)
        {
            GameObject barRootObject = new GameObject(HealthBarRootObjectName);
            barRootObject.layer = gameObject.layer;
            barRootObject.transform.SetParent(overlayRoot, false);
            healthBarRoot = barRootObject.transform;
            healthBarRoot.localPosition = healthBarWorldOffset;
            healthBarRoot.localRotation = Quaternion.identity;
        }

        if (healthBarBackground == null)
            healthBarBackground = CreateBarRenderer(HealthBarBackgroundObjectName, new Color(0f, 0f, 0f, 0.72f), 38);

        if (healthBarFill == null)
            healthBarFill = CreateBarRenderer(HealthBarFillObjectName, new Color(0.45f, 1f, 0.45f, 1f), 39);

        healthBarBackground.transform.localPosition = Vector3.zero;
        healthBarFill.transform.localPosition = Vector3.zero;
        healthBarBackground.transform.localScale = new Vector3(healthBarSize.x, healthBarSize.y, 1f);
        healthBarFill.transform.localScale = new Vector3(healthBarSize.x, healthBarSize.y * 0.72f, 1f);
    }

    private SpriteRenderer CreateBarRenderer(string objectName, Color color, int sortingOrderOffset)
    {
        GameObject barObject = new GameObject(objectName);
        barObject.layer = gameObject.layer;
        barObject.transform.SetParent(healthBarRoot, false);

        SpriteRenderer renderer = barObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetBarSprite();
        renderer.color = color;

        SpriteRenderer parentRenderer = GetComponent<SpriteRenderer>();
        if (parentRenderer != null)
        {
            renderer.sortingLayerID = parentRenderer.sortingLayerID;
            renderer.sortingOrder = parentRenderer.sortingOrder + sortingOrderOffset;
        }
        else
        {
            renderer.sortingOrder = sortingOrderOffset;
        }

        return renderer;
    }

    private static Sprite GetBarSprite()
    {
        if (barSprite != null)
            return barSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        barSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return barSprite;
    }

    private void ConfigureTextSorting(TextMeshPro worldText, int sortingOrderOffset)
    {
        SpriteRenderer parentRenderer = GetComponent<SpriteRenderer>();
        Renderer meshRenderer = worldText.GetComponent<Renderer>();
        if (meshRenderer != null && parentRenderer != null)
        {
            meshRenderer.sortingLayerID = parentRenderer.sortingLayerID;
            meshRenderer.sortingOrder = parentRenderer.sortingOrder + sortingOrderOffset;
        }
        else
        {
            worldText.sortingOrder = Mathf.Max(worldText.sortingOrder, sortingOrderOffset);
        }
    }

    private void SubscribeHealth()
    {
        if (healthSubscribed)
            return;

        if (health == null)
            health = GetComponent<Health>();

        if (health == null)
            return;

        health.OnHealthChanged.AddListener(HandleHealthChanged);
        healthSubscribed = true;
    }

    private void UnsubscribeHealth()
    {
        if (!healthSubscribed || health == null)
            return;

        health.OnHealthChanged.RemoveListener(HandleHealthChanged);
        healthSubscribed = false;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        RefreshHealthBar(currentHealth, maxHealth);
    }

    private void RefreshHealthBar()
    {
        if (health == null)
            health = GetComponent<Health>();

        int currentHealth = health != null ? health.CurrentHealth : 0;
        int maxHealth = health != null ? health.MaxHealth : 0;
        RefreshHealthBar(currentHealth, maxHealth);
    }

    private void RefreshHealthBar(int currentHealth, int maxHealth)
    {
        EnsureHealthBarReady();
        if (healthBarRoot == null || healthBarFill == null || healthBarBackground == null)
            return;

        bool shouldShow = ShouldShowHealthBar();
        healthBarRoot.gameObject.SetActive(shouldShow);
        if (!shouldShow)
            return;

        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        float percentage = Mathf.Clamp01((float)currentHealth / maxHealth);
        float fillWidth = healthBarSize.x * percentage;
        healthBarFill.transform.localScale = new Vector3(fillWidth, healthBarSize.y * 0.72f, 1f);
        healthBarFill.transform.localPosition = new Vector3((fillWidth - healthBarSize.x) * 0.5f, 0f, -0.01f);
        healthBarFill.color = GetHealthColor(percentage);
    }

    private bool ShouldShowHealthBar()
    {
        if (forceShowHealthBarForTesting)
            return visible;

        return visible && showHealthBarForThisPlayer && (!showHealthBarInLanOnly || LanRuntime.IsActive);
    }

    private static Color GetHealthColor(float percentage)
    {
        if (percentage > 0.5f)
            return Color.Lerp(new Color(1f, 0.84f, 0.25f, 1f), new Color(0.45f, 1f, 0.45f, 1f), (percentage - 0.5f) * 2f);

        return Color.Lerp(new Color(1f, 0.25f, 0.25f, 1f), new Color(1f, 0.84f, 0.25f, 1f), percentage * 2f);
    }
}
