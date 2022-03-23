using System.Collections;
using UnityEngine;
using PlayFab;
using System;
using System.Collections.Generic;
using PlayFab.MultiplayerAgent.Model;
using Unity.Netcode;
using PlayFab.ServerModels;

/// <summary>
/// AgentListener provides callbacks to GameServer to allow for deployment on playfab dedicated servers.
/// Use this only if you are deploying on Playfab hosted servers, as it interfaces with the MultiplayerAgentAPI.
/// </summary>
public class AgentListener : MonoBehaviour
{
    private List<ConnectedPlayer> _connectedPlayers;
    public bool Debugging = true;
    // Use this for initialization
    void Start()
    {
        if (Application.isEditor) return;

        var args = CommandLineUtility.GetArgs();

        Debug.Log($"AgentListener: Reading Args");
        if (args.TryGetValue("-mode", out string mlapiValue))
        {
            if (mlapiValue == "server")
            {
                StartRemoteServer();
            }
        }

    }




    private void StartRemoteServer()
    {
        Debug.Log("AgentListener: Starting Remote Server");
        _connectedPlayers = new List<ConnectedPlayer>();
        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = Debugging;
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;

        GameServer.Instance.OnPlayerAdded += OnPlayerAdded;
        GameServer.Instance.OnPlayerRemoved += OnPlayerRemoved;
        GameServer.Instance.PlayerIdentityApprovalCallback += ApprovePlayer;

        StartCoroutine(ReadyForPlayers());
    }


    /// <summary>
    /// Checks if there is an account with the playfab id and returns the username along with approving the connection.
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="callback"></param>
    public void ApprovePlayer(string playerID, PlayerIdentityVerificationDelegate callback)
    {
        Debug.Log("AgentListener: Begin Playfab user approval.");
        PlayFabServerAPI.GetUserAccountInfo(new GetUserAccountInfoRequest
        {
            PlayFabId = playerID,
        },
       success =>
       {
           Debug.Log("AgentListener: Playfab user found");
           callback(true, success.UserInfo.Username);
       },
       fail =>
       {
           Debug.Log("AgentListener: Invalid Playfab ID");
           callback(false, "");
       }); ;

    }

    IEnumerator ReadyForPlayers()
    {
        Debug.Log("AgentListener: Moving To Ready");
        yield return new WaitForSeconds(.5f);
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
    }

    private void OnServerActive()
    {
        GameServer.Instance.StartServer();
        Debug.Log("AgentListener: Server Started From Agent Activation");
    }

    private void OnPlayerRemoved(ulong clientId)
    {
        string playfabId = GameServer.Instance.Connections[clientId].PlayerId.ToString();
        ConnectedPlayer player = _connectedPlayers.Find(x => x.PlayerId.Equals(playfabId, StringComparison.OrdinalIgnoreCase));
        _connectedPlayers.Remove(player);
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
    }

    private void OnPlayerAdded(ulong clientId)
    {
        string playfabId = GameServer.Instance.Connections[clientId].PlayerId.ToString();
        _connectedPlayers.Add(new ConnectedPlayer(playfabId));
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
    }

    private void OnAgentError(string error)
    {
        Debug.Log(error);
    }

    private void OnShutdown()
    {
        Debug.Log("AgentListener: Server is shutting down");
        foreach (var conn in GameServer.Instance.Connections)
        {
            NetworkManager.Singleton.DisconnectClient(conn.Value.ClientId);
        }
        StartCoroutine(Shutdown());
    }

    IEnumerator Shutdown()
    {
        yield return new WaitForSeconds(5f);
        Application.Quit();
    }

    private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
    {
        Debug.LogFormat("AgentListener: Maintenance scheduled for: {0}", NextScheduledMaintenanceUtc.Value.ToLongDateString());
        // TODO: Send Maintenance data via RPC. 
        //foreach (var conn in GameServer.Instance.Connections)
        //{
        //    conn.Connection.Send<MaintenanceMessage>(new MaintenanceMessage()
        //    {
        //        ScheduledMaintenanceUTC = (DateTime)NextScheduledMaintenanceUtc
        //    });
        //}
    }
}