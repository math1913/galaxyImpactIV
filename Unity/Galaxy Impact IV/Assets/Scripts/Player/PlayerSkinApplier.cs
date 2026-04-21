using UnityEngine;

public class PlayerSkinApplier : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite[] skinSprites;
    [SerializeField] private bool applySavedSkinOnStart = true;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (applySavedSkinOnStart)
            ApplySavedSkin();
    }

    public void ApplySavedSkin()
    {
        ApplySkinIndex(PlayerSkinProfile.GetSelectedSkinIndex());
    }

    public void SetApplySavedSkinOnStart(bool apply)
    {
        applySavedSkinOnStart = apply;
    }

    public void ApplySkinIndex(int skinIndex)
    {
        if (targetRenderer == null || skinSprites == null || skinSprites.Length == 0)
            return;

        int normalized = Mathf.Clamp(skinIndex, 0, skinSprites.Length - 1);
        Sprite skin = skinSprites[normalized];

        if (skin != null)
            targetRenderer.sprite = skin;
    }
}
