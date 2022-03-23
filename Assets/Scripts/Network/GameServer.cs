using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mactinite.ToolboxCommons;
using System.Text;
using Unity.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Game Server is the first custom layer on top of network manager. 
/// Provides methods to host multiplayer games either as a dedicated server or client host.
/// It provides extension points to (hopefully) facilitate 3rd party integration (Playfab, Steam, Etc).
/// </summary>
public class GameServer : SingletonMonobehavior<GameServer>
{
    [SerializeField]
    private bool inGame = false;
    private NetworkManager netManager;
    
    public Action<ulong> OnPlayerAdded;
    public Action<ulong> OnPlayerRemoved;

    public Action<string, PlayerIdentityVerificationDelegate> PlayerIdentityApprovalCallback;

    public string SpawnPointTag = "SpawnPoint";
    public GameObject playerPrefab;

    public GameObject serverLogPrefab;
    private ServerLog loggerInstance;

    [Scene]
    public string gameScene;

    [Scene]
    public string lobbyScene;

    public int maxPlayers = 16;
    public Dictionary<ulong, PlayerNetworkConnection> Connections
    {
        get { return _connections; }
        private set { _connections = value; }
    }

    public NetworkManager NetworkManager => netManager;

    private Dictionary<ulong, PlayerNetworkConnection> _connections;
    [SerializeField]
    private bool spawnPrefabOnConnect;

    void Start()
    {
        netManager = GetComponentInParent<NetworkManager>();
        
    }
    public void StartServer()
    {

        NetworkManager.OnClientConnectedCallback += ClientConnected;
        NetworkManager.OnClientDisconnectCallback += ClientDisconnect;
        NetworkManager.NetworkConfig.ConnectionApproval = true;
        NetworkManager.ConnectionApprovalCallback += ApproveConnection;
        _connections = new Dictionary<ulong, PlayerNetworkConnection>();
        NetworkManager.StartServer();
        SpawnServerLogger();
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneEvent;
        
        GoToLobby();
    }

    public void StartHost()
    {

        NetworkManager.OnClientConnectedCallback += ClientConnected;
        NetworkManager.OnClientDisconnectCallback += ClientDisconnect;
        NetworkManager.NetworkConfig.ConnectionApproval = true;
        NetworkManager.ConnectionApprovalCallback += ApproveConnection;
        _connections = new Dictionary<ulong, PlayerNetworkConnection>();
        NetworkManager.StartHost();
        SpawnServerLogger();
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneEvent;

        GoToLobby();
    }

    public void GoToLobby()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(lobbyScene, LoadSceneMode.Single);
    }
    
    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
    }

    private void OnSceneEvent(ulong clientId, string sceneName, LoadSceneMode loadSceneMode){

        if (sceneName == gameScene)
        {
            HandleInGameClientLoad(clientId);
        }
        
    }

    private void OnAllClientsLoaded(bool timedOut)
    {
        // we can test this later, but lets log to see where this is executed
        string timedOutString = timedOut ? "yes" : "no";
        NetworkLog.LogInfoServer($"All clients loaded! timed out? {timedOutString}.");
    }

    private void HandleInGameClientLoad(ulong clientId)
    {
        //spawn the player prefab and give ownership to the client.
        var go = Instantiate(playerPrefab, GetSpawnPoint(), Quaternion.identity);
        var netObj = go.GetComponent<NetworkObject>();

        // we'll dispose of the player objects when we change scenes.
        netObj.SpawnAsPlayerObject(clientId, destroyWithScene: true);
    }
    
    

    void SpawnServerLogger()
    {
        if (NetworkManager.IsServer)
        {
            var go = Instantiate(serverLogPrefab);
            loggerInstance = go.GetComponent<ServerLog>();
            go.GetComponent<NetworkObject>().Spawn(false);
        }
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

                    string sanitizedUsername = username ?? "No Username"; 
                    
                    PlayerNetworkConnection connection = new PlayerNetworkConnection
                    {
                        IsAuthenticated = true,
                        ClientId = clientId,
                        PlayerId = playerID,
                        UserName = sanitizedUsername
                    };

                    Debug.Log("End connection approval check");
                    Debug.Log("Result: ACCEPTED");
                    Debug.Log($"Welcome {sanitizedUsername}");

                    ServerLog.Log($"{sanitizedUsername} has joined.");
                    _connections.Add(clientId, connection);
                    callback(spawnPrefabOnConnect, null, true, GetSpawnPoint(), Quaternion.identity);
                    
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
            _connections.Add(clientId, connection);
            callback(spawnPrefabOnConnect, null, true, GetSpawnPoint(), Quaternion.identity);
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

        if (ClientId == 0)
        {
            Shutdown();
            return;
        }
        
        // remove player data from connections dictionary
        if (_connections.ContainsKey(ClientId))
        {
            ServerLog.Log($"{_connections[ClientId].UserName} has left.");
            _connections.Remove(ClientId);
        }
        
        OnPlayerRemoved?.Invoke(ClientId);
    }

    public void Shutdown()
    {
        NetworkManager.OnClientConnectedCallback -= ClientConnected;
        NetworkManager.OnClientDisconnectCallback -= ClientDisconnect;
        NetworkManager.ConnectionApprovalCallback -= ApproveConnection;
        NetworkManager.Shutdown();
        if (NetworkManager.IsServer)
        {
            Connections.Clear();
        }
    }

    private void ClientConnected(ulong ClientId)
    {

        // spawn prefabs for newly connecting clients.
        if (inGame)
        {
            //spawn the player prefab and give ownership to the client.
            HandleInGameClientLoad(ClientId);
        }

        OnPlayerAdded?.Invoke(ClientId);
    }
    
}


[Serializable]
public struct PlayerNetworkConnection : IEquatable<PlayerNetworkConnection>
{
    public bool IsAuthenticated;
    public FixedString32Bytes PlayerId;
    public FixedString32Bytes LobbyId;
    public FixedString32Bytes UserName;
    public ulong ClientId;

    public bool Equals(PlayerNetworkConnection other)
    {
        return IsAuthenticated == other.IsAuthenticated && PlayerId.Equals(other.PlayerId) && LobbyId.Equals(other.LobbyId) && UserName.Equals(other.UserName) && ClientId.Equals(other.ClientId);
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerNetworkConnection other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsAuthenticated, PlayerId, LobbyId, UserName, ClientId);
    }
}

public delegate void PlayerIdentityVerificationDelegate(bool verified, string username);
