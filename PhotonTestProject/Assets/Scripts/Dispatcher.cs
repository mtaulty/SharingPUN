using System;
using System.Threading;
using UnityEngine;

public static class DispatcherGameObjectExtensions
{
  public static void Dispatch(this GameObject gameObject,
    Action action)
  {
    gameObject.GetComponent<Dispatcher>().Dispatch(action);
  }
}
public class Dispatcher : MonoBehaviour
{
  void Start()
  {
    // TODO: Not entirely happy about using a managed thread id as I think
    // at least in theory they can move around.
    this.startingThreadId = Thread.CurrentThread.ManagedThreadId;
  }
  public void Dispatch(Action action)
  {
    if (Thread.CurrentThread.ManagedThreadId != this.startingThreadId)
    {
      UnityEngine.WSA.Application.InvokeOnAppThread(
        () => action(), false);
    }
    else
    {
      action();
    }
  }
  int startingThreadId;
}