using UnityEngine;
using UnityEngine.Events;

namespace EndlessZ.Combat
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath = false;
        [SerializeField] private UnityEvent<float, float> healthChanged = new UnityEvent<float, float>();
        [SerializeField] private UnityEvent died = new UnityEvent();

        private float currentHealth;

        public event System.Action<GameObject> Died;

        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
            healthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float amount, GameObject instigator)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            healthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Died?.Invoke(instigator);
                died?.Invoke();

                if (destroyOnDeath)
                {
                    Destroy(gameObject);
                }
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            healthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxHealthMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
            {
                return;
            }

            float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 1f;
            maxHealth = Mathf.Max(1f, maxHealth * multiplier);
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, IsAlive ? 1f : 0f, maxHealth);
            healthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
