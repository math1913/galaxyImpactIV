using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class ApiConfig
{
    private const string ConfigFileName = "api-config.json";

    private static ApiConfigData cachedConfig;

    public static string BaseUrl
    {
        get
        {
            ApiConfigData config = Config;

            if (!string.IsNullOrWhiteSpace(config.baseUrl))
                return config.baseUrl.TrimEnd('/');

            string protocol = TrimSlashes(config.protocol).ToLowerInvariant();
            string host = config.serverHost.Trim();
            string port = config.serverPort > 0 ? $":{config.serverPort}" : string.Empty;
            string apiPath = TrimSlashes(config.apiPath);

            return string.IsNullOrEmpty(apiPath)
                ? $"{protocol}://{host}{port}"
                : $"{protocol}://{host}{port}/{apiPath}";
        }
    }

    public static string AuthLoginUrl => BuildUrl("auth", "login");
    public static string UsersUrl => BuildUrl("users");
    public static string AchievementsUrl => BuildUrl("achievements");

    public static string UserUrl(int userId)
    {
        return BuildUrl("users", userId.ToString());
    }

    public static string UserUpdateStatsUrl(int userId)
    {
        return BuildUrl("users", userId.ToString(), "updateStats");
    }

    public static string BuildUrl(params string[] segments)
    {
        StringBuilder url = new StringBuilder(BaseUrl.TrimEnd('/'));

        foreach (string segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
                continue;

            url.Append('/');
            url.Append(segment.Trim('/'));
        }

        return url.ToString();
    }

    public static void Reload()
    {
        cachedConfig = null;
    }

    private static ApiConfigData Config
    {
        get
        {
            if (cachedConfig == null)
                cachedConfig = LoadConfig();

            return cachedConfig;
        }
    }

    private static ApiConfigData LoadConfig()
    {
        ApiConfigData config = new ApiConfigData();
        string configPath = Path.Combine(Application.streamingAssetsPath, ConfigFileName);

        if (!File.Exists(configPath))
            return config;

        try
        {
            ApiConfigData fileConfig = JsonUtility.FromJson<ApiConfigData>(File.ReadAllText(configPath));

            if (fileConfig == null)
                return config;

            if (!string.IsNullOrWhiteSpace(fileConfig.baseUrl))
                config.baseUrl = fileConfig.baseUrl;

            if (!string.IsNullOrWhiteSpace(fileConfig.protocol))
                config.protocol = fileConfig.protocol;

            if (!string.IsNullOrWhiteSpace(fileConfig.serverHost))
                config.serverHost = fileConfig.serverHost;

            if (fileConfig.serverPort > 0)
                config.serverPort = fileConfig.serverPort;

            if (!string.IsNullOrWhiteSpace(fileConfig.apiPath))
                config.apiPath = fileConfig.apiPath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"No se pudo leer {configPath}: {ex.Message}");
        }

        return config;
    }

    private static string TrimSlashes(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Trim('/');
    }

    [Serializable]
    private class ApiConfigData
    {
        public string baseUrl = string.Empty;
        public string protocol = "http";
        public string serverHost = "127.0.0.1";
        public int serverPort = 8080;
        public string apiPath = "api";
    }
}
