using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField]
    private int maxHealth = 100;

    [SyncVar(hook = nameof(HandleHeathUpdated))]
    private int currentHealth;

    public event Action ServerOnDie;

    public event Action<int, int> ClientOnHeathUpdated;

    #region Server

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
        UnitBase.ServerOnPlayerDie += ServerHandlePlayerDie;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        UnitBase.ServerOnPlayerDie += ServerHandlePlayerDie;
    }

    [Server]
    private void ServerHandlePlayerDie(int connectionId)
    {
        if(connectionId == connectionToClient.connectionId)
        {
            DealDamage(currentHealth);
        }
    }

    [Server]
    public void DealDamage(int damageAmount)
    {
        if (currentHealth == 0)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damageAmount, 0);

        if (currentHealth != 0)
        {
            return;
        }

        ServerOnDie?.Invoke();
    }

    #endregion

    #region Client

    private void HandleHeathUpdated(int oldHealth, int newHealth)
    {
        ClientOnHeathUpdated?.Invoke(newHealth, maxHealth);
    }

    #endregion

}
