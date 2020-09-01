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
        Debug.Log(string.Format("Export path: {0}", exportPath));

        // TODO - replace UNITY_PROJECT_ID with https://docs.unity3d.com/Manual/UnityCloudBuildManifest.html
        var unityProjectId = Environment.GetEnvironmentVariable("UNITY_PROJECT_ID");
        var username = Environment.GetEnvironmentVariable("UNITY_USERNAME");
        var password = Environment.GetEnvironmentVariable("UNITY_PASSWORD");

        // zip
        Debug.Log("Zipping...");
        var zipPath = Path.Combine(Path.GetTempPath(), "upload.zip");
        Debug.Log(string.Format("Zip path: {0}", zipPath));
        ZipFile.CreateFromDirectory(Path.GetDirectoryName(exportPath), zipPath);
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

            Execute(request);
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return;
            }

            var response = request.downloadHandler.text;
            var loginResponse = JsonUtility.FromJson<LoginResponse>(response);
            accessToken = loginResponse.access_token;
        }
        Debug.Log("Done.");

        // get upload path
        Debug.Log("Creating Game Simulation build...");
        BuildResponse buildResponse;
        var buildsUrl = string.Format("https://api.prd.gamesimulation.unity3d.com/v1/builds?projectId={0}", unityProjectId);
        var buildRequest = new BuildRequest("SampleTestProject", "");
        var buildJson = JsonUtility.ToJson(buildRequest);
        using (var request = UnityWebRequest.Post(buildsUrl, buildJson))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", string.Format("Bearer {0}", accessToken));

            Execute(request);
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return;
            }

            var response = request.downloadHandler.text;
            buildResponse = JsonUtility.FromJson<BuildResponse>(response);
        }
        Debug.Log("Done.");


        Debug.Log(string.Format("Build upload: {0}", buildResponse.id));

        // upload build
        Debug.Log("Uploading Game Simulation build...");
        using (var request = new UnityWebRequest(buildResponse.upload_uri, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerFile(zipPath);

            Execute(request);
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return;
            }

        }
        Debug.Log("Done.");

        Debug.Log("Creating Game Simulation...");
        var simulationsUrl = string.Format("https://api.prd.gamesimulation.unity3d.com/v1/jobs?projectId={0}", unityProjectId);
        // TODO - load request from json asset
        var simulationRequest = new SimulationRequest();
        simulationRequest.jobName = "Sample Test Project Simulation";
        simulationRequest.buildId = buildResponse.id;
        simulationRequest.decisionEngineMetadata = new DecisionEngineMetadata();
        simulationRequest.decisionEngineMetadata.engineType = "gridsearch";
        var setting = new Setting();
        setting.key = "Increment Counter By";
        setting.type = "int";
        setting.values = new string[] { "1", "2", "3", "4" };
        simulationRequest.decisionEngineMetadata.settings = new Setting[] { setting };
        simulationRequest.maxRuntimeSeconds = "300";
        simulationRequest.runsPerParamCombo = 10;
        var simulationJson = JsonUtility.ToJson(simulationRequest);
        using (var request = UnityWebRequest.Post(simulationsUrl, simulationJson))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", string.Format("Bearer {0}", accessToken));

            Execute(request);
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return;
            }

            var response = request.downloadHandler.text;
            var simulationResponse = JsonUtility.FromJson<SimulationResponse>(response);
            Debug.Log(string.Format("Simulation id: {0}", simulationResponse.id));
        }
        Debug.Log("Done.");
    }

    public static void Execute(UnityWebRequest request) 
    {
        request.SendWebRequest();
        while (request.downloadProgress < 1.0f)
        {
            System.Threading.Thread.Sleep(100);
        }
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