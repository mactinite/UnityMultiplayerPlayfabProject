using Unity.Netcode;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using mactinite.ToolboxCommons;
using PlayFab.ServerModels;
using UnityEngine.SceneManagement;

/// <summary>
/// Lobby manager will read the connections from game server and render a list. 
/// It will also wait for all connected clients to ready up before enabling the start game button on the host.
/// </summary>
public partial class LobbyManager : NetworkBehaviour
{

    public Button startButton;
    public Button readyButton;
    public Button leaveButton;
    public GameObject playerList;
    public GameObject playerListItemPrefab;
    public Dictionary<ulong, bool> readyStatus = new Dictionary<ulong, bool>();
    public Dictionary<ulong, GameObject> lobbyListEntries = new Dictionary<ulong, GameObject>();


    [SerializeField, Scene]
    private string menuScene;
    
    public void Start()
    {
        // server needs to change the Ready button to a start button.
        if (IsServer)
        {
            readyButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(StartGame);
            GameServer.Instance.OnPlayerAdded += PlayerAdded;
            GameServer.Instance.OnPlayerRemoved += PlayerRemoved;

            // this should handle anyone who connected before the lobby manager loaded.
            foreach (var clientid in NetworkManager.ConnectedClientsIds)
            {
                if(!lobbyListEntries.ContainsKey(clientid))
                    PlayerAdded(clientid);
            }
            
            startButton.interactable = CheckAllPlayersReady();

        }
        else if(IsClient)
        {
            startButton.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(true);
            GameServer.Instance.OnPlayerAdded += PlayerAdded;
            GameServer.Instance.OnPlayerRemoved += PlayerRemoved;
            readyButton.onClick.AddListener(ReadyUp);
        }

        leaveButton.onClick.AddListener(LeaveLobby);

    }

    private void ReadyUp()
    {
        ClientReadyServerRpc(NetworkManager.Singleton.LocalClientId, true);
    }

    private void StartGame()
    {

        if (CheckAllPlayersReady())
        {
            GameServer.Instance.StartGame();
        }
    }

    private bool CheckAllPlayersReady()
    {
        bool AllReady = true;

        // foreach (var player in players)
        // {
        //     if (player.isReady == false && player != NetworkManager.Singleton.ServerClientId)
        //     {
        //         AllReady = false;
        //     }
        // }

        return AllReady;
    }

    private void PlayerRemoved(ulong clientId)
    {
        PlayerLeftClientRpc(clientId);
    }

    private void PlayerAdded(ulong clientId)
    {
        PlayerJoinedClientRpc(clientId, GameServer.Instance.Connections[clientId]);

        // Send all the player data to newly joining players. 
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong>(){clientId}
            }
        };
        foreach (var player in GameServer.Instance.Connections.Values)
        {
            if(player.ClientId != clientId)
                PlayerJoinedClientRpc(player.ClientId, player, clientRpcParams);
        }
        
    }
    

    [ServerRpc(RequireOwnership = false)]
    public void ClientReadyServerRpc(ulong clientId, bool isReady)
    {
        bool oldValue = readyStatus[clientId];
        readyStatus[clientId] = isReady;
        if (oldValue != isReady)
        {
            BroadcastRpc(clientId, isReady);

            if (CheckAllPlayersReady())
            {
                startButton.interactable = true;
            } else
            {
                startButton.interactable = false;
            }
        }
    }


    [ClientRpc]
    public void PlayerJoinedClientRpc(ulong clientId, PlayerNetworkConnection connectionData,
        ClientRpcParams clientRpcParams = default)
    {

        if (readyStatus.ContainsKey(clientId))
        {
            readyStatus[clientId] = false;
        }
        else
        {
            readyStatus.Add(clientId, false);
        }
        
        if (lobbyListEntries.TryGetValue(clientId, out var listItem))
        {
            TMP_Text[] text = listItem.GetComponentsInChildren<TMP_Text>();
            text[0].text = connectionData.UserName.ToString();
            text[1].text = readyStatus[clientId] ? "READY" : "NOT READY";

            if (clientId == NetworkManager.Singleton.ServerClientId)
            {
                text[1].text = "HOST";
            }
            
        }
        else
        {
            GameObject newListItem = Instantiate(playerListItemPrefab, playerList.transform);
            TMP_Text[] text = newListItem.GetComponentsInChildren<TMP_Text>();
            text[0].text = connectionData.UserName.ToString();
            text[1].text = readyStatus[clientId] ? "READY" : "NOT READY";

            if (clientId == NetworkManager.Singleton.ServerClientId)
            {
                text[1].text = "HOST";
            }
            
            lobbyListEntries.Add(clientId, newListItem);
        }
        
            
    }

    [ClientRpc]
    public void PlayerLeftClientRpc(ulong clientId, ClientRpcParams clientRpcParams = default)
    {
        if (lobbyListEntries.ContainsKey(clientId))
        {
            Destroy(lobbyListEntries[clientId]);
            lobbyListEntries.Remove(clientId);
        }
    }

    private void BroadcastRpc(ulong clientId, bool isReady)
    {
        // broadcast RPC
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = GameServer.Instance.Connections.Keys.ToList(),
            }
        };

        PlayerReadyClientRpc(clientId, isReady, clientRpcParams);
    }

    /// <summary>
    /// Changes ready state for clientId to isReady on the clients. Updates UI if it was changed.
    /// </summary>
    /// <param name="clientId">client to update</param>
    /// <param name="isReady">ready status</param>
    [ClientRpc]
    public void PlayerReadyClientRpc(ulong clientId, bool isReady, ClientRpcParams clientRpcParams = default)
    {
        if (readyStatus.ContainsKey(clientId))
        {
            readyStatus[clientId] = isReady;
            TMP_Text[] text = lobbyListEntries[clientId].GetComponentsInChildren<TMP_Text>();
            text[1].text = isReady ? "READY" : "NOT READY";
            text[1].color = isReady ? Color.green : Color.red;
        }
    }

    public void LeaveLobby()
    {
       
        GameServer.Instance.Shutdown();
        SceneManager.LoadScene(menuScene, LoadSceneMode.Single);

    }

    public override void OnDestroy()
    {
        // clean up event delegates
        if (GameServer.Instance != null)
        {
            GameServer.Instance.OnPlayerAdded -= PlayerAdded;
            GameServer.Instance.OnPlayerRemoved -= PlayerRemoved;
        }

        base.OnDestroy();
    }
}
