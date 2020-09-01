using UnityEngine;
using System;
using System.IO.Compression;
using UnityEngine.Networking;
using System.IO;

#if true || UNITY_CLOUD_BUILD
public class Build {
    const string ZIP_NAME = "upload.zip";

    const string GAME_SIMULATION_API_HOST = "https://api.prd.gamesimulation.unity3d.com";
    const string UNITY_API_HOST = "https://api.unity.com";
    const string UNITY_API_LOGIN_REL_PATH = "/v1/core/api/login";

    public static void PostExport(string exportPath)
    {
        Debug.Log(string.Format("Export path: {0}", exportPath));

        // TODO - replace UNITY_PROJECT_ID with https://docs.unity3d.com/Manual/UnityCloudBuildManifest.html
        var unityProjectId = Environment.GetEnvironmentVariable("UNITY_PROJECT_ID");
        var username = Environment.GetEnvironmentVariable("UNITY_USERNAME");
        var password = Environment.GetEnvironmentVariable("UNITY_PASSWORD");

        Debug.Log("Zipping...");
        string zipPath = ZipBuild(exportPath);
        Debug.Log("Done.");

        Debug.Log(string.Format("Zip path: {0}", zipPath));

        Debug.Log("Getting token...");
        string accessToken = GetAccessToken(username, password);
        Debug.Log("Done.");

        Debug.Log("Creating Game Simulation build...");
        BuildResponse buildResponse = CreateGameSimulationBuild(unityProjectId, accessToken);
        Debug.Log("Done.");

        Debug.Log(string.Format("Build upload: {0}", buildResponse.id));

        Debug.Log("Uploading Game Simulation build...");
        UploadGameSimulationBuild(zipPath, buildResponse);
        Debug.Log("Done.");

        Debug.Log("Creating Game Simulation...");
        var simulationResponse = CreateGameSimulation(unityProjectId, accessToken, buildResponse);
        Debug.Log("Done.");

        Debug.Log(string.Format("Simulation id: {0}", simulationResponse.id));
}

    private static string ZipBuild(string exportPath)
    {
        var zipPath = Path.Combine(Path.GetTempPath(), ZIP_NAME);
        ZipFile.CreateFromDirectory(Path.GetDirectoryName(exportPath), zipPath);
        return zipPath;
    }

    private static string GetAccessToken(string username, string password)
    {
        var loginUrl = string.Format("{0}{1}", UNITY_API_HOST, UNITY_API_LOGIN_REL_PATH);
        var loginRequest = new LoginRequest(username, password);

        using (var request = CreatePostJsonRequest(loginUrl, loginRequest))
        {
            var loginResponse = Execute<LoginResponse>(request);
           
            return loginResponse.access_token;
        }
    }

    private static BuildResponse CreateGameSimulationBuild(string unityProjectId, string accessToken)
    {
        var buildsUrl = string.Format("{0}/v1/builds?projectId={1}", GAME_SIMULATION_API_HOST, unityProjectId);
        var buildRequest = new BuildRequest("SampleTestProject", "");

        using (var request = CreatePostJsonRequest(buildsUrl, buildRequest))
        {
            SetAuthorizationHeader(request, accessToken);

            return Execute<BuildResponse>(request);
        }
    }

    private static void UploadGameSimulationBuild(string zipPath, BuildResponse buildResponse)
    {
        using (var request = new UnityWebRequest(buildResponse.upload_uri, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerFile(zipPath);

            Execute(request);
        }
    }

    private static SimulationResponse CreateGameSimulation(string unityProjectId, string accessToken, BuildResponse buildResponse)
    {
        var simulationsUrl = string.Format("{0}/v1/jobs?projectId={1}", GAME_SIMULATION_API_HOST, unityProjectId);

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

        using (var request = CreatePostJsonRequest(simulationsUrl, simulationRequest))
        {
            SetAuthorizationHeader(request, accessToken);

            return Execute<SimulationResponse>(request);
        }
    }

    public static UnityWebRequest CreatePostJsonRequest(string url, object body)
    {
        var json = JsonUtility.ToJson(body);
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        SetJsonHeaders(request);
        return request;
    }

    public static void SetJsonHeaders(UnityWebRequest request)
    {
        const string APPLICATION_JSON = "application/json";

        request.SetRequestHeader("Content-Type", APPLICATION_JSON);
        request.SetRequestHeader("Accept", APPLICATION_JSON);
    }

    public static void SetAuthorizationHeader(UnityWebRequest request, string accessToken)
    {
        request.SetRequestHeader("Authorization", string.Format("Bearer {0}", accessToken));
    }

    public static T Execute<T>(UnityWebRequest request)
    {
        Execute(request);
        var response = request.downloadHandler.text;
        return JsonUtility.FromJson<T>(response);
    }

    public static void Execute(UnityWebRequest request) 
    {
        request.SendWebRequest();

        // HACK - typically we would yield the SendWebRequest response
        //  and this method would be executed as a coroutine.
        while (!request.isDone)
        {
            System.Threading.Thread.Sleep(100);
        }

        if (request.isNetworkError || request.isHttpError)
        {
            throw new Exception(request.error);
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