using UnityEngine;

public class HealOnKillEffect : MonoBehaviour
{
    [SerializeField] private int healAmount = 5;
    private Health hp;

    private void Awake()
    {
        hp = GetComponent<Health>();
        enabled = false;
    }

    public void SetHealAmount(int amount)
    {
        healAmount = Mathf.Max(0, amount);
    }

    private void OnEnable()
    {
        EnemyController.OnAnyEnemyKilled += HandleKill;
    }

    private void OnDisable()
    {
        EnemyController.OnAnyEnemyKilled -= HandleKill;
    }

    private void HandleKill(EnemyController.EnemyType type)
    {
        if (hp == null) return;
        hp.Heal(healAmount);
    }
}
