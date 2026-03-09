using System;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public const int MaxBuffs = 3;

    [Serializable]
    public class ActiveBuff
    {
        public string id;
        public float remaining;
        public Sprite icon;
        public float total;

        private readonly Action onRemove;

        public ActiveBuff(string id, float duration, Action onRemove, Sprite icon)
        {
            this.id = id;
            total = duration;
            remaining = duration;
            this.onRemove = onRemove;
            this.icon = icon;
        }

        public void Refresh(float duration) 
        {
            total = duration;
            remaining = duration;
        }

        public void Tick(float dt) => remaining -= dt;

        public void Remove() => onRemove?.Invoke();
    }

    private readonly List<ActiveBuff> active = new();
    public IReadOnlyList<ActiveBuff> Active => active;

    public void AddOrRefresh(string id, float duration, Action onApply, Action onRemove, Sprite icon = null)
    {
        // Si existe -> refresca duración
        var existing = active.Find(b => b.id == id);
        if (existing != null)
        {
            existing.Refresh(duration);
            return;
        }

        // Si está lleno -> remover el que expira antes
        if (active.Count >= MaxBuffs)
            RemoveSoonest();

        var buff = new ActiveBuff(id, duration, onRemove, icon);
        active.Add(buff);
        onApply?.Invoke();
    }

    private void RemoveSoonest()
    {
        int idx = 0;
        for (int i = 1; i < active.Count; i++)
        {
            if (active[i].remaining < active[idx].remaining)
                idx = i;
        }

        active[idx].Remove();
        active.RemoveAt(idx);
    }

    private void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            active[i].Tick(Time.deltaTime);
            if (active[i].remaining <= 0f)
            {
                active[i].Remove();
                active.RemoveAt(i);
            }
        }
    }
}
