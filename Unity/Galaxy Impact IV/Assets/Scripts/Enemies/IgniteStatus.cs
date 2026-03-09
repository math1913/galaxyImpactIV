using UnityEngine;

public class IgniteStatus : MonoBehaviour
{
    private Health hp;
    private int damagePerTick;
    private float tickInterval;
    private float remaining;

    private float tickTimer;

    public void Apply(Health health, int dmgPerTick, float tick, float duration)
    {
        hp = health;
        damagePerTick = Mathf.Max(0, dmgPerTick);
        tickInterval = Mathf.Max(0.05f, tick);
        remaining = Mathf.Max(0f, duration);
        tickTimer = 0f;
        enabled = true;
    }

    private void Update()
    {
        if (hp == null || remaining <= 0f || damagePerTick <= 0)
        {
            Destroy(this);
            return;
        }

        float dt = Time.deltaTime;
        remaining -= dt;
        tickTimer += dt;

        while (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            hp.TakeDamage(damagePerTick);
        }

        if (remaining <= 0f)
            Destroy(this);
    }
}
