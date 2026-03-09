using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Diagnostics;

/// Componente genérico de vida, con eventos para UI o efectos.
public class Health : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool destroyOnDeath = false;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged; // (current, max)
    public UnityEvent OnDeath;
    public UnityEvent<int> OnDamage; // daño recibido

    private bool _isDead;
    private bool _invulnerable = false;
    public bool IsInvulnerable => _invulnerable;

    private Shield shield; // referencia si el jugador tiene escudo
    [Header("Damage Feedback")]
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.12f;

    // Si queda vacío, auto-detecta
    [SerializeField] private SpriteRenderer[] flashRenderers;

    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip playerDamageSound;
    [SerializeField] private AudioClip enemyDamageSound;
    [SerializeField, Range(0f, 1f)] private float damageSoundVolume = 1f;

    // para distinguir jugador/enemigo
    [SerializeField] private bool autoDetectPlayerByTag = true;
    [SerializeField] private bool isPlayer = false;

    // Interno
    private Color[] _originalColors;
    private Coroutine _flashRoutine;

    [Header("Death Audio")]
    [SerializeField] private AudioClip deathSound;            // si lo asignás, tiene prioridad
    [SerializeField] private AudioClip playerDeathSound;      // fallback si deathSound == null
    [SerializeField] private AudioClip enemyDeathSound;       // fallback si deathSound == null
    [SerializeField, Range(0f, 1f)] private float deathSoundVolume = 1f;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        shield = GetComponent<Shield>();
        if (autoDetectPlayerByTag)
            isPlayer = CompareTag("Player");

        if (flashRenderers == null || flashRenderers.Length == 0)
            flashRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (flashRenderers != null && flashRenderers.Length > 0)
        {
            _originalColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
                _originalColors[i] = flashRenderers[i] != null ? flashRenderers[i].color : Color.white;
        }
    }

    public void SetMaxHealth(int newMaxHealth, bool resetCurrent = true)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        if (resetCurrent)
            CurrentHealth = maxHealth;

        _isDead = false;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        if (_isDead) return;
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (_isDead || _invulnerable || amount <= 0) return;
        TriggerDamageFeedback();
        int remainingDamage = amount;

        //Primero recibe el daño el escudo si existe
        if (shield != null && shield.CurrentShield > 0)
        {
            int absorbed = shield.AbsorbDamage(remainingDamage);
            remainingDamage -= absorbed; // lo que queda pasa a la vida

            if (remainingDamage <= 0)
            {
                OnDamage?.Invoke(amount); // todo el daño fue absorbido
                return; // no pasamos a la vida
            }
        }
        
        //Si todavia queda daño por recibir lo recibe la vida
        CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);

        OnDamage?.Invoke(amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
            Die();
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;
        TriggerDeathFeedback();
        OnDeath?.Invoke();

        if (destroyOnDeath)
            StartCoroutine(DestroyAfterFrame());
    }

    private IEnumerator DestroyAfterFrame()
    {
        yield return null;
        Destroy(gameObject);
    }

    public void ResetHealth()
    {
        _isDead = false;
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
    public void SetInvulnerable(bool value)
    {
        _invulnerable = value;
    }
    private void TriggerDamageFeedback()
    {
        AudioClip clip = damageSound;
        if (clip == null)
            clip = isPlayer ? playerDamageSound : enemyDamageSound;

        if (clip != null && damageSoundVolume > 0f && AudioListener.volume > 0f)
            AudioSource.PlayClipAtPoint(clip, transform.position, damageSoundVolume);
        // Flash
        if (flashOnDamage && flashRenderers != null && flashRenderers.Length > 0)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(DamageFlashCoroutine());
        }
    }

    private IEnumerator DamageFlashCoroutine()
    {
        // Pintar rojo
        for (int i = 0; i < flashRenderers.Length; i++)
            if (flashRenderers[i] != null)
                flashRenderers[i].color = damageFlashColor;

        yield return new WaitForSeconds(damageFlashDuration);

        // Volver al color original
        if (_originalColors != null)
        {
            for (int i = 0; i < flashRenderers.Length; i++)
                if (flashRenderers[i] != null)
                    flashRenderers[i].color = _originalColors[i];
        }

        _flashRoutine = null;
    }
    private void TriggerDeathFeedback()
    {
        AudioClip clip = deathSound;
        if (clip == null)
            clip = isPlayer ? playerDeathSound : enemyDeathSound;

        if (clip != null && deathSoundVolume > 0f && AudioListener.volume > 0f)
            AudioSource.PlayClipAtPoint(clip, transform.position, deathSoundVolume);
    }

}
