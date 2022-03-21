using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;
using Unity.Netcode;

public class TestNetworkMenu : MonoBehaviour
{

    public List<MenuTab> tabs = new List<MenuTab>();


    [System.Serializable]
    public struct MenuTab
    {
        public Button button;
        public GameObject menuObject;
    }


    private void Start()
    {
        joinButton.onClick.AddListener(JoinGame);
    }
    public void HostGame()
    {
        GameServer.Instance.StartHost();
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnect;
        gameObject.SetActive(false);
    }

    private void OnDisconnect(ulong client)
    {
        if(client == NetworkManager.Singleton.LocalClientId)
        {
            gameObject.SetActive(true);
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnect;
        }
    }

    public TMP_InputField ipInputField;
    public TMP_InputField portInputField;
    public Button joinButton;
    public TMP_Text statusText;
    public TMP_Text errorText;
    public void JoinGame()
    {

        string ipAddress = ipInputField.text;
        string port = portInputField.text;




        if (IPAddress.TryParse(ipAddress, out var iPAddress))
        {
            if (int.TryParse(port, out int portNumber))
            {
                GameClient.ConnectToServer(ipAddress, portNumber);
                //hide input fields.
                ipInputField.gameObject.SetActive(false);
                portInputField.gameObject.SetActive(false);
                joinButton.gameObject.SetActive(false);
                statusText.gameObject.SetActive(true);

                Invoke("ConnectionTimeout", 10f);
                statusText.text = "Connecting...";
            }
            else
            {

                errorText.text = "Invalid port number";
            }
        }
        else
        {
            errorText.text = "Invalid IP Address";
        }


    }
    
    private void ConnectionTimeout()
    {
        ipInputField.gameObject.SetActive(true);
        portInputField.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);
        statusText.gameObject.SetActive(false);
        errorText.text = "connection timeout, try again";
        NetworkManager.Singleton.Shutdown();
    }
}
