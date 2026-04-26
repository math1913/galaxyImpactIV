using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class MainMenuController : MonoBehaviour
{
    private const string LoginSceneName = "LoginScene";
    private const string GameSceneName = "GameScene";
    private const string SettingsSceneName = "Settings";
    private const string AchievementsSceneName = "Achievements";
    private const string LobbySceneName = "Lobby";
    private const string SelectSkinSceneName = "selectSkin";
    private const string LanPlayerPreviewResource = "Net/LanPlayer";
    private const string PreviewFrameObjectName = "PreviewFrame";
    private const string BorderPreviewObjectName = "BorderPreview";
    private const string PlayerStatsObjectName = "PlayerStats";
    private const string SkinPreviewObjectName = "CurrentSkinPreview";

    [Header("Player Stats UI")]
    public TMP_Text usernameText;
    public TMP_Text levelText;
    public TMP_Text aliensKilledText;
    public TMP_Text highScoreText;

    [Header("Opcional: titulo u otros textos")]
    public TMP_Text titleText;

    [Header("Main Menu Layout")]
    [SerializeField] private RectTransform previewFrame;
    [SerializeField] private RectTransform previewBorder;
    [SerializeField] private RectTransform playerStatsPanel;
    [SerializeField] private Image skinPreviewImage;
    [SerializeField] private Sprite skinPreviewBackgroundSprite;
    [SerializeField] private Sprite defaultLobbySprite;
    [SerializeField] private Sprite mateoLobbySprite;
    [SerializeField] private Sprite ericLobbySprite;
    [SerializeField] private Sprite ignasiLobbySprite;
    [Tooltip("Ancho y alto del fondo de la skin en el MainMenu.")]
    [SerializeField] private Vector2 skinPreviewFrameSize = new Vector2(840f, 560f);
    [Tooltip("Posicion del fondo. Esta anclado arriba: para bajarlo, usa Y mas negativo.")]
    [SerializeField] private Vector2 skinPreviewFramePosition = new Vector2(0f, -400f);
    [Tooltip("Ancho y alto del personaje dentro del fondo.")]
    [SerializeField] private Vector2 skinPreviewImageSize = new Vector2(460f, 530f);
    [Tooltip("Escala extra solo para la skin default del lobby preview. Usa menos de 1 si se ve mas grande que las demas.")]
    [SerializeField, Range(0.5f, 1.2f)] private float defaultLobbySkinScale = 0.60f;
    [Tooltip("Offset extra solo para la skin default del lobby preview. X positivo mueve a la derecha, Y negativo baja.")]
    [SerializeField] private Vector2 defaultLobbySkinPositionOffset = new Vector2(12f, -35f);
    [Tooltip("Posicion del personaje dentro del fondo.")]
    [SerializeField] private Vector2 skinPreviewImagePosition = new Vector2(0f, -20f);
    [Tooltip("Ancho y alto del panel de usuario y stats.")]
    [SerializeField] private Vector2 statsPanelSize = new Vector2(560f, 175f);
    [Tooltip("Posicion del panel de usuario y stats. Esta anclado abajo.")]
    [SerializeField] private Vector2 statsPanelPosition = new Vector2(0f, 135f);
    [SerializeField] private float usernameFontSize = 24f;
    [SerializeField] private float statFontSize = 20f;

    [Header("Servicios")]
    public AuthService authService;

    private User currentUser;

#if UNITY_EDITOR
    private bool editorLayoutApplyQueued;

    private void OnValidate()
    {
        QueueEditorLayoutApply();
    }

    [ContextMenu("Apply Main Menu Layout")]
    private void ApplyLayoutFromInspector()
    {
        ResolveMenuReferences();
        ConfigureMainMenuLayout();
        MarkLayoutDirty();
    }

    private void QueueEditorLayoutApply()
    {
        if (editorLayoutApplyQueued)
            return;

        editorLayoutApplyQueued = true;
        EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            editorLayoutApplyQueued = false;
            ApplyLayoutFromInspector();
        };
    }

    private void MarkLayoutDirty()
    {
        if (Application.isPlaying)
            return;

        EditorUtility.SetDirty(this);

        if (previewFrame != null)
            EditorUtility.SetDirty(previewFrame);
        if (previewBorder != null)
            EditorUtility.SetDirty(previewBorder);
        if (playerStatsPanel != null)
            EditorUtility.SetDirty(playerStatsPanel);
        if (skinPreviewImage != null)
            EditorUtility.SetDirty(skinPreviewImage);

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private async void Start()
    {
        ResolveMenuReferences();

        int userId = PlayerPrefs.GetInt("userId", -1);
        if (userId == -1)
        {
            Debug.LogWarning("No hay userId en PlayerPrefs. Volviendo al Login.");
            SceneManager.LoadScene(LoginSceneName);
            return;
        }

        ConfigureMainMenuLayout();

        currentUser = await authService.GetUserById(userId);
        if (currentUser != null)
        {
            PlayerPrefs.SetString("username", currentUser.username);
            PlayerPrefs.Save();
        }

        if (currentUser == null)
        {
            Debug.LogError("No se pudo cargar el usuario desde la API");
            return;
        }

        if (usernameText != null)
            usernameText.text = currentUser.username;

        if (levelText != null)
            levelText.text = "Level: " + currentUser.nivelActual;

        int aliensTotales = 0;
        if (currentUser.puntuaciones != null)
        {
            foreach (int killCount in currentUser.puntuaciones)
                aliensTotales += killCount;
        }

        if (aliensKilledText != null)
            aliensKilledText.text = "Aliens Killed: " + aliensTotales;

        int highScore = 0;
        if (currentUser.puntuaciones != null && currentUser.puntuaciones.Length > 0)
        {
            foreach (int score in currentUser.puntuaciones)
            {
                if (score > highScore)
                    highScore = score;
            }
        }

        if (highScoreText != null)
            highScoreText.text = "High Score: " + highScore;

        if (titleText != null)
            titleText.text = "MAIN MENU";
    }

    public void OnStartGame()
    {
        SceneManager.LoadScene(GameSceneName);
    }

    public void OnSettings()
    {
        SceneManager.LoadScene(SettingsSceneName);
    }

    public void OnLogros()
    {
        SceneManager.LoadScene(AchievementsSceneName);
    }

    public void OnLobby()
    {
        LanSessionLifecycle.ShutdownSession();
        SceneManager.LoadScene(LobbySceneName);
    }

    public void OnSelectSkin()
    {
        SceneManager.LoadScene(SelectSkinSceneName);
    }

    public void OnLogout()
    {
        PlayerPrefs.DeleteKey("userId");
        PlayerPrefs.Save();
        SceneManager.LoadScene(LoginSceneName);
    }

    private void ResolveMenuReferences()
    {
        usernameText = usernameText != null ? usernameText : FindTextByName("Username");
        levelText = levelText != null ? levelText : FindTextByName("Level");
        aliensKilledText = aliensKilledText != null ? aliensKilledText : FindTextByName("AlliensKilled");
        highScoreText = highScoreText != null ? highScoreText : FindTextByName("HighScore");
        previewFrame = previewFrame != null ? previewFrame : FindRectByName(PreviewFrameObjectName);
        previewBorder = previewBorder != null ? previewBorder : FindRectByName(BorderPreviewObjectName);
        playerStatsPanel = playerStatsPanel != null ? playerStatsPanel : FindRectByName(PlayerStatsObjectName);
    }

    private void ConfigureMainMenuLayout()
    {
        ConfigureSkinPreview();
        ConfigureStatsPanel();
    }

    private void ConfigureSkinPreview()
    {
        if (previewFrame == null)
            return;

        Vector2 frameSize = SizeOrFallback(skinPreviewFrameSize, new Vector2(840f, 560f));
        previewFrame.sizeDelta = frameSize;
        previewFrame.anchoredPosition = skinPreviewFramePosition;

        if (previewBorder != null)
        {
            previewBorder.sizeDelta = frameSize + new Vector2(20f, 20f);
            previewBorder.anchoredPosition = previewFrame.anchoredPosition;
        }

        Image frameImage = previewFrame.GetComponent<Image>();
        if (frameImage != null)
        {
            frameImage.sprite = skinPreviewBackgroundSprite;
            frameImage.type = Image.Type.Simple;
            frameImage.preserveAspect = false;
            frameImage.color = skinPreviewBackgroundSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.25f);
            frameImage.raycastTarget = false;
        }

        foreach (TMP_Text text in previewFrame.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.text.ToUpperInvariant().Contains("GAME PREVIEW"))
                text.gameObject.SetActive(false);
        }

        if (skinPreviewImage == null)
            skinPreviewImage = FindOrCreateSkinPreviewImage(previewFrame);

        if (skinPreviewImage == null)
            return;

        RectTransform skinRect = skinPreviewImage.rectTransform;
        skinRect.anchorMin = new Vector2(0.5f, 0.5f);
        skinRect.anchorMax = new Vector2(0.5f, 0.5f);
        skinRect.pivot = new Vector2(0.5f, 0.5f);
        int selectedSkinIndex = PlayerSkinProfile.GetSelectedSkinIndex();
        skinRect.anchoredPosition = GetSkinPreviewImagePosition(selectedSkinIndex);
        skinRect.sizeDelta = GetSkinPreviewImageSize(selectedSkinIndex);

        skinPreviewImage.sprite = LoadSelectedSkinSprite(selectedSkinIndex);
        skinPreviewImage.preserveAspect = true;
        skinPreviewImage.raycastTarget = false;
        skinPreviewImage.color = Color.white;
        skinPreviewImage.enabled = skinPreviewImage.sprite != null;
    }

    private void ConfigureStatsPanel()
    {
        if (playerStatsPanel == null)
            return;

        playerStatsPanel.sizeDelta = SizeOrFallback(statsPanelSize, new Vector2(560f, 175f));
        playerStatsPanel.anchoredPosition = statsPanelPosition;

        Image statsImage = playerStatsPanel.GetComponent<Image>();
        if (statsImage != null)
            statsImage.color = new Color(0f, 0f, 0f, 0.45f);

        ConfigureStatsText(usernameText, 48f, usernameFontSize, 520f, 32f);
        ConfigureStatsText(levelText, 14f, statFontSize, 520f, 28f);
        ConfigureStatsText(aliensKilledText, -20f, statFontSize, 520f, 28f);
        ConfigureStatsText(highScoreText, -54f, statFontSize, 520f, 28f);
    }

    private Image FindOrCreateSkinPreviewImage(RectTransform parent)
    {
        Transform existing = parent.Find(SkinPreviewObjectName);
        if (existing != null)
            return existing.GetComponent<Image>();

        GameObject previewObject = new GameObject(SkinPreviewObjectName, typeof(RectTransform), typeof(Image));
        RectTransform rectTransform = previewObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.SetAsLastSibling();
        return previewObject.GetComponent<Image>();
    }

    private Vector2 GetSkinPreviewImageSize(int skinIndex)
    {
        Vector2 baseSize = SizeOrFallback(skinPreviewImageSize, new Vector2(460f, 530f));
        if (PlayerSkinProfile.NormalizeSkinIndex(skinIndex) != 0)
            return baseSize;

        return baseSize * Mathf.Max(0.1f, defaultLobbySkinScale);
    }

    private Vector2 GetSkinPreviewImagePosition(int skinIndex)
    {
        if (PlayerSkinProfile.NormalizeSkinIndex(skinIndex) != 0)
            return skinPreviewImagePosition;

        return skinPreviewImagePosition + defaultLobbySkinPositionOffset;
    }

    private Sprite LoadSelectedSkinSprite(int selectedSkinIndex)
    {
        Sprite lobbySprite = GetLobbySkinSprite(selectedSkinIndex);
        if (lobbySprite != null)
            return lobbySprite;

        return LoadGameplaySkinSprite(selectedSkinIndex);
    }

    private Sprite GetLobbySkinSprite(int skinIndex)
    {
        switch (skinIndex)
        {
            case 1:
                return mateoLobbySprite;
            case 2:
                return ericLobbySprite;
            case 3:
                return ignasiLobbySprite;
            default:
                return defaultLobbySprite;
        }
    }

    private Sprite LoadGameplaySkinSprite(int skinIndex)
    {
        GameObject playerPrefab = Resources.Load<GameObject>(LanPlayerPreviewResource);
        if (playerPrefab == null)
            return null;

        PlayerSkinApplier skinApplier = playerPrefab.GetComponent<PlayerSkinApplier>();
        if (skinApplier == null)
            return null;

        return skinApplier.GetSkinSprite(skinIndex);
    }

    private void ConfigureStatsText(TMP_Text text, float y, float fontSize, float width, float height)
    {
        if (text == null)
            return;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, y);
        rectTransform.sizeDelta = new Vector2(width, height);

        text.enableAutoSizing = false;
        text.fontSize = fontSize > 0f ? fontSize : 20f;
        text.alignment = TextAlignmentOptions.Center;
    }

    private RectTransform FindRectByName(string objectName)
    {
        foreach (RectTransform rectTransform in FindObjectsOfType<RectTransform>(true))
        {
            if (rectTransform.name == objectName)
                return rectTransform;
        }

        return null;
    }

    private TMP_Text FindTextByName(string objectName)
    {
        foreach (TMP_Text text in FindObjectsOfType<TMP_Text>(true))
        {
            if (text.name == objectName)
                return text;
        }

        return null;
    }

    private Vector2 SizeOrFallback(Vector2 value, Vector2 fallback)
    {
        return value.x <= 0f || value.y <= 0f ? fallback : value;
    }
}
