using System.Collections.Generic;
using UnityEngine;

namespace EndlessZ.Movement
{
    public sealed class MovementSpeedModifierTarget : MonoBehaviour
    {
        private readonly Dictionary<Object, float> modifiers = new Dictionary<Object, float>();

        public float CurrentMultiplier { get; private set; } = 1f;

        public void AddModifier(Object source, float multiplier)
        {
            if (source == null)
            {
                return;
            }

            modifiers[source] = Mathf.Max(0f, multiplier);
            RecalculateMultiplier();
        }

        public void RemoveModifier(Object source)
        {
            if (source == null || !modifiers.Remove(source))
            {
                return;
            }

            RecalculateMultiplier();
        }

        private void RecalculateMultiplier()
        {
            float multiplier = 1f;
            foreach (float modifier in modifiers.Values)
            {
                multiplier *= modifier;
            }

            CurrentMultiplier = multiplier;
        }
    }
}
