using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementUIItem : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image bordePanel;     // El panel exterior que se colorea por categoría
    public Image fondoCompleto;  // El fondo que se enciende cuando se completa

    public TextMeshProUGUI title;
    public TextMeshProUGUI descripcion;
    public Slider sliderProgreso;
    public TextMeshProUGUI txtProgreso;

    [Header("Color Default")]
    public Color colorDefault = Color.gray;
    public Color colorCompletado = new Color(1f, 1f, 1f, 0.15f);

    [Header("KILL")]
    public Color killNormal;
    public Color killFast;
    public Color killTank;
    public Color killShooter;
    public Color killTotal;

    [Header("WAVES")]
    public Color wavesTotal;

    [Header("TIME")]
    public Color timeSurvive;

    [Header("SCORE")]
    public Color scoreSingle;

    [Header("PICKUP")]
    public Color pickupHealth;
    public Color pickupShield;
    public Color pickupAmmo;
    public Color pickupExp;

    [Header("LEVEL")]
    public Color levelReach;

    public void SetData(AchievementAPIClient.AchievementDTO dto)
    {
        title.text = dto.nombre;
        descripcion.text = dto.descripcion;

        sliderProgreso.maxValue = dto.objetivo;
        sliderProgreso.value = Mathf.Min(dto.progresoActual, dto.objetivo);

        txtProgreso.text = Mathf.Min(dto.progresoActual, dto.objetivo)
                        + " / " + dto.objetivo;

        // COLOR BASE SEGÚN CATEGORÍA + TIPO
        Color baseColor = GetColorPorLogro(dto);

        //EL BORDE SIEMPRE TIENE EL COLOR BASE
        bordePanel.color = baseColor;

        // Mismo color, pero suavizado
        Color fondo = baseColor;
        float progreso = Mathf.Clamp01(dto.progresoActual / (float)dto.objetivo); //porcentaje de progeso (de 0 a 1)
        fondo.a = progreso; // transparencia del fondo al completar
        fondoCompleto.color = fondo;

        Debug.Log($"[LOGRO] {dto.codigo} — Categoria: {dto.categoria}, Tipo: {dto.tipo}");
    }


    private Color GetColorPorLogro(AchievementAPIClient.AchievementDTO dto)
    {
        switch (dto.categoria)
        {
            case "KILL":
                switch (dto.tipo)
                {
                    case "NORMAL": return killNormal;
                    case "FAST": return killFast;
                    case "TANK": return killTank;
                    case "SHOOTER": return killShooter;
                    case "TOTAL": return killTotal;
                }
                break;

            case "PICKUP":
                switch (dto.tipo)
                {
                    case "HEALTH": return pickupHealth;
                    case "SHIELD": return pickupShield;
                    case "AMMO": return pickupAmmo;
                    case "EXP": return pickupExp;
                }
                break;

            case "WAVES":
                if (dto.tipo == "TOTAL") return wavesTotal;
                break;

            case "TIME":
                if (dto.tipo == "SURVIVE") return timeSurvive;
                break;

            case "SCORE":
                if (dto.tipo == "SINGLE") return scoreSingle;
                break;

            case "LEVEL":
                if (dto.tipo == "REACH") return levelReach;
                break;
        }

        return colorDefault;
    }
}
