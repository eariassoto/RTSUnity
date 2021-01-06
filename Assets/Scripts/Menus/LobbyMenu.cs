using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour
{
    [SerializeField] private GameObject lobbyUI = null;
    [SerializeField] private Button startGameButton = null;
    [SerializeField] private TMP_Text[] playersNameTexts = new TMP_Text[4];

    private void Start()
    {
        RTSNetworkManager.ClientOnConnected += HandleClientConnected;
        RTSPlayer.AuthorityOnPartyOwnerStateUpdated += AuthorityHandleOnPartyOwnerStateUpdated;
        RTSPlayer.ClientOnInfoUpdated += ClientHandleOnInfoUpdated;
    }

    private void OnDestroy()
    {
        RTSNetworkManager.ClientOnConnected -= HandleClientConnected;
        RTSPlayer.AuthorityOnPartyOwnerStateUpdated -= AuthorityHandleOnPartyOwnerStateUpdated;
        RTSPlayer.ClientOnInfoUpdated -= ClientHandleOnInfoUpdated;
    }

    private void ClientHandleOnInfoUpdated()
    {
        List<RTSPlayer> players = ((RTSNetworkManager)NetworkManager.singleton).Players;
        for(int i = 0; i < players.Count; i++)
        {
            playersNameTexts[i].text = players[i].GetDisplayName();
        }
        for (int i = players.Count; i < playersNameTexts.Length; i++)
        {
            playersNameTexts[i].text = "Waiting for Player...";
        }

        startGameButton.interactable = players.Count >= 2;
    }

    private void HandleClientConnected()
    {
        lobbyUI.SetActive(true);
    }

    public void StartGame()
    {
        NetworkClient.connection.identity.GetComponent<RTSPlayer>().CmdStartGame();
    }

    public void LeaveLobby()
    {
        if(NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();

            SceneManager.LoadScene(0);
        }
    }

    public void AuthorityHandleOnPartyOwnerStateUpdated(bool state)
    {
        startGameButton.gameObject.SetActive(state);
    }
}
