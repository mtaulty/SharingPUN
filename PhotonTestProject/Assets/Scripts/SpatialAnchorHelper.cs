using System.IO;
using UnityEngine;
using UnityEngine.VR.WSA;
using UnityEngine.VR.WSA.Sharing;
using System.Linq;
using System;

#if UNITY_UWP && !UNITY_EDITOR
using System.Threading.Tasks;
#endif // UNITY_UWP

public static class SpatialAnchorHelper
{
  public static
#if UNITY_UWP && !UNITY_EDITOR
    async
#endif
    void ImportWorldAnchorToGameObject(
    GameObject gameObject,
    byte[] worldAnchorBits,
    Action<bool> callback)
  {
#if UNITY_UWP && !UNITY_EDITOR
    var result = await ImportWorldAnchorToGameObjectAsync(gameObject, worldAnchorBits);

    if (callback != null)
    {
      callback(result);
    }
#endif
  }

  public static 
#if UNITY_UWP && !UNITY_EDITOR
    async
#endif
    void AddAndExportWorldAnchorForGameObject(
      GameObject gameObject,
      Action<byte[]> callback)
  {
#if UNITY_UWP && !UNITY_EDITOR
    var result = await AddAndExportWorldAnchorForGameObjectAsync(gameObject);
    callback(result);
#endif
  }

#if UNITY_UWP && !UNITY_EDITOR
  public static async Task<bool> ImportWorldAnchorToGameObjectAsync(
    GameObject gameObject,
    byte[] worldAnchorBits)
  {
    var completion = new TaskCompletionSource<bool>();
    bool worked = false;

    Debug.Log("Importing spatial anchor...");

    WorldAnchorTransferBatch.ImportAsync(worldAnchorBits,
      (reason, batch) =>
      {
        Debug.Log("Import completed - succeeded? " +
          (reason == SerializationCompletionReason.Succeeded));

        if (reason == SerializationCompletionReason.Succeeded)
        {
          Debug.Log("Attempting to look into world anchor batch");

          var anchorId = batch.GetAllIds().FirstOrDefault();

          Debug.Log("Anchor id found? " + (anchorId != null));

          if (!string.IsNullOrEmpty(anchorId))
          {
            Debug.Log("Locking world anchor");

            batch.LockObject(anchorId, gameObject);
            worked = true;
          }
        }
        batch.Dispose();
        completion.SetResult(true);
      }
    );
    await completion.Task;

    return (worked);
  }
  public static async Task<byte[]> AddAndExportWorldAnchorForGameObjectAsync(
    GameObject gameObject)
  {
    var worldAnchor = gameObject.AddComponent<WorldAnchor>();
    byte[] bits = null;

    Debug.Log("Waiting for world anchor to be located");

    await PredicateLoopWatcher.WaitForPredicateAsync(
      gameObject,
      () => worldAnchor.isLocated);

    using (var worldAnchorBatch = new WorldAnchorTransferBatch())
    {
      worldAnchorBatch.AddWorldAnchor("anchor", worldAnchor);

      var completion = new TaskCompletionSource<bool>();

      using (var memoryStream = new MemoryStream())
      {
        Debug.Log("Exporting world anchor...");

        WorldAnchorTransferBatch.ExportAsync(
          worldAnchorBatch,
          data =>
          {
            memoryStream.Write(data, 0, data.Length);
          },
          reason =>
          {
            Debug.Log("Export completed - succeeded? " +
              (reason == SerializationCompletionReason.Succeeded));

            if (reason != SerializationCompletionReason.Succeeded)
            {
              bits = null;
            }
            else
            {
              bits = memoryStream.ToArray();
            }
            completion.SetResult(bits != null);
          }
        );
        await completion.Task;
      }
    }
    return (bits);
  }
#endif // UNITY_UWP
}
