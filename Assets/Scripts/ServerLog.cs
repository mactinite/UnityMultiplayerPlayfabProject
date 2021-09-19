using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mactinite.ToolboxCommons;
using MLAPI;
using MLAPI.Messaging;
using TMPro;

public class ServerLog : NetworkBehaviour
{
    public Transform LinePrefab;
    public static ServerLog Instance;
    public Transform logLayout;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        } else
        {
            Destroy(this);
        }
    }

    public static void Log(string message)
    {
        Instance.LogMessage(message);
    }

    public void LogMessage(string message)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            SendMessageToClientsClientRpc(message);
        }
    }
    
    [ClientRpc]
    private void SendMessageToClientsClientRpc(string message)
    {
        LogInternal(message);
    }


    // instantiate the log on the client
    private void LogInternal(string message)
    {
        Transform line = Instantiate(LinePrefab, logLayout);
        line.GetComponentInChildren<TMP_Text>().text = message;
        Destroy(line.gameObject, 6f);
    }

}
