using UnityEngine;

public class PickupXP : PickupBase
{
    [SerializeField] private int xpAmount = 20;

    protected override void OnPickup(Collider2D player)
    {
        if (GameStatsManager.Instance != null)
        {
            GameStatsManager.Instance.AddXP(xpAmount);
        }
        else
        {
            Debug.LogWarning("PickupXP: No se encontró GameStatsManager.Instance.");
        }
    }
}
