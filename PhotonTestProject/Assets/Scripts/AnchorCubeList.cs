using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnchorCubeList
{
  public AnchorCubeList()
  {
    this.anchorCubePositionListMap = new Dictionary<string, List<Vector3>>();
  }
  public void AddCubeWaitingForAnchor(string anchorId, Vector3 relativePosition)
  {
    if (!this.anchorCubePositionListMap.ContainsKey(anchorId))
    {
      this.anchorCubePositionListMap[anchorId] = new List<Vector3>();
    }
    this.anchorCubePositionListMap[anchorId].Add(relativePosition);
  }
  public List<Vector3> GetCubesWaitingForAnchor(string anchorId)
  {
    List<Vector3> cubes = null;

    if (this.anchorCubePositionListMap.ContainsKey(anchorId))
    {
      cubes = this.anchorCubePositionListMap[anchorId];
      this.anchorCubePositionListMap.Remove(anchorId);
    }
    return (cubes);
  }
  Dictionary<string, List<Vector3>> anchorCubePositionListMap;
}
