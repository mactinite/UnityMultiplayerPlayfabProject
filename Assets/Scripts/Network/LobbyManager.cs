using MLAPI;
using MLAPI.Messaging;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MLAPI.NetworkVariable.Collections;
using MLAPI.NetworkVariable;
using mactinite.ToolboxCommons;

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

    [SerializeField]
    private NetworkList<LobbyPlayer> players = new NetworkList<LobbyPlayer>(new NetworkVariableSettings
    {
        ReadPermission = NetworkVariablePermission.Everyone,
        WritePermission = NetworkVariablePermission.ServerOnly,
    });
    [SerializeField, Scene]
    private string menuScene;

    public override void NetworkStart()
    {

        // server needs to change the Ready button to a start button.
        if (IsHost)
        {
            readyButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(StartGame);
            GameServer.Instance.OnPlayerAdded += PlayerAdded;
            GameServer.Instance.OnPlayerRemoved += PlayerRemoved;
            var netManager = NetworkManager.Singleton;
            // loop through connections on network manager and add to players list
            for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
            {
                ulong clientId = netManager.ConnectedClientsList[i].ClientId;
                players.Add(new LobbyPlayer
                {
                    userName = GameServer.Instance.Connections[clientId].UserName,
                    ClientId = clientId,
                    isReady = false,
                });
            }
            UpdateUI();
            startButton.interactable = CheckAllPlayersReady();

        }
        else if(IsClient)
        {
            startButton.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(true);
            players.OnListChanged += onPlayersChange;
            readyButton.onClick.AddListener(ReadyUp);
        } else
        {
            startButton.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(false);
            GameServer.Instance.OnPlayerAdded += PlayerAdded;
            GameServer.Instance.OnPlayerRemoved += PlayerRemoved;
            var netManager = NetworkManager.Singleton;
            // loop through connections on network manager and add to players list
            for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
            {
                ulong clientId = netManager.ConnectedClientsList[i].ClientId;

                players.Add(new LobbyPlayer
                {
                    userName = GameServer.Instance.Connections[clientId].UserName,
                    ClientId = clientId,
                    isReady = false,
                });
                UpdateUI();
            }
            // we are dedicated server so perhaps some kind of match timer or start criteria.
        }

        leaveButton.onClick.AddListener(LeaveLobby);

    }

    public void OnDestroy()
    {
        GameServer.Instance.OnPlayerAdded -= PlayerAdded;
        GameServer.Instance.OnPlayerRemoved -= PlayerRemoved;
    }

    private void onPlayersChange(NetworkListEvent<LobbyPlayer> changeEvent)
    {
        UpdateUI();
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

        foreach (var player in players)
        {
            if (player.isReady == false && player.ClientId != NetworkManager.Singleton.ServerClientId)
            {
                AllReady = false;
            }
        }

        return AllReady;
    }

    private void PlayerRemoved(ulong clientId)
    {
        LobbyPlayer player = players.ToList().Find(x => x.ClientId.Equals(clientId));
        players.Remove(player);
        UpdateUI();
    }

    private void PlayerAdded(ulong clientId)
    {
        players.Add(new LobbyPlayer
        {
            userName = GameServer.Instance.Connections[clientId].UserName,
            ClientId = clientId, 
            isReady = false 
        });
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
        foreach(var player in players)
        {
            GameObject listItem = Instantiate(playerListItemPrefab, playerList.transform);
            TMP_Text[] text = listItem.GetComponentsInChildren<TMP_Text>();
            text[0].text = player.userName;
            text[1].text = player.isReady ? "READY" : "NOT READY";

            if(player.ClientId == NetworkManager.Singleton.ServerClientId)
            {
                text[1].text = "HOST";
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClientReadyServerRpc(ulong clientId, bool isReady)
    {
        LobbyPlayer player = players.ToList().Find(x => x.ClientId.Equals(clientId));
        bool oldValue = player.isReady;
        player.isReady = isReady;
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
                TargetClientIds = players.ToList().ConvertAll<ulong>(player => player.ClientId).ToArray(),
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
        LobbyPlayer player = players.ToList().Find(x => x.ClientId.Equals(clientId));
        bool oldValue = player.isReady;
        player.isReady = isReady;
        if (oldValue != isReady)
        {
            UpdateUI();
        }
    }

    public void LeaveLobby()
    {
        if (IsHost)
        {
            //shut down server and go back to main menu.
            NetworkManager.Singleton.StopHost();
            UnityEngine.SceneManagement.SceneManager.LoadScene(menuScene);
        } else
        {
            NetworkManager.Singleton.StopClient();
            UnityEngine.SceneManagement.SceneManager.LoadScene(menuScene);

        }
    }



}
