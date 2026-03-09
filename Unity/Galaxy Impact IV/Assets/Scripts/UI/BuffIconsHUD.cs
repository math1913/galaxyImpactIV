using UnityEngine;
using UnityEngine.UI;

public class BuffIconsHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BuffManager buffManager;
    [SerializeField] private Image[] slots; // tamaño 3
    [SerializeField] private Image[] radials; // tamaño 3, Image Filled Radial360

    [SerializeField] private CanvasGroup canvasGroup; // opcional

    [Header("Behavior")]
    [SerializeField] private bool hideWhenEmpty = true;

    private void Awake()
    {
        // Si no lo asignaste, lo buscamos en el Player
        if (buffManager == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                buffManager = player.GetComponent<BuffManager>();
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        Refresh();
    }

    private void Update()
    {
        // Es barato (máx 3). Si luego querés, lo hacemos event-driven.
        Refresh();
    }

    private void Refresh()
    {
        if (buffManager == null || slots == null || slots.Length == 0)
            return;

        var buffs = buffManager.Active;
        int count = buffs.Count;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < count && buffs[i] != null && buffs[i].icon != null)
            {
                slots[i].sprite = buffs[i].icon;
                slots[i].enabled = true;

                // Radial
                if (radials != null && i < radials.Length && radials[i] != null)
                {
                    float total = Mathf.Max(0.0001f, buffs[i].total);
                    float fill = Mathf.Clamp01(buffs[i].remaining / total);

                    radials[i].enabled = true;
                    radials[i].fillAmount = fill;
                }
            }
            else
            {
                slots[i].sprite = null;
                slots[i].enabled = false;

                if (radials != null && i < radials.Length && radials[i] != null)
                {
                    radials[i].fillAmount = 0f;
                    radials[i].enabled = false;
                }
            }
        }
        if (hideWhenEmpty)
            SetVisible(count > 0);
    }

    private void SetVisible(bool visible)
    {
        // Evitamos desactivar el GameObject para que el script siga actualizando.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
        else
        {
            // fallback: activa/desactiva imágenes
            for (int i = 0; i < slots.Length; i++)
                slots[i].enabled = visible && slots[i].enabled;
        }
    }
}
