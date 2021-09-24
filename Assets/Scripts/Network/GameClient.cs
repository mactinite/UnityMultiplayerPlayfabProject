using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Helpers;
using System;
using MLAPI;
using System.Text;
using UnityEngine.UI;
using MLAPI.Transports.UNET;
using mactinite.ToolboxCommons;

/// <summary>
/// Right now this handles authenticating with playfab and launching the network client and manipulating the UI. 
/// Handles playfab authentication for all client types.
/// </summary>
public class GameClient : MonoBehaviour
{
    public GameObject signInDisplay;
    PlayFabAuthService _authService;
    NetworkManager _nm;
    public static string EntityId;
    public TMP_Text statusText;
    public GameObject authenticationPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text loginErrorText;
    public Toggle rememberMeToggle;
    public CameraFollow cameraFollow;

    [SerializeField, Scene]
    public string menuScene;

    public void Start()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(menuScene);

        bool isServer = false;
        var args = CommandLineUtility.GetArgs();
        Debug.Log($"Reading Args");
        _authService = PlayFabAuthService.Instance;

        if (args.TryGetValue("-mode", out string mlapiValue))
        {
            if (mlapiValue == "server")
            {
                isServer = true;
            }
        }


        if (!isServer)
        {
            _authService.InfoRequestParams = new GetPlayerCombinedInfoRequestParams();
            _authService.InfoRequestParams.GetUserAccountInfo = true;
            ActivateClient();
            _authService.Authenticate(Authtypes.UsernameAndPassword);
            loginErrorText.gameObject.SetActive(false);
        } else
        {
            // when running as a dedicated server we authenticate silently.
            _authService.Authenticate(Authtypes.Silent);
            PlayFabAuthService.OnLoginSuccess += OnServerLogin;
        }
    }

    private void OnServerLogin(LoginResult success)
    {
        Debug.Log("Server authenticated with playfab");
        GameServer.Instance.StartServer();
    }

    public void ActivateClient()
    {
        PlayFabAuthService.OnDisplayAuthentication += OnDisplayAuth;
        PlayFabAuthService.OnLoginSuccess += OnLoginSuccess;
        PlayFabAuthService.OnPlayFabError += OnError;

        _nm = NetworkManager.Singleton;
        _nm.OnClientConnectedCallback += OnConnected;
        _nm.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        
        var netManager = NetworkManager.Singleton;
        if (netManager.LocalClientId == clientId)
        {
            if (netManager.IsHost)
            {
                netManager.StopHost();

            }
            else if (netManager.IsClient)
            {
                netManager.StopClient();
            }
            else if (netManager.IsServer)
            {
                netManager.StopServer();
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(menuScene);
        }

    }

    public static void ConnectToServer(string ipv4Address, int port)
    {
        var networkManager = NetworkManager.Singleton;
        UNetTransport transport = networkManager.GetComponent<UNetTransport>();
        transport.ConnectAddress = ipv4Address;
        transport.ConnectPort = port;
        networkManager.StartClient();
    }


    public void SubmitAuth()
    {
        _authService.Username = usernameInput.text;
        _authService.Password = passwordInput.text;
        _authService.RememberMe = rememberMeToggle.isOn;

        _authService.Authenticate(Authtypes.UsernameAndPassword);
    }


    public void Logout()
    {
        _authService.RememberMe = false;
        _authService.ClearRememberMe();
        _authService.Username = "";
        _authService.Password = "";
        _authService.AuthTicket = "";
        _authService.Email = "";
        _authService.Authenticate(Authtypes.UsernameAndPassword);
    }

    private void OnError(PlayFabError error)
    {
        loginErrorText.gameObject.SetActive(true);
        loginErrorText.text = $"{error.ErrorMessage}";
        _authService.RememberMe = false;
        _authService.ClearRememberMe();
        Debug.Log(error.GenerateErrorReport());
    }

    private void OnConnected(ulong ClientId)
    {
        // client connected to server
    }

    private void OnLoginSuccess(LoginResult success)
    {

        EntityId = success.EntityToken.Entity.Id;
        // encode ID into the network manager so we can read it in the server when clients connect;
        _nm.NetworkConfig.ConnectionApproval = true;
        _nm.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(success.PlayFabId);


        if (authenticationPanel.activeSelf)
        {
            authenticationPanel.SetActive(false);
        }
        string statusMessage = $"Getting Account Info";
        statusText.text = statusMessage;
        Debug.Log(statusMessage);
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), (success) =>
        {
            _authService.Username = success.AccountInfo.Username;
            string statusMessage = $"Welcome back, {success.AccountInfo.Username}.";
            statusText.text = statusMessage;
            Debug.Log(statusMessage);
        }, (error) =>
        {
            string statusMessage = $"Oh no, something went wrong. :( try again later.";
            loginErrorText.text = statusMessage;
            Debug.Log(statusMessage);
            authenticationPanel.SetActive(true);
        }
        );
    }

    private void OnDisplayAuth()
    {
        authenticationPanel.SetActive(true);
    }

}
