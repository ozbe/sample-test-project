using UnityEngine;
using Unity.Simulation.Games;
using System;
using System.IO.Compression;
using UnityEngine.Networking;
using System.IO;

#if true || UNITY_CLOUD_BUILD
public class Build {
    public static void PostExport(string exportPath)
    {
        var unityProjectId = Environment.GetEnvironmentVariable("UNITY_PROJECT_ID");
        var username = Environment.GetEnvironmentVariable("UNITY_USERNAME");
        var password = Environment.GetEnvironmentVariable("UNITY_PASSWORD");

        // zip
        Debug.Log("Zipping...");
        var zipPath = Path.Combine(Path.GetDirectoryName(exportPath), "upload.zip");
        ZipFile.CreateFromDirectory(exportPath, zipPath);
        Debug.Log("Done.");

        // get token
        Debug.Log("Getting token...");
        string accessToken;
        var loginRequest = new LoginRequest(username, password);
        var loginJson = JsonUtility.ToJson(loginRequest);
        using (var request = UnityWebRequest.Post("https://api.unity.com/v1/core/api/login", loginJson))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.Send();
            var response = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
            var loginResponse = JsonUtility.FromJson<LoginResponse>(response);
            accessToken = loginResponse.access_token;
        }
        Debug.Log("Done.");

        // get upload path
        Debug.Log("Creating Game Simulation build...");
        BuildResponse buildResponse;
        var buildsUrl = string.Format("https://api.prd.gamesimulation.unity3d.com/v1/builds?projectId={0}", unityProjectId);
        var buildRequest = new BuildRequest("Foo", "Bar");
        var buildJson = JsonUtility.ToJson(buildRequest);
        using (var request = UnityWebRequest.Post(buildsUrl, buildJson))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", string.Format("Bearer {0}", accessToken));
            request.Send();
            var response = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
            buildResponse = JsonUtility.FromJson<BuildResponse>(response);
        }
        Debug.Log("Done.");


        Debug.Log(string.Format("Build upload: {0}", buildResponse.id));

        // upload build
        Debug.Log("Uploading Game Simulation build...");
        using (var request = new UnityWebRequest(buildResponse.upload_uri, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerFile(zipPath);
            request.Send();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return;
            }

        }
        Debug.Log("Done.");

        Debug.Log("Creating Game Simulation...");
        var simulationsUrl = string.Format("https://api.prd.gamesimulation.unity3d.com/v1/jobs?projectId={0}", unityProjectId);
        var simulationRequest = new SimulationRequest();
        simulationRequest.jobName = "All work and no play";
        simulationRequest.buildId = buildResponse.id;
        simulationRequest.decisionEngineMetadata = new DecisionEngineMetadata();
        simulationRequest.decisionEngineMetadata.engineType = "gridsearch";
        var setting = new Setting();
        setting.key = "Increment Counter By";
        setting.type = "int";
        setting.values = new string[] { "1", "2", "3", "4" };
        simulationRequest.decisionEngineMetadata.settings = new Setting[] { setting };
        simulationRequest.maxRuntimeSeconds = "300";
        simulationRequest.runsPerParamCombo = 1;
        var simulationJson = JsonUtility.ToJson(simulationRequest);
        using (var request = UnityWebRequest.Post(simulationsUrl, simulationJson))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", string.Format("Bearer {0}", accessToken));
            request.Send();
            var response = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
            var simulationResponse = JsonUtility.FromJson<SimulationResponse>(response);
            Debug.Log(string.Format("Simulation id: {0}", simulationResponse.id));
        }
        Debug.Log("Done.");
    }

    [Serializable]
    struct LoginRequest
    {
        public LoginRequest(string username, string password)
        {
            this.username = username;
            this.password = password;
            this.grant_type = "PASSWORD";
        }
        public string username;
        public string password;
        public string grant_type;
    }

    [Serializable]
    struct LoginResponse
    {
        public string access_token;
    }

    [Serializable]
    struct BuildRequest 
    {
        public BuildRequest(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
        public string name;
        public string description;
    }

    [Serializable]
    struct BuildResponse
    {
        public string id;
        public string upload_uri;
    }

    [Serializable]
    struct SimulationRequest
    {
        public string jobName;
        public string buildId;
        public DecisionEngineMetadata decisionEngineMetadata;
        public string maxRuntimeSeconds;
        public int runsPerParamCombo;
    }

    [Serializable]
    struct DecisionEngineMetadata
    {
        public string engineType;
        public Setting[] settings;
    }

    [Serializable]
    struct Setting
    {
        public string key;
        public string type;
        public string[] values;
    }

    [Serializable]
    struct SimulationResponse
    {
        public string id;
    }
}
#endif

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
