using System;
using System.Collections;
using System.Net;
using Unity3dAzure.StorageServices;
using UnityEngine;

public class AzureBlobStorageHelper : MonoBehaviour
{
  [SerializeField]
  string AzureStorageAccountName;

  [SerializeField]
  string AzureStorageAccessKey;

  [SerializeField]
  string AzureStorageContainerName;

  public void UploadWorldAnchorBlob(string identifier, byte[] bits, Action<bool> callback)
  {
    Debug.Log(string.Format("Uploading {0:G2}MB blob to Azure",
      bits.Length / (1024 * 1024)));

    var blobService = this.MakeBlobService();

    StartCoroutine(
      blobService.PutImageBlob(
        r =>
        {
          Debug.Log("Upload complete - succeeded? " + !r.IsError);
          callback(!r.IsError);
        },
        bits,
        this.AzureStorageContainerName,
        identifier,
        contentType));
  }
  public void DownloadWorldAnchorBlob(string identifier,
    Action<byte[]> callback)
  {
    Debug.Log("Downloading blob from Azure storage");

    // The blob service doesn't seem to include a method to just download 
    // a blob as a byte[] so doing it here out of the pieces that the
    // service uses internally.
    var client = this.MakeStorageClient();

    string url = UrlHelper.BuildQuery(
      client.PrimaryEndpoint(), 
      string.Empty, 
      this.AzureStorageContainerName + "/" + identifier);

    StorageRequest request = new StorageRequest(url, Method.GET);

    StartCoroutine(this.DownloadBlob(request, callback));
  }
  IEnumerator DownloadBlob(StorageRequest request, Action<byte[]> callback)
  {
    yield return request.request.Send();
    request.Result(
      response =>
      {
        Debug.Log("Download completed - succeeded? " + !response.IsError);

        byte[] bits = null;

        if (!response.IsError)
        {
          bits = request.request.downloadHandler.data;
          Debug.Log(string.Format("Downloaded {0:G2}MB blob from Azure",
            bits.Length / (1024 * 1024)));
        }
        callback(bits);
      }
    );
  }
  StorageServiceClient MakeStorageClient()
  {
    // Not keeping these objects around to try to avoid sharing them across
    // multiple, re-entrant/concurrent usages.
    return (new StorageServiceClient(
      this.AzureStorageAccountName,
      this.AzureStorageAccessKey
      )
    );
  }

  BlobService MakeBlobService()
  {
    // Not keeping these objects around to try to avoid sharing them across
    // multiple, re-entrant/concurrent usages.
    return (new BlobService(this.MakeStorageClient()));
  }
  static readonly string contentType = "application/octet-stream";
}
