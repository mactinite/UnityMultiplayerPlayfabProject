using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using PlayFab;
using PlayFab.MultiplayerModels;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Matchmaker : MonoBehaviour
{
    public GameObject playButton;
    public GameObject leaveQueueButton;
    public TMP_Text statusText;
    private const string QueueName = "QuickPlay";
    private string ticketId;
    private Coroutine pollTicketCoroutine;


    public void StartMatchMaking()
    {
        playButton.SetActive(false);
        statusText.text = "Joining Queue";
        statusText.gameObject.SetActive(true);

        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest
            {
                Creator = new MatchmakingPlayer
                {
                    Entity = new EntityKey
                    {
                        Id = GameClient.EntityId,
                        Type = "title_player_account"
                    },
                    Attributes = new MatchmakingPlayerAttributes
                    {
                        DataObject = new { }
                    }
                },
                GiveUpAfterSeconds = 120,
                QueueName = QueueName
            },
            OnMatchmakingTicketCreated,
            OnMatchMakingError);

    }

    private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult result)
    {
        ticketId = result.TicketId;
        pollTicketCoroutine = StartCoroutine(PollTicket());
        leaveQueueButton.SetActive(true);
        statusText.text = "In Queue";
    }

    private void OnMatchMakingError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    private IEnumerator PollTicket()
    {
        while (true)
        {
            PlayFabMultiplayerAPI.GetMatchmakingTicket(new GetMatchmakingTicketRequest
            {
                TicketId = ticketId,
                QueueName = QueueName,
            },
            OnGetMatchmakingTicket,
            OnMatchMakingError);

            yield return new WaitForSeconds(6);
        }
    }
    private void OnGetMatchmakingTicket(GetMatchmakingTicketResult result)
    {
        statusText.text = $"Status: {result.Status}";

        switch (result.Status)
        {
            case "Matched":
                StopCoroutine(pollTicketCoroutine);
                StartMatch(result.MatchId);
                break;
            case "Canceled":
                break;
        }
    }

    private void StartMatch(string matchId)
    {
        statusText.text = $"Starting Match!";
        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest
            {
                MatchId = matchId,
                QueueName = QueueName
            },
               OnGetMatch,
               OnMatchMakingError
            );
    }

    private void OnGetMatch(GetMatchResult result)
    {
        statusText.text = $"Starting Match!";
        NetworkManager.Singleton.StartClient();
    }
}
