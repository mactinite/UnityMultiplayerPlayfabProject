using Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    public float damage = 100;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ITakeDamage>(out var damageReceiver))
        {
            damageReceiver.Damage(damage);
        }
    }
}
