
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;
using ECM.Controllers;
using TMPro;
using System;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using mactinite.ToolboxCommons;
using Random = UnityEngine.Random;
using UnityEngine.InputSystem;
using ECM.Examples;
using MLAPI.Logging;

namespace Game
{
    public class NetworkedPlayer : NetworkBehaviour, ITakeDamage
    {

        public float speed = 5f;
        public BaseCharacterController controller;
        public TMP_Text nameplate;
        public NetworkVariable<string> playerNameNetworkVariable = new NetworkVariable<string>();
        public NetworkVariable<float> healthNetworkVariable = new NetworkVariable<float>(100);
        private float _currentHealth = 100;
        public float maxHealth = 100;
        public bool isDead = true;
        public GameObject playerDisplay;
        [Layer]
        public int remotePlayerLayer;
        PlayerInput input;
        public float CurrentHealth { get => _currentHealth; }

        private void Start()
        {
            playerNameNetworkVariable.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
            playerNameNetworkVariable.Settings.ReadPermission = NetworkVariablePermission.Everyone;

            healthNetworkVariable.Settings.WritePermission = NetworkVariablePermission.ServerOnly;
            healthNetworkVariable.Settings.ReadPermission = NetworkVariablePermission.Everyone;
            _currentHealth = maxHealth;
            if (IsServer)
            {
                healthNetworkVariable.Value = _currentHealth;
            }
            else
            {
                healthNetworkVariable.OnValueChanged += OnHealthChanged;
            }



            playerNameNetworkVariable.OnValueChanged += PlayerNameChanged;


        }

        private void OnHealthChanged(float previousValue, float newValue)
        {
            // update damage
            _currentHealth = newValue;

            if (IsClient && !IsOwner)
            {
                // other player damage effects
                Debug.Log($"{playerNameNetworkVariable.Value} took damage, their health is now {newValue}");

            }
            else if (IsOwner)
            {
                // self damage effects
                Debug.Log($"Ouch! took damage, new health is {newValue}");
            }
        }

        private void PlayerNameChanged(string previousValue, string newValue)
        {
            if (!IsOwner && previousValue != newValue)
            {
                nameplate.text = newValue;
            }
        }

        public override void NetworkStart()
        {
            if (!IsOwner)
            {
                controller.enabled = false;
                nameplate.text = playerNameNetworkVariable.Value;
                nameplate.gameObject.SetActive(true);
                gameObject.layer = remotePlayerLayer;
            }


            if (IsOwner)
            {
                NetworkLog.LogInfoServer("Player Object NetworkStarted");
                nameplate.gameObject.SetActive(false);
                GetAccountInfoRequest request = new GetAccountInfoRequest();
                PlayFabClientAPI.GetAccountInfo(request, Success, Fail);
                CameraFollow.Instance.followTarget = transform;
            }

        }

        private void Fail(PlayFabError error)
        {
            Debug.LogError(error.GenerateErrorReport());
            nameplate.text = "Unknown Walrus";
        }

        private void Success(GetAccountInfoResult result)
        {
            string name = result.AccountInfo.Username;
            playerNameNetworkVariable.Value = name;
        }


        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, int clientId = -1)
        {
            Debug.Log($"Processing {damage} damage from {clientId}");
            _currentHealth -= damage;
            if (_currentHealth <= 0)
            {
                // kill player
                OnKillClientRpc();
                ulong sourceClientId = (ulong)clientId;
                if (clientId == -1)
                {
                    string[] deathPhrases =
                    {
                    " has shuffled off this mortal coil",
                    " has won a darwin award",
                    " has met their untimely demise",
                    " has unfortunately, died",
                    };
                    Debug.Log($"{GameServer.Instance.Connections[OwnerClientId].UserName} died to the elements");
                    ServerLog.Log($"{ GameServer.Instance.Connections[OwnerClientId].UserName} {deathPhrases[Random.Range(0, deathPhrases.Length)]}");
                }
                else
                {
                    Debug.Log($"{GameServer.Instance.Connections[OwnerClientId].UserName} was killed by {GameServer.Instance.Connections[sourceClientId].UserName}");
                    ServerLog.Log($"{GameServer.Instance.Connections[sourceClientId].UserName} killed {GameServer.Instance.Connections[OwnerClientId].UserName}");
                }

            }
            else
            {
                // update network variable
                healthNetworkVariable.Value = _currentHealth;
                healthNetworkVariable.SetDirty(true);

            }
        }

        [ServerRpc]
        public void RequestRespawnServerRpc()
        {
            StartCoroutine(Respawn());
        }

        [ClientRpc]
        public void OnKillClientRpc()
        {
            isDead = true;
            controller.enabled = false;
            playerDisplay.SetActive(false);
            gameObject.GetComponent<Collider>().enabled = false;
            if (IsOwner)
            {
                RequestRespawnServerRpc();
            }
        }

        [ClientRpc]
        public void OnRespawnClientRpc(Vector3 position)
        {
            transform.position = position;
            playerDisplay.SetActive(true);
            isDead = false;
            gameObject.GetComponent<Collider>().enabled = true;

            if (IsOwner)
            {
                controller.enabled = true;
            }
        }

        public void Damage(float damage, int clientId = -1)
        {
            TakeDamageServerRpc(damage, clientId);
        }

        IEnumerator Respawn()
        {
            ServerLog.Log($"{GameServer.Instance.Connections[OwnerClientId].UserName} Respawning in 3 seconds");
            yield return new WaitForSecondsRealtime(3f);
            _currentHealth = maxHealth;
            healthNetworkVariable.Value = _currentHealth;
            OnRespawnClientRpc(GameServer.Instance.GetSpawnPoint());
        }
    }
}