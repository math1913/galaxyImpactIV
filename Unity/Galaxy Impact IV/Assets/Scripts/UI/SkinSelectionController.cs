using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SkinSelectionController : MonoBehaviour
{
    private static readonly string[] SkinNames =
    {
        "Default",
        "Oriental Salvaje",
        "P\u00edcaro de playa",
        "Tit\u00e1n de la pampa"
    };

    private static readonly Color SelectedGold = new Color(1f, 0.72f, 0.16f, 1f);

    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Transform optionsRoot;

    private readonly List<SkinOptionView> options = new List<SkinOptionView>();
    private int selectedIndex;
    private TMP_Text selectedText;
    private Button confirmButton;

    private void Awake()
    {
        if (optionsRoot == null)
            optionsRoot = transform;

        selectedIndex = PlayerSkinProfile.GetSelectedSkinIndex();
        BuildScreen();
        RefreshSelection();
    }

    private void BuildScreen()
    {
        BuildHeader();
        BuildOptions();
        if (options.Count > 0)
            selectedIndex = Mathf.Clamp(selectedIndex, 0, options.Count - 1);
        BuildFooter();
    }

    private void BuildHeader()
    {
        RectTransform header = CreateUiRect("SkinHeader", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(760f, 120f), new Vector2(0f, -95f));
        TMP_Text title = CreateText("SkinTitle", header, "SELECT SKIN", 58, FontStyles.Bold);
        title.color = Color.white;

        selectedText = CreateText("SelectedSkinText", header, string.Empty, 24, FontStyles.Normal);
        RectTransform selectedRect = selectedText.rectTransform;
        selectedRect.anchorMin = new Vector2(0f, 0f);
        selectedRect.anchorMax = new Vector2(1f, 0f);
        selectedRect.anchoredPosition = new Vector2(0f, 10f);
        selectedRect.sizeDelta = new Vector2(0f, 34f);
        selectedText.color = SelectedGold;
    }

    private void BuildOptions()
    {
        Image[] skinImages = optionsRoot.GetComponentsInChildren<Image>(true);
        List<Image> selectableImages = new List<Image>();

        foreach (Image image in skinImages)
        {
            if (image == null || image.sprite == null)
                continue;

            if (image.gameObject.name.StartsWith("Skin"))
                selectableImages.Add(image);
        }

        selectableImages.Sort((a, b) =>
        {
            int positionCompare = GetSkinSortX(a).CompareTo(GetSkinSortX(b));
            return positionCompare != 0 ? positionCompare : string.CompareOrdinal(a.gameObject.name, b.gameObject.name);
        });

        for (int i = 0; i < selectableImages.Count; i++)
            ConfigureOption(selectableImages[i], i);
    }

    private void ConfigureOption(Image skinImage, int index)
    {
        RectTransform card = skinImage.transform.parent as RectTransform;
        if (card == null || card == transform)
            card = skinImage.rectTransform;

        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(300f, 420f);
        card.anchoredPosition = new Vector2((index - 1.5f) * 340f, -20f);

        Image cardImage = card.GetComponent<Image>();
        if (cardImage == null)
            cardImage = card.gameObject.AddComponent<Image>();

        cardImage.color = new Color(0.06f, 0.08f, 0.12f, 0.78f);
        cardImage.raycastTarget = true;

        Outline outline = card.GetComponent<Outline>();
        if (outline == null)
            outline = card.gameObject.AddComponent<Outline>();

        outline.effectDistance = new Vector2(4f, -4f);

        Button button = card.GetComponent<Button>();
        if (button == null)
            button = card.gameObject.AddComponent<Button>();

        button.targetGraphic = cardImage;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        // Cambiado de verde a lila FF4BFC
        colors.highlightedColor = new Color(1f, 0.294f, 0.988f, 1f); 
        colors.pressedColor = new Color(0.8f, 0.235f, 0.792f, 1f);
        colors.selectedColor = new Color(1f, 0.294f, 0.988f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.RemoveAllListeners();

        int capturedIndex = index;
        button.onClick.AddListener(() => SelectSkin(capturedIndex));

        SkinSelectionOption hover = card.GetComponent<SkinSelectionOption>();
        if (hover == null)
            hover = card.gameObject.AddComponent<SkinSelectionOption>();

        hover.Initialize(this, capturedIndex);

        skinImage.raycastTarget = false;
        skinImage.preserveAspect = true;
        RectTransform skinRect = skinImage.rectTransform;
        skinRect.anchorMin = new Vector2(0.5f, 0.5f);
        skinRect.anchorMax = new Vector2(0.5f, 0.5f);
        skinRect.pivot = new Vector2(0.5f, 0.5f);
        skinRect.anchoredPosition = new Vector2(0f, 8f);
        skinRect.sizeDelta = new Vector2(220f, 300f);

        TMP_Text label = card.Find("SkinLabel")?.GetComponent<TMP_Text>();
        if (label == null)
            label = CreateText("SkinLabel", card, GetSkinName(index), 22, FontStyles.Bold);

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 36f);
        labelRect.sizeDelta = new Vector2(0f, 42f);
        label.text = GetSkinName(index);
        label.enableWordWrapping = true;
        label.color = new Color(0.92f, 0.94f, 0.98f, 1f);

        options.Add(new SkinOptionView(card, cardImage, outline, label));
    }

    private void BuildFooter()
    {
        RectTransform footer = CreateUiRect("SkinFooter", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(620f, 92f), new Vector2(0f, 86f));
        HorizontalLayoutGroup layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;
        layout.spacing = 18f;

        confirmButton = CreateButton("ConfirmSkinButton", footer, "Use Skin");
        confirmButton.onClick.AddListener(ConfirmAndReturn);

        Button backButton = CreateButton("BackToMenuButton", footer, "Back");
        backButton.onClick.AddListener(ReturnToMenu);
    }

    public void SelectSkin(int skinIndex)
    {
        selectedIndex = PlayerSkinProfile.NormalizeSkinIndex(skinIndex);
        PlayerSkinProfile.SetSelectedSkinIndex(selectedIndex);
        RefreshSelection();
    }

    public void SetHover(int skinIndex, bool isHovering)
    {
        if (skinIndex < 0 || skinIndex >= options.Count || skinIndex == selectedIndex)
            return;

        options[skinIndex].Card.localScale = isHovering ? Vector3.one * 1.04f : Vector3.one;
    }

    private void ConfirmAndReturn()
    {
        PlayerSkinProfile.SetSelectedSkinIndex(selectedIndex);
        ReturnToMenu();
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < options.Count; i++)
        {
            bool isSelected = i == selectedIndex;
            SkinOptionView option = options[i];

            option.Card.localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;
            option.CardImage.color = isSelected
                ? new Color(0.22f, 0.14f, 0.03f, 0.9f)
                : new Color(0.06f, 0.08f, 0.12f, 0.78f);
            option.Outline.effectColor = isSelected
                ? SelectedGold
                // Cambiado de verde suave a lila suave FF4BFC
                : new Color(1f, 0.294f, 0.988f, 0.35f); 
            option.Label.color = isSelected
                ? SelectedGold
                : new Color(0.92f, 0.94f, 0.98f, 1f);
        }

        if (selectedText != null)
            selectedText.text = $"Seleccionado: {GetSkinName(selectedIndex)}";
    }

    private static float GetSkinSortX(Image image)
    {
        return image != null ? image.rectTransform.position.x : float.MaxValue;
    }

    private static string GetSkinName(int skinIndex)
    {
        return skinIndex >= 0 && skinIndex < SkinNames.Length
            ? SkinNames[skinIndex]
            : $"Skin {skinIndex + 1}";
    }

    private static RectTransform CreateUiRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0f, 64f);

        Image image = go.GetComponent<Image>();
        // Cambiado de verde a lila FF4BFC
        image.color = new Color(1f, 0.294f, 0.988f, 0.92f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text label = CreateText("Text", go.transform, text, 26, FontStyles.Bold);
        label.color = new Color(0.04f, 0.06f, 0.05f, 1f);
        return button;
    }

    private class SkinOptionView
    {
        public RectTransform Card;
        public Image CardImage;
        public Outline Outline;
        public TMP_Text Label;

        public SkinOptionView(RectTransform card, Image cardImage, Outline outline, TMP_Text label)
        {
            Card = card;
            CardImage = cardImage;
            Outline = outline;
            Label = label;
        }
    }
}