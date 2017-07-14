using HUX.Focus;
using System;
using UnityEngine;

public class Root : MonoBehaviour
{
  public Root()
  {
    this.anchorPositionList = new AnchorPositionList();
    this.anchorCubeList = new AnchorCubeList();
    this.sessionId = Guid.NewGuid();
  }
  void Start()
  {
    var roomJoiner = this.GetComponent<PhotonRoomJoiner>();
    roomJoiner.JoinedRoom += JoinedRoomEventHandler;
  }
  void JoinedRoomEventHandler(object sender, EventArgs e)
  {
    var roomJoiner = this.GetComponent<PhotonRoomJoiner>();
    roomJoiner.JoinedRoom -= this.JoinedRoomEventHandler;

    Debug.Log("Room has been joined on the server");
    Debug.Log("Room was created on server? " + roomJoiner.HasCreatedRoom);
  }
  public void OnVoiceCommandCreateCube()
  {
    Debug.Log("Got voice command to create a cube");

    // Get a point 3m in front of where the user is looking.
    var position = FocusManager.Instance.GazeFocuser.FocusRay.GetPoint(3.0f);

    // See if we have a world anchor within range of this?
    var anchor = this.anchorPositionList.GetAnchorWithinRangeOfPosition(position);

    if (anchor == null)
    {
      Debug.Log("No world anchor in range - creating anchor");
      this.CreateAndExportWorldAnchoredParent(position);
    }
    else
    {
      Debug.Log("Existing world anchor in range - adding cube");
      var relativePosition = position - anchor.transform.position;
      this.AddCubeToAnchoredParent(anchor, relativePosition, true);
    }
  }
  void CreateAndExportWorldAnchoredParent(Vector3 position)
  {
    // Create an empty game object to act as our anchor and put it
    // on a list.
    var anchor = this.anchorPositionList.CreateAnchor(
      this.gameObject, position);

    // World anchor that object, export the data from that.
    SpatialAnchorHelper.AddAndExportWorldAnchorForGameObject(anchor,
      bits =>
      {
        Debug.Log("Exported world anchor - byte array is null?" + (bits == null));

        // Upload those bits for that world anchor to Azure blob storage.
        if (bits != null)
        {
          this.gameObject.Dispatch(
            () =>
            {
              this.UploadWorldAnchorToStorage(anchor, bits);
            }
          );
        }
      }
    );
  }
  void UploadWorldAnchorToStorage(GameObject anchor, byte[] bits)
  {
    var blobService = this.gameObject.GetComponent<AzureBlobStorageHelper>();

    Debug.Log("Uploading world anchor byte array to Azure blob storage");

    blobService.UploadWorldAnchorBlob(
      anchor.name,
      bits,
      result =>
      {
        Debug.Log("Upload succeeded?" + result);

        if (result)
        {
          // Use an RPC to notify other devices that a new world anchor
          // has been created & uploaded.
          var photonView = PhotonView.Get(this.gameObject);

          // NB: Using "AllBuffered" here in the hope that if device A rejoins
          // a room it will get its own RPCs replayed to it which doesn't seem
          // to happen if I use OtherBuffered.
          photonView.RPC(
            "WorldAnchorCreatedRemotely",
            PhotonTargets.AllBuffered,
            this.sessionId.ToString(),
            anchor.name);

          // Finally, create the cube as a child of the anchor.
          this.AddCubeToAnchoredParent(anchor, Vector3.zero, true);
        }
      }
    );
  }
  void AddCubeToAnchoredParent(
    GameObject anchor,
    Vector3 relativePosition,
    bool createdLocally)
  {
    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.transform.parent = anchor.transform;
    cube.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
    cube.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
    cube.transform.localPosition = relativePosition;

    // If this is a cube we've just made on this device then notify other
    // devices that this has happened via RPC.
    if (createdLocally)
    {
      var photonView = PhotonView.Get(this.gameObject);

      // NB: Using "AllBuffered" here in the hope that if device A rejoins
      // a room it will get its own RPCs replayed to it which doesn't seem
      // to happen if I use OtherBuffered.
      photonView.RPC(
        "CubeCreatedRemotely",
        PhotonTargets.AllBuffered,
        this.sessionId.ToString(),
        anchor.name,
        relativePosition);
    }
  }
  void DrainPendingCubesForAnchor(string anchorId)
  {
    // Have we got any cubes that we were asked to create before their
    // world anchor parent arrived over the network?
    var cubes = this.anchorCubeList.GetCubesWaitingForAnchor(anchorId);

    if (cubes != null)
    {
      var anchor = this.anchorPositionList.GetAnchorById(anchorId);

      foreach (var cube in cubes)
      {
        this.AddCubeToAnchoredParent(anchor, cube, false);
      }
    }
  }
  [PunRPC]
  void WorldAnchorCreatedRemotely(string sessionId, string anchorId)
  {
    if (sessionId != this.sessionId.ToString())
    {
      Debug.Log("Received RPC for remote world anchor creation");

      // Download the bits representing the anchor
      var blobService = this.gameObject.GetComponent<AzureBlobStorageHelper>();

      blobService.DownloadWorldAnchorBlob(anchorId,
        bits =>
        {
          Debug.Log("Downloaded world anchor blob from Azure - null? " + (bits == null));

          if (bits != null)
          {
            // Create the new, empty game object to represent it, leaving
            // this until after the download to reduce the big race window
            // that I have here!
            var anchor = this.anchorPositionList.CreateAnchor(
                  this.gameObject,
                  Vector3.zero,
                  anchorId);

            Debug.Log("Importing world anchor blob");

            // Now import the world anchor onto that game object
            SpatialAnchorHelper.ImportWorldAnchorToGameObject(
              anchor,
              bits,
              succeeded =>
              {
                Debug.Log("Import complete - succeeded? " + succeeded);

                if (succeeded)
                {
                  // Add any child cubes that arrived before the world anchor
                  // arrived.
                  this.gameObject.Dispatch(
                    () => this.DrainPendingCubesForAnchor(anchor.name));
                }
              }
            );
          }
        }
      );
    }
  }
  [PunRPC]
  void CubeCreatedRemotely(string sessionId, string anchorId, Vector3 relativePosition)
  {
    if (sessionId != this.sessionId.ToString())
    {
      Debug.Log("Received RPC for remote cube creation");

      var anchor = this.anchorPositionList.GetAnchorById(anchorId);

      // Seems like there's a reasonable chance that the cube may well
      // arrive before the world anchor has downloaded.
      if (anchor != null)
      {
        Debug.Log("Anchor for cube already present, adding cube");

        // The easier case, the anchor is already here.
        this.AddCubeToAnchoredParent(anchor, relativePosition, false);
      }
      else
      {
        Debug.Log("Anchor for cube not already present, queueing cube for later");

        // Keep the cube on a list for when the anchor shows up.
        this.anchorCubeList.AddCubeWaitingForAnchor(anchorId, relativePosition);
      }
    }
  }
  // NB: Using this because when I use the OthersBuffered option on the RPC
  // I find that DeviceA does not seem to get DeviceA's buffered RPCs when
  // it joins a session.
  Guid sessionId;
  AnchorPositionList anchorPositionList;
  AnchorCubeList anchorCubeList;
}