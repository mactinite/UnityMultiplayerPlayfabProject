using Unity;
using UnityEngine;
using System.Collections.Generic;
using MLAPI.Serialization;

[System.Serializable]
public class LobbyPlayer : INetworkSerializable
{
    public string userName;
    public ulong ClientId;
    public bool isReady;

    void INetworkSerializable.NetworkSerialize(NetworkSerializer serializer)
    {
        serializer.Serialize(ref ClientId);
        serializer.Serialize(ref isReady);
        serializer.Serialize(ref userName);
    }
}
