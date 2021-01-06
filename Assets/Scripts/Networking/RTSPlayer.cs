using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSPlayer : NetworkBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private LayerMask buildingBlockLayer = new LayerMask();
    [SerializeField] private Building[] buildings = new Building[0];
    [SerializeField] private float buildingRangeLimit = 5f;

    [SyncVar(hook = nameof(ClientHandleResourcesUpdated))]
    private int resources = 500;
    
    [SyncVar(hook = nameof(AuthorityHandlePartyOwnerStateUpdated))]
    private bool isPartyOwner = false;
    [SyncVar(hook = nameof(ClientHandleDisplayNameUpdated))]
    private string displayName;

    public event Action<int> ClientOnResourcesUpdated;

    public static event Action<bool> AuthorityOnPartyOwnerStateUpdated;
    public static event Action ClientOnInfoUpdated;

    private Color teamColor = new Color();
    private List<Unit> myUnits = new List<Unit>();
    private List<Building> myBuildings = new List<Building>();

    public string GetDisplayName()
    {
        return displayName;
    }

    public bool GetIsPartyOwner()
    {
        return isPartyOwner;
    }

    public List<Unit> GetMyUnits()
    {
        return myUnits;
    }

    public int GetResources()
    {
        return resources;
    }

    public List<Building> GetMyBuildings()
    {
        return myBuildings;
    }

    public Color GetTeamColor()
    {
        return teamColor;
    }

    public Transform GetCameraTransform()
    {
        return cameraTransform;
    }

    public bool CanPlaceBuilding(BoxCollider buildingCollider, Vector3 point)
    {
        if (Physics.CheckBox(
            point + buildingCollider.center,
            buildingCollider.size / 2,
            Quaternion.identity,
            buildingBlockLayer))
        {
            return false;
        }

        foreach (Building b in myBuildings)
        {
            if ((point - b.transform.position).sqrMagnitude <= buildingRangeLimit * buildingRangeLimit)
            {
                return true;
            }
        }

        return false;
    }

    #region Server
    public override void OnStartServer()
    {
        Unit.ServerOnUnitSpawned += ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned += ServerHandleUnitDespawned;

        Building.ServerOnBuildingSpawned += ServerHandleBuildingSpawned;
        Building.ServerOnBuildingDespawned += ServerHandleBuildingDespawned;

        DontDestroyOnLoad(gameObject);
    }

    public override void OnStopServer()
    {
        Unit.ServerOnUnitSpawned -= ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned -= ServerHandleUnitDespawned;

        Building.ServerOnBuildingSpawned -= ServerHandleBuildingSpawned;
        Building.ServerOnBuildingDespawned -= ServerHandleBuildingDespawned;
    }

    [Server]
    public void SetDisplayName(string displayName)
    {
        this.displayName = displayName;
    }    

    [Server]
    public void SetPartyOwner(bool state)
    {
        isPartyOwner = state;
    }

    [Server]
    public void SetResources(int newResources)
    {
        resources = newResources;
    }

    [Server]
    public void SetTeamColor(Color newTeamColor)
    {
        teamColor = newTeamColor;
    }

    [Command]
    public void CmdStartGame()
    {
        if(!isPartyOwner) { return; }

        ((RTSNetworkManager)NetworkManager.singleton).StartGame();
    }

    [Command]
    public void CmdTryPlaceBuilding(int buildingId, Vector3 desiredLocation)
    {
        Building buildingToPlace = null;
        foreach (Building b in buildings)
        {
            if (b.GetId() == buildingId)
            {
                buildingToPlace = b;
                break;
            }
        }

        if (buildingToPlace == null) { return; }

        if (resources < buildingToPlace.GetPrice()) { return; }

        BoxCollider buildingCollider = buildingToPlace.GetComponent<BoxCollider>();

        if (!CanPlaceBuilding(buildingCollider, desiredLocation)) { return; }

        GameObject buildingInstance =
            Instantiate(buildingToPlace.gameObject, desiredLocation, buildingToPlace.transform.rotation);
        NetworkServer.Spawn(buildingInstance, connectionToClient);

        SetResources(resources - buildingToPlace.GetPrice());
    }

    private void ServerHandleUnitSpawned(Unit unit)
    {
        if (unit.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myUnits.Add(unit);
    }

    private void ServerHandleUnitDespawned(Unit unit)
    {
        if (unit.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myUnits.Remove(unit);
    }

    private void ServerHandleBuildingSpawned(Building building)
    {
        if (building.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myBuildings.Add(building);
    }

    private void ServerHandleBuildingDespawned(Building building)
    {
        if (building.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myBuildings.Remove(building);
    }

    #endregion


    #region Client

    public override void OnStartAuthority()
    {
        if (NetworkServer.active) { return; }

        Unit.ServerOnUnitSpawned += AuthorityHandleUnitSpawn;
        Unit.ServerOnUnitDespawned += AuthorityHandleUnitDespawn;

        Building.ServerOnBuildingSpawned += AuthorityHandleBuildingSpawn;
        Building.ServerOnBuildingDespawned += AuthorityHandleBuildingDespawn;
    }

    public override void OnStartClient()
    {
        if (NetworkServer.active) { return; }

        DontDestroyOnLoad(gameObject);

        ((RTSNetworkManager)NetworkManager.singleton).Players.Add(this);
    }

    public override void OnStopClient()
    {
        ClientOnInfoUpdated?.Invoke();

        if (!isClientOnly) { return; }

        ((RTSNetworkManager)NetworkManager.singleton).Players.Add(this);

        if (!hasAuthority) { return; }

        Unit.ServerOnUnitSpawned -= AuthorityHandleUnitSpawn;
        Unit.ServerOnUnitDespawned -= AuthorityHandleUnitDespawn;

        Building.ServerOnBuildingSpawned -= AuthorityHandleBuildingSpawn;
        Building.ServerOnBuildingDespawned -= AuthorityHandleBuildingDespawn;
    }

    private void ClientHandleResourcesUpdated(int oldValue, int newValue)
    {
        ClientOnResourcesUpdated?.Invoke(newValue);
    }

    private void AuthorityHandleUnitSpawn(Unit unit)
    {
        myUnits.Add(unit);
    }

    private void AuthorityHandleUnitDespawn(Unit unit)
    {
        myUnits.Remove(unit);
    }

    private void AuthorityHandleBuildingSpawn(Building building)
    {
        myBuildings.Add(building);
    }

    private void AuthorityHandleBuildingDespawn(Building building)
    {
        myBuildings.Remove(building);
    }

    private void AuthorityHandlePartyOwnerStateUpdated(bool oldState, bool newState)
    {
        if(!hasAuthority) { return; }

        AuthorityOnPartyOwnerStateUpdated?.Invoke(newState);
    }

    private void ClientHandleDisplayNameUpdated(string oldName, string newName)
    {
        ClientOnInfoUpdated?.Invoke();
    }

    #endregion
}
