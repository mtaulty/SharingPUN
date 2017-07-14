using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnchorPositionList
{
  public AnchorPositionList()
  {
    this.anchors = new List<GameObject>();
  }
  public GameObject GetAnchorWithinRangeOfPosition(Vector3 position)
  {
    var entry = this.anchors
      .Select(
        a => new { Anchor = a, Distance = Vector3.Distance(a.transform.position, position) })
      .OrderBy(
        a => a.Distance)
      .FirstOrDefault();

    return (entry == null ? null : entry.Anchor);
  }
  public GameObject GetAnchorById(string id)
  {
    return (this.anchors.FirstOrDefault(a => a.name == id));
  }
  public GameObject CreateAnchor(GameObject parent, Vector3 position,
    string id = null)
  {
    // Make an empty game object.
    var anchor = new GameObject();

    if (id == null)
    {
      id = "anchor" + Guid.NewGuid().ToString();
    }

    anchor.name = id;
    anchor.transform.parent = parent.transform;
    anchor.transform.Translate(position, Space.Self);

    this.anchors.Add(anchor);

    return (anchor);
  }
  public void RemoveAnchor(GameObject anchor)
  {
    this.anchors.Remove(anchor);
  }
  List<GameObject> anchors;
  const float maxAnchorDistance = 3.0f;
}