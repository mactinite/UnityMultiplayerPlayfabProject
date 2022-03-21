using Unity.Netcode;
using PlayFab;
using PlayFab.MultiplayerModels;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode.Transports.UNET;
using TMPro;
using Random = UnityEngine.Random;
using PlayFab.ClientModels;
using UnityEngine.UI;
using System.Net;

public class LoadServer : MonoBehaviour
{
    public string serverBuildID;
    public GameObject playButton;
    public TMP_Text statusText;

    public TMP_InputField customServerIpField;
    public TMP_InputField customServerPortField;
    public Button connectToCustomServerButton;

    public CameraFollow cameraFollow;
    public GameObject FindServerPanel;
    public void RequestServer()
    {
        Debug.Log("Getting Server");
        RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest();

        PlayFabClientAPI.ExecuteCloudScript<ListMultiplayerServersResponse>(new PlayFab.ClientModels.ExecuteCloudScriptRequest
        {
            FunctionName = "GetServerList", // Arbitrary function name (must exist in your uploaded cloud.js file)
            FunctionParameter = new { buildId = serverBuildID }, // The parameter provided to your function
            GeneratePlayStreamEvent = true, // Optional - Shows this event in PlayStream
        }, OnCloudServerList, OnRequestMultiplayerServerError);

        //PlayFabMultiplayerAPI.ListMultiplayerServers(new ListMultiplayerServersRequest
        //{
        //    BuildId = serverBuildID,
        //    Region = AzureRegion.EastUs.ToString()
        //},
        //OnRequestMultiplayerServerList,
        //OnRequestMultiplayerServerError
        //);

        playButton.SetActive(false);
        statusText.gameObject.SetActive(true);
        statusText.text = "Retrieving Server List";



    }

    public void ConnectToCustomServer()
    {
        string ipAddress = customServerIpField.text;
        string port = customServerPortField.text;
        if (IPAddress.TryParse(ipAddress, out var iPAddress))
        {
            if (int.TryParse(port, out int portNumber))
            {
                GameClient.ConnectToServer(ipAddress, portNumber);
            }
            else
            {
                statusText.text = "Invalid port number";
            }
        }
        else
        {
            statusText.text = "Invalid IP Address";
        }
    }

    private void OnCloudServerList(ExecuteCloudScriptResult result)
    {
        if (result.FunctionResult != null)
        {
            ListMultiplayerServersResponse list = (ListMultiplayerServersResponse)result.FunctionResult;
            OnRequestMultiplayerServerList(list);
        }
        else
        {
            statusText.text = "No servers available for some reason. Try again later.";
            playButton.SetActive(true);
        }
    }

    private void OnRequestMultiplayerServerList(ListMultiplayerServersResponse result)
    {
        int serverCount = result.MultiplayerServerSummaries.Count;
        string sessionToJoin = result.MultiplayerServerSummaries[Random.Range(0, serverCount)].SessionId;
        if (sessionToJoin != null)
        {
            GetMultiplayerServerDetails(sessionToJoin);
        }
        else
        {
            statusText.text = "No sessions available, creating a new one";
            RequestMultiplayerServerRequest request = new RequestMultiplayerServerRequest();
            request.BuildId = serverBuildID;
            request.SessionId = System.Guid.NewGuid().ToString();
            request.PreferredRegions = new List<string>() { AzureRegion.EastUs.ToString() };
            PlayFabMultiplayerAPI.RequestMultiplayerServer(request, OnRequestMultiplayerServer, OnRequestMultiplayerServerError);
        }

    }

    private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse result)
    {
        statusText.text = $"Connecting";
        Debug.Log($"Connecting to {result.IPV4Address} : {result.Ports[0].Num}");
        GameClient.ConnectToServer(result.IPV4Address, (ushort)result.Ports[0].Num);
    }

    public void GetMultiplayerServerDetails(string Session)
    {
        PlayFabClientAPI.ExecuteCloudScript<GetMultiplayerServerDetailsResponse>(new PlayFab.ClientModels.ExecuteCloudScriptRequest
        {
            FunctionName = "GetServerDetails", // Arbitrary function name (must exist in your uploaded cloud.js file)
            FunctionParameter = new { sessionId = Session, buildId = serverBuildID }, // The parameter provided to your function
            GeneratePlayStreamEvent = true, // Optional - Shows this event in PlayStream
        }, OnGetServerDetails, OnGetServerDetailsError);

        statusText.text = "Getting Server Details";
    }

    private void OnGetServerDetailsError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    private void OnGetServerDetails(ExecuteCloudScriptResult result)
    {
        GetMultiplayerServerDetailsResponse response = (GetMultiplayerServerDetailsResponse)result.FunctionResult;

        if (response != null)
        {
            statusText.text = $"Connecting";
            Debug.Log($"Connecting to {response.IPV4Address} : {response.Ports[0].Num}");
            GameClient.ConnectToServer(response.IPV4Address, (ushort)response.Ports[0].Num);
        }
        else
        {
            Debug.Log(result.Error.Message);
            statusText.text = "Unable to join server, try again later";
            playButton.SetActive(true);
        }
    }



    private void OnRequestMultiplayerServerError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
        statusText.text = "Unable to join server, try again later";
        playButton.SetActive(true);
    }

}
