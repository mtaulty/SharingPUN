using Photon;
using System;
using UnityEngine;

public class PhotonConnector : PunBehaviour
{
  public event EventHandler FirstConnection;

  [SerializeField]
  string ApplicationVersion = "1.0";
  
  // Use this for initialization
  void Start()
  {
    Debug.Log("Connecting to Master Server");
    PhotonNetwork.ConnectUsingSettings(this.ApplicationVersion);
  }
  public override void OnConnectedToMaster()
  {
    Debug.Log("Connected to Master Server");
    if (this.FirstConnection != null)
    {
      this.FirstConnection(this, EventArgs.Empty);
    }
    base.OnConnectedToMaster();
  }
  public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
  {
    base.OnPhotonPlayerConnected(newPlayer);
  }
}
