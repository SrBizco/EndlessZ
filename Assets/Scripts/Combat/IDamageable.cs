using UnityEngine;

namespace EndlessZ.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }

        void TakeDamage(float amount, GameObject instigator);
    }
}
