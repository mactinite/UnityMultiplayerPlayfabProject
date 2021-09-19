using MLAPI;
using MLAPI.Messaging;
using PlayFab;
using PlayFab.ServerModels;
using PlayFab.MultiplayerAgent.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using mactinite.ToolboxCommons;
using MLAPI.Serialization.Pooled;
using System.Text;
using System.IO;
using MLAPI.Serialization;
using MLAPI.SceneManagement;

public class GameServer : SingletonMonobehavior<GameServer>
{
    private NetworkManager netManager;

    public Action<string> OnPlayerAdded;
    public Action<string> OnPlayerRemoved;

    public int maxConnections = 100;
    [Scene]
    public string lobbyScene;
    public Dictionary<ulong, PlayerNetworkConnection> Connections
    {
        get { return _connections; }
        private set { _connections = value; }
    }
    private Dictionary<ulong, PlayerNetworkConnection> _connections;


    void Start()
    {
        netManager = GetComponentInParent<NetworkManager>();
    }

    internal void StartServer()
    {
        netManager.StartServer();
        netManager.OnClientConnectedCallback += ClientConnected;
        netManager.OnClientDisconnectCallback += ClientDisconnect;
        netManager.NetworkConfig.ConnectionApproval = true;
        netManager.ConnectionApprovalCallback += ApproveConnection;
        _connections = new Dictionary<ulong, PlayerNetworkConnection>();
        // not sure what this will do.
        NetworkSceneManager.SwitchScene(lobbyScene);

    }

    private void ApproveConnection(byte[] data, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        Debug.Log("Start connection approval check");
        string PlayfabID = Encoding.ASCII.GetString(data);
        Debug.Log(PlayfabID);

        PlayFabServerAPI.GetUserAccountInfo(new GetUserAccountInfoRequest
        {
            PlayFabId = PlayfabID,
        },
        success =>
        {
            bool approve = true;
            bool createPlayerObject = true;
            PlayerNetworkConnection connection = new PlayerNetworkConnection
            {
                IsAuthenticated = true,
                ClientId = clientId,
                PlayFabId = PlayfabID,
                LobbyId = PlayFabMultiplayerAgentAPI.SessionConfig.SessionId,
                UserName = success.UserInfo.Username
            };

            if (_connections.ContainsKey(clientId))
            {
                _connections[clientId] = connection;
            }
            else
            {
                _connections.Add(clientId, connection);
            }

            Debug.Log("End connection approval check");
            Debug.Log("Result: ACCEPTED");
            Debug.Log($"Welcome {success.UserInfo.Username}");



            //If approve is true, the connection gets added. If it's false. The client gets disconnected
            callback(createPlayerObject, null, approve, GetSpawnPoint(), Quaternion.identity);

            ServerLog.Log($"{success.UserInfo.Username} has joined.");
        },
        fail =>
        {
            bool approve = false;
            bool createPlayerObject = false;
            callback(createPlayerObject, null, approve, Vector3.zero, Quaternion.identity);
            Debug.Log("End connection approval");
            Debug.Log("Result: DENIED");
        });;
    }

    private Vector3 GetSpawnPoint()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints.Length > 0)
        {
            GameObject spawnPointObject = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            return spawnPointObject.transform.position;
        } else
        {
            return Vector3.zero;
        }
    }

        private void ClientDisconnect(ulong ClientId)
    {
        // remove player data from connections dictionary
        if (_connections.ContainsKey(ClientId))
        {
            ServerLog.Log($"{_connections[ClientId].UserName} has left.");
            _connections.Remove(ClientId);
        }
    }

    private void ClientConnected(ulong ClientId)
    {
        Debug.Log($"Client Connected : {ClientId}");
    }

}


[Serializable]
public class PlayerNetworkConnection
{
    public bool IsAuthenticated;
    public string PlayFabId;
    public string LobbyId;
    public string UserName;
    public ulong ClientId;
}

