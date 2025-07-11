using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A singleton dispatcher used to safely execute coroutines or actions on Unity's main thread
/// from other threads (e.g., networking, WebRTC callbacks).
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    // A thread-safe queue to store actions that need to be executed on the main thread
    private static readonly Queue<Action> ExecutionQueue = new();

    /// <summary>
    /// Enqueues a coroutine to be run on the main Unity thread.
    /// Useful when a background thread needs to run Unity-specific operations.
    /// </summary>
    public static void Enqueue(IEnumerator action)
    {
        lock (ExecutionQueue)
        {
            // Wrap the coroutine in an action that starts it using the Singleton instance
            ExecutionQueue.Enqueue(() => Singleton.StartCoroutine(action));
        }
    }

    // Singleton instance of the dispatcher
    public static MainThreadDispatcher Singleton;

    /// <summary>
    /// Ensures only one instance of the dispatcher exists and persists across scenes.
    /// </summary>
    void Awake()
    {
        if (Singleton == null) Singleton = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Executes all queued actions every frame on the main thread.
    /// </summary>
    void Update()
    {
        lock (ExecutionQueue)
        {
            while (ExecutionQueue.Count > 0)
            {
                var action = ExecutionQueue.Dequeue();
                action?.Invoke();
            }
        }
    }
}
