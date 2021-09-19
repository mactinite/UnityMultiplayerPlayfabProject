using UnityEngine;

namespace Game
{
    interface ITakeDamage
    {
        public void Damage(float damage, int sourceClient = -1);
    }
}