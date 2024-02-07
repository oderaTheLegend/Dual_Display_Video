using UnityEngine;
using System.IO;

/// <summary>
/// Represents the configuration settings for video playback.
/// </summary>
[System.Serializable]
public class VideoConfig
{
    [Tooltip("Duration in seconds to wait for inactivity before showing the static screen.")]
    public float inactivityDuration = 30f;

    [Tooltip("The IP address of the server.")]
    public string ipAddress = "127.0.0.1";

    [Tooltip("The port number for communication.")]
    public int port = 3000;

    private string ConfigFilePath => Path.Combine(Application.dataPath, "../configuration.json");

    /// <summary>
    /// Saves the configuration data to a JSON file.
    /// </summary>
    public void Save()
    {
        string json = JsonUtility.ToJson(this, true); 
        File.WriteAllText(ConfigFilePath, json);
    }

    /// <summary>
    /// Loads the configuration data from a JSON file.
    /// </summary>
    /// <returns>The loaded configuration data.</returns>
    public static VideoConfig Load()
    {
        string configFilePath = Path.Combine(Application.dataPath, "../configuration.json");
        if (File.Exists(configFilePath))
        {
            string json = File.ReadAllText(configFilePath);
            return JsonUtility.FromJson<VideoConfig>(json);
        }
        else
        {
            VideoConfig config = new VideoConfig();
            config.Save(); 
            return config;
        }
    }
}