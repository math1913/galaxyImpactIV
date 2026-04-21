using UnityEngine;

public static class PlayerSkinProfile
{
    public const int SkinCount = 4;

    private const int DefaultSkinIndex = 0;
    private const string GlobalSkinKey = "selectedSkinIndex";
    private const string UserSkinKeyPrefix = "selectedSkinIndex.user.";

    public static int GetSelectedSkinIndex()
    {
        string profileKey = GetProfileKey();
        int selected = PlayerPrefs.GetInt(profileKey, DefaultSkinIndex);
        return NormalizeSkinIndex(selected);
    }

    public static void SetSelectedSkinIndex(int skinIndex)
    {
        int normalized = NormalizeSkinIndex(skinIndex);
        PlayerPrefs.SetInt(GetProfileKey(), normalized);
        PlayerPrefs.Save();
    }

    public static int NormalizeSkinIndex(int skinIndex)
    {
        if (skinIndex < 0 || skinIndex >= SkinCount)
            return DefaultSkinIndex;

        return skinIndex;
    }

    private static string GetProfileKey()
    {
        int userId = PlayerPrefs.GetInt("userId", -1);
        return userId >= 0 ? $"{UserSkinKeyPrefix}{userId}" : GlobalSkinKey;
    }
}
