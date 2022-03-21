using System;
using Unity;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

[System.Serializable]
public class LobbyPlayer : INetworkSerializable, IEquatable<LobbyPlayer>
{
    public FixedString32Bytes userName;
    public ulong ClientId;
    public bool isReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref isReady);
        serializer.SerializeValue(ref userName);
    }

    public bool Equals(LobbyPlayer other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return userName.Equals(other.userName) && ClientId == other.ClientId && isReady == other.isReady;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((LobbyPlayer) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(userName, ClientId, isReady);
    }
}
