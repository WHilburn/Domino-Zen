using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LevelProgressManager
{
    private const string SaveFileName = "LevelProgress.json";

    [System.Serializable]
    private class LevelProgressData
    {
        public string levelName;
        public List<string> filledIndicatorIDs = new List<string>();
    }

    public static void SaveProgress(string levelName, HashSet<PlacementIndicator> filledIndicators)
    {
        var progressData = new LevelProgressData
        {
            levelName = levelName,
            filledIndicatorIDs = new List<string>()
        };

        foreach (var indicator in filledIndicators)
        {
            progressData.filledIndicatorIDs.Add(indicator.GetInstanceID().ToString());
        }

        string json = JsonUtility.ToJson(progressData, true);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFileName), json);
        Debug.Log($"Progress saved for level {levelName}.");
    }

    public static void LoadProgress(string levelName, HashSet<PlacementIndicator> allIndicators)
    {
        string filePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        GameObject dominoPrefab = GameManager.Instance.dominoPrefab;
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No saved progress file found.");
            return;
        }

        string json = File.ReadAllText(filePath);
        var progressData = JsonUtility.FromJson<LevelProgressData>(json);

        if (progressData.levelName != levelName)
        {
            Debug.LogWarning("Saved progress does not match the current level.");
            return;
        }

        foreach (var indicator in allIndicators)
        {
            if (progressData.filledIndicatorIDs.Contains(indicator.GetInstanceID().ToString()))
            {
                indicator.RestoreProgress(); // Restore the filled state and spawn a domino in the indicator
            }
        }

        Debug.Log($"Progress loaded for level {levelName}.");
    }
}
