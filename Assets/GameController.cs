using UnityEngine;
using Unity.Simulation.Games;
using System;
using System.IO.Compression;
using UnityEngine.Networking;
using System.IO;

public class GameController : MonoBehaviour
{
    int SimpleCounter; //simple counter we want to measure in this simulation
    float timeToNextCounterIncrement; //used to track when the next counter increment happens
    float startTime;
    bool isReady = false;
    int ticks = 0;
    string CounterName = "Simple Counter"; //Our counter name
    float timePerCounterIncrement = 1f; //number of seconds between each counter increment
    float TimeOutSeconds = 120; //Finish simulation after 120 seconds or 2 minutes.
    int counterIncrementBy = 1; //How much to increment the counter by on each counter increment


    // Start is called before the first frame update
    void Start()
    {
        GameSimManager.Instance.FetchConfig(OnConfigFetched);
    }

    void OnConfigFetched(GameSimConfigResponse response)
    {
        Debug.Log("Got a config!");
        counterIncrementBy = response.GetInt("Increment Counter By");
        StartGame();
    }

    void StartGame()
    {
        isReady = true;
        timeToNextCounterIncrement = timePerCounterIncrement;
        startTime = Time.realtimeSinceStartup;
        Debug.Log("Seconds to run simulation: " + TimeOutSeconds);
        SimpleCounter = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (isReady)
        {
            timeToNextCounterIncrement -= Time.deltaTime;
            if (timeToNextCounterIncrement <= 0)
            {
                ticks++;
                timeToNextCounterIncrement += timePerCounterIncrement;
                OnTick();
            }
            if (Time.realtimeSinceStartup - startTime > TimeOutSeconds)
            {
                Quit();
            }
        }
    }

    void OnTick()
    {
        SimpleCounter += counterIncrementBy;
        Debug.Log(CounterName+": " + SimpleCounter);
        GameSimManager.Instance.SetCounter(CounterName, SimpleCounter);
    }

    void Quit()
    {
        
        Int64 CompletionTime = (Int64)(Time.realtimeSinceStartup - startTime);
        Debug.Log("Final " + CounterName + ": " + SimpleCounter);
        Debug.Log("Completion Time in Seconds: " + CompletionTime);
        GameSimManager.Instance.SetCounter("Completion Time", CompletionTime);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        Debug.Log("Quit running");
    }
}
