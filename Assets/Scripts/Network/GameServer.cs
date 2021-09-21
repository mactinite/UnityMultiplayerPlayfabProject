using MLAPI;
using System;
using System.Collections.Generic;
using UnityEngine;
using mactinite.ToolboxCommons;
using System.Text;
using MLAPI.SceneManagement;

/// <summary>
/// Game Server is the first custom layer on top of network manager. 
/// Provides methods to host multiplayer games either as a dedicated server or client host.
/// It provides extension points to (hopefully) facilitate 3rd party integration (Playfab, Steam, Etc).
/// </summary>
public class GameServer : SingletonMonobehavior<GameServer>
{
    private NetworkManager netManager;

    public Action<string> OnPlayerAdded;
    public Action<string> OnPlayerRemoved;

    public Action<string, PlayerIdentityVerificationDelegate> PlayerIdentityApprovalCallback;

    public string SpawnPointTag = "SpawnPoint";
    public GameObject playerPrefab;

    public GameObject serverLogPrefab;
    private ServerLog loggerInstance;
    [Scene]
    public string lobbyScene;

    [Scene]
    public string gameScene;

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


    public void StopServer()
    {
        netManager.StopServer();
    }


    public void StopHost()
    {
        netManager.StopHost();
    }


    public void StartServer()
    {

        netManager.OnClientConnectedCallback += ClientConnected;
        netManager.OnClientDisconnectCallback += ClientDisconnect;
        netManager.NetworkConfig.ConnectionApproval = true;
        netManager.ConnectionApprovalCallback += ApproveConnection;
        _connections = new Dictionary<ulong, PlayerNetworkConnection>();
        netManager.StartServer();
        SpawnServerLogger();
        var progress = NetworkSceneManager.SwitchScene(gameScene);
        progress.OnClientLoadedScene += OnClientLoaded;
    }

    private void OnClientLoaded(ulong clientId)
    {
        ////spawn the player prefab and give ownership to the client.
        //var go = Instantiate(playerPrefab, GetSpawnPoint(), Quaternion.identity);
        //var netObj = go.GetComponent<NetworkObject>();

        //// we'll dispose of the player objects when we change scenes.
        //netObj.SpawnAsPlayerObject(clientId);

        // Lets try instead to grab each clients player object and move it.

        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.position = GetSpawnPoint();

    }

    void SpawnServerLogger()
    {
        if (netManager.IsServer)
        {
            var go = Instantiate(serverLogPrefab);
            loggerInstance = go.GetComponent<ServerLog>();
            go.GetComponent<NetworkObject>().Spawn(null, false);
        }
    }

    public void StartHost()
    {

        netManager.OnClientConnectedCallback += ClientConnected;
        netManager.OnClientDisconnectCallback += ClientDisconnect;
        netManager.NetworkConfig.ConnectionApproval = true;
        netManager.ConnectionApprovalCallback += ApproveConnection;
        _connections = new Dictionary<ulong, PlayerNetworkConnection>();
        netManager.StartHost();
        SpawnServerLogger();

        if (PlayerIdentityApprovalCallback != null)
        {
            string hostPlayerID = Encoding.ASCII.GetString(netManager.NetworkConfig.ConnectionData);
            PlayerIdentityApprovalCallback(hostPlayerID, (approved, username) =>
            {
                PlayerNetworkConnection connection = new PlayerNetworkConnection
                {
                    IsAuthenticated = true,
                    ClientId = netManager.LocalClientId,
                    PlayerId = hostPlayerID,
                    UserName = username
                };

                Debug.Log("End connection approval check");
                Debug.Log("Result: ACCEPTED");
                Debug.Log($"Welcome {username}");

                ServerLog.Log($"{username} has joined.");
                Connections.Add(netManager.LocalClientId, connection);
            });
        }
        
        var progress = NetworkSceneManager.SwitchScene(gameScene);
        progress.OnClientLoadedScene += OnClientLoaded;
    }



    /// <summary>
    /// Receives the player identity via connection data and calls back for authentication from the 3rd party integration.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="clientId"></param>
    /// <param name="callback"></param>
    private void ApproveConnection(byte[] data, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        Debug.Log("Start connection approval check");
        string playerID = Encoding.ASCII.GetString(data);

        Debug.Log(playerID);

        if (PlayerIdentityApprovalCallback != null)
        {
            PlayerIdentityApprovalCallback(playerID, (approved, username) =>
            {
                if (approved)
                {
                    PlayerNetworkConnection connection = new PlayerNetworkConnection
                    {
                        IsAuthenticated = true,
                        ClientId = clientId,
                        PlayerId = playerID,
                        UserName = username
                    };

                    Debug.Log("End connection approval check");
                    Debug.Log("Result: ACCEPTED");
                    Debug.Log($"Welcome {username}");

                    ServerLog.Log($"{username} has joined.");
                    Connections.Add(clientId, connection);
                    callback(true, null, true, GetSpawnPoint(), Quaternion.identity);

                }
                else
                {
                    callback(false, null, false, Vector3.zero, Quaternion.identity);
                    Debug.Log("End connection approval");
                    Debug.Log("Result: DENIED");
                }
            });
        } else
        {
            // if no approval checks are registered, we will allow the user and use the data passed as username.
            PlayerNetworkConnection connection = new PlayerNetworkConnection
            {
                IsAuthenticated = false,
                ClientId = clientId,
                PlayerId = playerID,
                UserName = playerID
            };

            Debug.Log("End connection approval check");
            Debug.Log("Result: ACCEPTED");
            Debug.Log($"Welcome {playerID}");

            ServerLog.Log($"{playerID} has joined.");

            callback(true, null, true, GetSpawnPoint(), Quaternion.identity);
        }



    }
    
    // Gets random position of tagged spawn points, or Vector3.zero if no spawns are defined.
    public Vector3 GetSpawnPoint()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag(SpawnPointTag);
        if (spawnPoints.Length > 0)
        {
            GameObject spawnPointObject = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            return spawnPointObject.transform.position;
        }
        else
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
    public string PlayerId;
    public string LobbyId;
    public string UserName;
    public ulong ClientId;
}

public delegate void PlayerIdentityVerificationDelegate(bool verified, string username);
