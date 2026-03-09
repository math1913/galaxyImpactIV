using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AchievementMenu : MonoBehaviour
{
    [Header("Referencias")]
    private AchievementAPIClient api => AchievementAPIClient.Instance;

    public Transform contentPanel;        
    public GameObject logroPrefab;

    [Header("Usuario")]
    public long userId;

    [Header("Layout")]
    public float itemHeight = 140f;  
    public float itemSpacing = 15f;

    private List<AchievementAPIClient.AchievementDTO> achievements;

    private async void Start()
    {
        userId = PlayerPrefs.GetInt("userId", 1);

        // esperar a que el Singleton esté listo:
        while (AchievementAPIClient.Instance == null)
            await Task.Yield();

        await LoadAchievements();
    }

    public async Task LoadAchievements()
    {
        var apiClient = AchievementAPIClient.Instance;

        if (apiClient == null)
        {
            Debug.LogError("AchievementAPIClient.Instance es NULL. ¿Está en LoginScene con DontDestroyOnLoad?");
            return;
        }

        achievements = await apiClient.GetAchievements(userId);

        if (achievements == null)
        {
            Debug.LogError("No se pudieron cargar los logros.");
            return;
        }

        // Limpiar anteriores
        foreach (Transform child in contentPanel)
            Destroy(child.gameObject);

        // Instanciar ítems
        foreach (var logro in achievements)
        {
            GameObject item = Instantiate(logroPrefab, contentPanel);
            var ui = item.GetComponent<AchievementUIItem>();
            ui.SetData(logro);

            RectTransform rt = item.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
        }

        // Ajustar tamaño del content
        RectTransform contentRt = contentPanel.GetComponent<RectTransform>();
        int count = achievements.Count;

        float totalHeight = count * itemHeight + (count - 1) * itemSpacing;

        contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, totalHeight);

        Debug.Log($"Se cargaron {achievements.Count} logros. Altura content={totalHeight}");
    }
}
