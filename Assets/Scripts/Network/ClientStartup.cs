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

public class ClientStartup : MonoBehaviour
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
    public Button submitButton;
    public Button playButton;

    public Button logoutButton;

    public Toggle rememberMeToggle;
    public GameObject findServerPanel;

    public CameraFollow cameraFollow;

    public void Start()
    {


        bool isServer = false;
        var args = GetCommandlineArgs();
        Debug.Log($"Reading Args {args.Values.ToString()}");
        if (args.TryGetValue("-mode", out string mlapiValue))
        {
            if (mlapiValue == "server")
            {
                isServer = true;
            }
        }

        if (!isServer)
        {
            _authService = PlayFabAuthService.Instance;
            _authService.InfoRequestParams = new GetPlayerCombinedInfoRequestParams();
            _authService.InfoRequestParams.GetUserAccountInfo = true;
            StartClient();
            _authService.Authenticate(Authtypes.UsernameAndPassword);
            loginErrorText.gameObject.SetActive(false);
            submitButton.onClick.AddListener(SubmitAuth);
            logoutButton.onClick.AddListener(Logout);

        }


    }

    public void StartClient()
    {
        PlayFabAuthService.OnDisplayAuthentication += OnDisplayAuth;
        PlayFabAuthService.OnLoginSuccess += OnLoginSuccess;
        PlayFabAuthService.OnPlayFabError += OnError;

        _nm = NetworkManager.Singleton;
        _nm.OnClientConnectedCallback += OnConnected;
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
        _authService.Authenticate();
        if (ClientId == NetworkManager.Singleton.LocalClientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ClientId,
            out var networkedClient))
            {
                var player = networkedClient.PlayerObject;
                if (player)
                {
                    cameraFollow.followTarget = player.transform;
                    authenticationPanel.SetActive(false);
                    findServerPanel.SetActive(false);
                }
            }
        }
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
            findServerPanel.SetActive(true);
        }
        logoutButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);
        string statusMessage = $"Getting Account Info";
        statusText.text = statusMessage;
        Debug.Log(statusMessage);
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), (success) =>
        {
            _authService.Username = success.AccountInfo.Username;
            string statusMessage = $"Welcome back, {success.AccountInfo.Username}.";
            logoutButton.gameObject.SetActive(true);
            playButton.gameObject.SetActive(true);
            statusText.text = statusMessage;
            Debug.Log(statusMessage);
        }, (error) =>
        {
            string statusMessage = $"Oh no, something went wrong. :( try again later.";
            loginErrorText.text = statusMessage;
            Debug.Log(statusMessage);
            authenticationPanel.SetActive(true);
            findServerPanel.SetActive(false);
        }
        );



    }

    private void OnDisplayAuth()
    {
        authenticationPanel.SetActive(true);
    }

    private Dictionary<string, string> GetCommandlineArgs()
    {
        Dictionary<string, string> argDictionary = new Dictionary<string, string>();

        var args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; ++i)
        {
            var arg = args[i].ToLower();
            if (arg.StartsWith("-"))
            {
                var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
                value = (value?.StartsWith("-") ?? false) ? null : value;

                argDictionary.Add(arg, value);
            }
        }
        return argDictionary;
    }

}
