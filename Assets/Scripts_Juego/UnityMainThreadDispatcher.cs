using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance;
    private static readonly Queue<Action> executionQueue = new Queue<Action>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null)
        {
            instance = new GameObject("UnityMainThreadDispatcher").AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(instance.gameObject);
        }
    }

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            Initialize();
        }
        return instance;
    }

    void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }
}