using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Playfab Player Approval
/// Adds an approval callback to GameServer that prevents clients without a playfab account from connecting and returns the username.
/// </summary>
public class PlayfabPlayerApproval : MonoBehaviour
{
   
    // Start is called before the first frame update
    void Start()
    {
        // register approval callback
        GameServer.Instance.PlayerIdentityApprovalCallback += ApprovePlayer;
    }

    // Update is called once per frame
    void Update()
    {

    }


    /// <summary>
    /// Checks if there is an account with the playfab id and returns the username along with approving the connection.
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="callback"></param>
    public void ApprovePlayer(string playerID, PlayerIdentityVerificationDelegate callback)
    {
        Debug.Log("AgentListener: Begin Playfab user approval.");
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest
        {
            PlayFabId = playerID,
        },
       success =>
       {
           Debug.Log("PlayfabPlayerApproval: Playfab user found");
           callback(true, success.AccountInfo.Username);
       },
       fail =>
       {
           Debug.Log("PlayfabPlayerApproval: Invalid Playfab ID");
           callback(false, "");
       }); ;

    }

}
