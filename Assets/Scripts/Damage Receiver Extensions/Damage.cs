using System;
using mactinite.EDS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Damage : DamageBase, INetworkSerializable, IEquatable<Damage>
{
    public ulong sourceClientid;
    public float timestamp;
    public Damage()
    {
        sourceClientid = NetworkManager.Singleton.LocalClientId;
        timestamp = Time.time;
    }

    public Damage(float amount) : base(amount)
    {
        sourceClientid = NetworkManager.Singleton.LocalClientId;
        timestamp = Time.time;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref amount);
        serializer.SerializeValue(ref newHealth);
        serializer.SerializeValue(ref sourceClientid);
        serializer.SerializeValue(ref timestamp);
    }

    public bool Equals(Damage other)
    {
        if (other == null) return false;
        
        return timestamp.Equals(other.timestamp) && sourceClientid.Equals(other.sourceClientid);
    }
}
