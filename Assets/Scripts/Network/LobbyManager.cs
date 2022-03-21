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


    [SerializeField, Scene]
    private string menuScene;
    
    public override void OnNetworkSpawn()
    {
        // server needs to change the Ready button to a start button.
        if (IsServer)
        {
            readyButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(StartGame);
            GameServer.Instance.OnPlayerAdded += PlayerAdded;
            GameServer.Instance.OnPlayerRemoved += PlayerRemoved;
            PlayerAdded(0);
            UpdateUI();
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
        readyStatus.Remove(clientId);
        UpdateUI();
    }

    private void PlayerAdded(ulong clientId)
    {
        readyStatus.Add(clientId, false);
        UpdateUI();
    }


    private void UpdateUI()
    {
        // clear list of players
        foreach (Transform child in playerList.transform)
        {
            Destroy(child.gameObject);
        }

        // now lets create new ones for everyone.
        foreach(var player in GameServer.Instance.Connections)
        {
            GameObject listItem = Instantiate(playerListItemPrefab, playerList.transform);
            TMP_Text[] text = listItem.GetComponentsInChildren<TMP_Text>();
            text[0].text = player.Value.PlayerId;
            text[1].text = readyStatus[player.Key] ? "READY" : "NOT READY";

            if(player.Value.ClientId == NetworkManager.Singleton.ServerClientId)
            {
                text[1].text = "HOST";
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClientReadyServerRpc(ulong clientId, bool isReady)
    {
        var player = GameServer.Instance.Connections.ToList().Find(x => x.Key.Equals(clientId));
        bool oldValue = readyStatus[player.Key];
        readyStatus[player.Key] = isReady;
        if (oldValue != isReady)
        {
            UpdateUI();
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

    private void BroadcastRpc(ulong clientId, bool isReady)
    {
        // broadcast RPC
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = GameServer.Instance.Connections.Values.ToList().ConvertAll<ulong>(player => player.ClientId).ToArray(),
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
        // update player list
        var player = GameServer.Instance.Connections.Values.ToList().Find(x => x.ClientId.Equals(clientId));
        bool oldValue = readyStatus[player.ClientId];
        readyStatus[player.ClientId] = isReady;
        if (oldValue != isReady)
        {
            UpdateUI();
        }
    }

    public void LeaveLobby()
    {
        NetworkManager.Singleton.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene(menuScene);
    }



}
