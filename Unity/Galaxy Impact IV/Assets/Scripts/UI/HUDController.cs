using UnityEngine;
using TMPro;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("Referencias del jugador")]
    [Tooltip("Componente Health del jugador.")]
    [SerializeField] private Health playerHealth;

    [Tooltip("Componente Weapon del jugador.")]
    [SerializeField] private Weapon playerWeapon;

    [Tooltip("Componente Shield del jugador.")]
    [SerializeField] private Shield playerShield;

    [Header("Referencias del Wave Manager")]
    [Tooltip("Script WaveManager para mostrar la oleada actual.")]
    [SerializeField] private WaveManager waveManager;

    [Header("UI de Vida")]
    [SerializeField] private UnityEngine.UI.Slider healthBar;
    [SerializeField] private TMP_Text healthText;

    [Header("UI de Escudo")]
    [SerializeField] private UnityEngine.UI.Slider shieldBar;
    [SerializeField] private TMP_Text shieldText;

    [Header("Elementos de UI")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text waveText;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private Color healthFlashColor = Color.red;
    [SerializeField] private Color shieldFlashColor = Color.cyan;

    private Color healthOriginalColor;
    private Color shieldOriginalColor;

    private int lastShieldValue;

    // último valor de vida para diferenciar daño vs cura
    private int lastHealthValue;

    private void Start()
    {
        if (waveManager)
            waveManager.OnWaveStarted.AddListener(UpdateWaveText);

        if (healthBar)
            healthOriginalColor = healthBar.fillRect.GetComponent<UnityEngine.UI.Image>().color;

        if (shieldBar)
            shieldOriginalColor = shieldBar.fillRect.GetComponent<UnityEngine.UI.Image>().color;

        if (playerHealth)
        {
            playerHealth.OnHealthChanged.AddListener(OnHealthChanged);

            // Render inicial sin flashear (porque lastHealthValue no estaba seteado)
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);

            // Inicializar el último valor después del primer render
            lastHealthValue = playerHealth.CurrentHealth;
        }

        if (playerShield)
        {
            playerShield.OnShieldChanged.AddListener(OnShieldChanged);
            OnShieldChanged(playerShield.CurrentShield, playerShield.MaxShield);
            lastShieldValue = playerShield.CurrentShield;
        }

        if (playerWeapon)
        {
            playerWeapon.OnAmmoChanged.AddListener(UpdateAmmo);
            playerWeapon.OnTotalAmmoChanged.AddListener(UpdateAmmo);
            UpdateAmmo(playerWeapon.CurrentAmmo);
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        if (healthText)
            healthText.text = $"{current} | {max}";

        if (healthBar)
            healthBar.value = max > 0 ? (float)current / max : 0f;

        //flashear solo si bajó respecto al valor anterior
        if (current < lastHealthValue)
            StartCoroutine(FlashBar(healthBar, healthFlashColor, healthOriginalColor));

        // actualizar el valor anterior
        lastHealthValue = current;
    }

    private void OnShieldChanged(int current, int max)
    {
        if (shieldText)
            shieldText.text = $"{current} |  {max}";

        if (shieldBar)
            shieldBar.value = max > 0 ? (float)current / max : 0f;

        // solo si el escudo bajo
        if (current < lastShieldValue)
            StartCoroutine(FlashBar(shieldBar, shieldFlashColor, shieldOriginalColor));

        // actualizar el valor anterior
        lastShieldValue = current;
    }

    private void UpdateAmmo(int _)
    {
        if (!ammoText) return;

        int current = playerWeapon.CurrentAmmo;

        ammoText.text = playerWeapon.IsReloading
            ? $"{current}\nReloading..."
            : $"{current}";
    }

    private void UpdateWaveText(int wave)
    {
        if (!waveText) return;
        waveText.text = $"ROUND {wave}";
    }

    private IEnumerator FlashBar(UnityEngine.UI.Slider bar, Color flashColor, Color originalColor)
    {
        if (bar == null || bar.fillRect == null) yield break;

        var image = bar.fillRect.GetComponent<UnityEngine.UI.Image>();
        if (image == null) yield break;

        image.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        image.color = originalColor;
    }
}
