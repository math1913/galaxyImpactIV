using UnityEngine;
using UnityEngine.Events;

public class Shield : MonoBehaviour
{
    [Header("Shield Settings")]
    [SerializeField] private int maxShield = 50;
    [SerializeField] private int currentShield = 0;

    // Evento igual que en Health
    public UnityEvent<int, int> OnShieldChanged = new UnityEvent<int, int>();

    public int CurrentShield => currentShield;
    public int MaxShield => maxShield;

    /// Añadir escudo
    public void AddShield(int amount)
    {
        currentShield = Mathf.Clamp(currentShield + amount, 0, maxShield);
        OnShieldChanged.Invoke(currentShield, maxShield);
    }

    /// Quitar escudo (cuando recibes daño)
    public int AbsorbDamage(int dmg)
    {
        int absorbed = Mathf.Min(currentShield, dmg);
        currentShield -= absorbed;

        OnShieldChanged.Invoke(currentShield, maxShield);
        return absorbed; // cantidad de daño mitigado
    }
}
