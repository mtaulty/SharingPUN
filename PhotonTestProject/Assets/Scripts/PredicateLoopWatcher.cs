using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_UWP && !UNITY_EDITOR
using System.Threading.Tasks;
#endif

public class PredicateLoopWatcher : MonoBehaviour
{
#if UNITY_UWP && !UNITY_EDITOR
  public PredicateLoopWatcher()
  {
    this.completed = new TaskCompletionSource<bool>();
  }
  public Func<bool> Predicate { get; set; }

  void Update()
  {
    if ((this.Predicate != null) && this.Predicate())
    {
      this.completed.SetResult(true);
    }
  }
  public async static Task WaitForPredicateAsync(
    GameObject gameObject,
    Func<bool> predicate)
  {
    var component = gameObject.AddComponent<PredicateLoopWatcher>();
    component.Predicate = predicate;
    await component.completed.Task;
    Destroy(component);
  }
  TaskCompletionSource<bool> completed;
#else
  public PredicateLoopWatcher()
  {

  }
#endif
}
