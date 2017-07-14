using Photon;
using System;
using UnityEngine;

public class PhotonRoomJoiner : PunBehaviour
{
  public event EventHandler JoinedRoom;

  [SerializeField]
  bool useNetworkConnector = true;

  [SerializeField]
  string roomName = "Default Room";

  public bool HasCreatedRoom { get; private set; }

  void Start()
  {
    this.pendingCreateOrJoin = !this.useNetworkConnector;

    if (this.useNetworkConnector)
    {
      var networkConnector = this.gameObject.GetComponent<PhotonConnector>();
      networkConnector.FirstConnection += FirstConnectionEventHandler;
    }
  }
  void FirstConnectionEventHandler(object sender, System.EventArgs e)
  {
    var networkConnector = this.gameObject.GetComponent<PhotonConnector>();
    networkConnector.FirstConnection -= this.FirstConnectionEventHandler;
    this.pendingCreateOrJoin = true;
  }
  void Update()
  {
    if (this.pendingCreateOrJoin)
    {
      this.pendingCreateOrJoin = false;

      Debug.Log("Attempting to join or create room " + this.roomName);

      // Creating the room should fail if it already exists according to
      // the docs.
      PhotonNetwork.JoinOrCreateRoom(
        this.roomName,
        new RoomOptions()
        {
          IsOpen = true,
          IsVisible = true,
          CleanupCacheOnLeave = false,
          EmptyRoomTtl = EMPTY_ROOM_TTL_MS,
          PlayerTtl = PLAYER_TTL_MS          
        },
        TypedLobby.Default
      );
    }
  }
  public override void OnCreatedRoom()
  {
    Debug.Log("Room created");

    this.HasCreatedRoom = true;
    base.OnCreatedRoom();
  }
  public override void OnJoinedRoom()
  {
    Debug.Log("Room joined");

    this.FireJoinedRoom(true);
    base.OnJoinedRoom();
  }
  void FireJoinedRoom(bool joinedRoom)
  {
    if (this.JoinedRoom != null)
    {
      this.JoinedRoom(this, EventArgs.Empty);
    }
  }
  bool pendingCreateOrJoin;
  const int PLAYER_TTL_MS = 250;
  const int EMPTY_ROOM_TTL_MS = 3 * 60 * 1000;
}