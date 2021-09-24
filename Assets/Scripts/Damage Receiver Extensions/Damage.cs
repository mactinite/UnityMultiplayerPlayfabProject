using mactinite.EDS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Serialization;
using MLAPI;

public class Damage : DamageBase, INetworkSerializable
{
    public ulong sourceClientid;
    public Damage()
    {
        sourceClientid = NetworkManager.Singleton.LocalClientId;
    }

    public Damage(float amount) : base(amount)
    {
        sourceClientid = NetworkManager.Singleton.LocalClientId;
    }

    public void NetworkSerialize(NetworkSerializer serializer)
    {
        serializer.Serialize(ref amount);
        serializer.Serialize(ref newHealth);
        serializer.Serialize(ref sourceClientid);
    }
}
