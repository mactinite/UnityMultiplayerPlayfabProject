using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class NetworkedDamageReceiver : NetworkBehaviour
{
    Damageable receiver;
    // Sync health in a var so we can use it to initialize clients who connect later
    NetworkVariable<float> health = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone);

    private void Awake()
    {
        receiver = GetComponent<Damageable>();
        if (IsServer)
        {
            health.Value = receiver.health;
            Debug.Log($"{gameObject.name}s health intiialized on server at {receiver.health}.");
        }
    }

    private void Start()
    {
    }

    private void OnEnable()
    {
        receiver.OnDamage += OnDamagedNetworked;

        receiver.OnDestroyed += OnDestroyed;
    }

    private void OnDestroyed(Vector2 location)
    {
        if(NetworkManager.Singleton.LocalClientId == OwnerClientId)
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {

        receiver.OnDamage -= OnDamagedNetworked;
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            receiver.health = health.Value;
            Debug.Log($"{gameObject.name}s health intiialized on client at {receiver.health}.");

            if (health.Value <= 0)
            {
                receiver.destroyed = true;
                // TODO: handle initializing destroyed damageables.
                // I'm thinking they'd be destroyed, or pooled at this point.
            }
        }
    }

    private void OnDamagedNetworked(Vector2 sourcePosition, Damage damage)
    {
        // Remote clients call the server RPC to update daamge
        // insure that client sending RPC is source of damage.
        if (IsClient && damage.sourceClientid == NetworkManager.Singleton.LocalClientId)
        {
            ClientDamageServerRpc(NetworkManager.Singleton.LocalClientId, damage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ClientDamageServerRpc(ulong sourceClient, Damage damage)
    {
        // run damage code on server n
        if (IsServer)
        {
            if (!IsHost)
            {
                // damage on the server, 
                receiver.Damage(damage);
                Debug.Log($"Damage received on server");

            } else
            {
                Debug.Log($"Host relaying damage");
            }
            // and then replicate to all other clients.
            var allOtherClients = NetworkManager.Singleton.ConnectedClientsList.Where(connection => {
                if (connection.ClientId != sourceClient)
                {
                    return connection.ClientId != NetworkManager.Singleton.LocalClientId; // don't want to send it to the server in a host scenario scenarios.
                }
                return false;

            }).ToList();

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = allOtherClients.ConvertAll<ulong>(connection => connection.ClientId).ToArray()
                }
            };

            // Apply damage
            RemoteDamageUpdateClientRpc(damage, clientRpcParams);
            // update health network value
            health.Value = damage.NewHealth;

        }
    }

    [ClientRpc]
    void RemoteDamageUpdateClientRpc(Damage damage, ClientRpcParams parameters = default)
    {
        // ... and then replicate to all clients.
        receiver.Damage(damage);

    }

}
