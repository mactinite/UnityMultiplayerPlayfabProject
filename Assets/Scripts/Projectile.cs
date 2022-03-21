using Game;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mactinite.ToolboxCommons;

public class Projectile : NetworkBehaviour
{
    Rigidbody rigidBody;
    public float velocity = 5f;
    public float damage = 10f;
    public LayerMask hitMask;

    private void Start()
    {
    }

    public override void OnNetworkSpawn()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.AddForce(transform.forward * velocity, ForceMode.VelocityChange);
        if (!IsOwner)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (IsOwner && collision.gameObject.IsInLayerMask(hitMask))
        {

            if(collision.gameObject.TryGetComponent<Damageable>(out var damageable))
            {
                damageable.Damage(new Damage(damage));
            }

            DestroyProjectileServerRpc();
            // destroy locally as well
            Destroy(gameObject);
        }
        
        // on other clients we destroy the projectile, but just locally
        if(!IsOwner && collision.gameObject.IsInLayerMask(hitMask))
        {
            Destroy(gameObject);
        }
    }

    [ServerRpc]
    void DestroyProjectileServerRpc()
    {
        Debug.Log("Disposing of projectile");
        NetworkObject.Despawn();
    }

}
