using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOverDisplay : MonoBehaviour
{
    [SerializeField] private GameObject gameObjectDisplayParent = null;
    [SerializeField] private TMP_Text winnerNameText = null;

    private void Start()
    {
        GameOverHandler.ClientOnGameOver += ClientHandlerGameOver;
    }

    private void OnDestroy()
    {
        GameOverHandler.ClientOnGameOver -= ClientHandlerGameOver;
    }

    public void LeaveGame()
    {
        if(NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();
        }
    }

    private void ClientHandlerGameOver(string winner)
    {
        winnerNameText.text = $"{winner} has Won!";
        gameObjectDisplayParent.SetActive(true);
    }
}
