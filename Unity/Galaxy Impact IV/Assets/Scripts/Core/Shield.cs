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

    /// AÃ±adir escudo
    public void AddShield(int amount)
    {
        if (LanRuntime.IsActive && !LanRuntime.IsServer) return;
        currentShield = Mathf.Clamp(currentShield + amount, 0, maxShield);
        OnShieldChanged.Invoke(currentShield, maxShield);
    }

    /// Quitar escudo (cuando recibes daÃ±o)
    public int AbsorbDamage(int dmg)
    {
        if (LanRuntime.IsActive && !LanRuntime.IsServer) return 0;

        int absorbed = Mathf.Min(currentShield, dmg);
        currentShield -= absorbed;

        OnShieldChanged.Invoke(currentShield, maxShield);
        return absorbed; // cantidad de daÃ±o mitigado
    }

    public void ApplyStateFromNetwork(int currentValue, int maxValue)
    {
        maxShield = Mathf.Max(0, maxValue);
        currentShield = Mathf.Clamp(currentValue, 0, maxShield);
        OnShieldChanged.Invoke(currentShield, maxShield);
    }
}
