using UnityEngine;
using UnityEngine.UI;

public class DashHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DashChargesEffect dash;
    [SerializeField] private Image icon;
    [SerializeField] private Image[] bars; // tamaÃ±o 3
    [SerializeField] private bool hideWhenFullAndNoPickupColorChange = false; // opcional

    private void Awake()
    {
        if (dash == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) dash = player.GetComponent<DashChargesEffect>();
        }
    }

    private void Update()
    {
        if (dash == null || bars == null || bars.Length < 3) return;

        int charges = dash.Charges;

        for (int i = 0; i < 3; i++)
        {
            bool filled = i < charges;
            bars[i].color = filled ? dash.ActiveColor : dash.EmptyColor;
        }

        // Si querÃ©s ocultarlo cuando no se usa, podÃ©s manejarlo acÃ¡.
        // Por defecto lo dejamos siempre visible.
    }

    public void BindPlayer(DashChargesEffect effect)
    {
        dash = effect;
    }
}
