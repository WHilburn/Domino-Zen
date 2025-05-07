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
            progressData.filledIndicatorIDs.Add(indicator.UniqueID); // Use UniqueID
        }

        string json = JsonUtility.ToJson(progressData, true);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFileName), json);
        Debug.Log($"Progress saved for level {levelName} at {Application.persistentDataPath}/{SaveFileName}");
    }

    public static void LoadProgress(string levelName, HashSet<PlacementIndicator> allIndicators)
    {
        string filePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No saved progress file found at " + filePath);
            return;
        }

        string json = File.ReadAllText(filePath);
        var progressData = JsonUtility.FromJson<LevelProgressData>(json);

        if (progressData.levelName != levelName)
        {
            Debug.LogWarning("Saved progress does not match the current level at " + filePath);
            return;
        }

        foreach (var indicator in allIndicators)
        {
            if (progressData.filledIndicatorIDs.Contains(indicator.UniqueID)) // Use UniqueID
            {
                indicator.FillSelf(); // Restore the filled state and spawn a domino in the indicator
            }
        }

        var allIndicatorIDs = new List<string>();
        foreach (var indicator in allIndicators)
        {
            allIndicatorIDs.Add(indicator.UniqueID); // Use UniqueID
        }
        foreach (var id in progressData.filledIndicatorIDs)
        {
            if (!allIndicatorIDs.Contains(id))
            {
                Debug.LogWarning($"Indicator with ID {id} not found in the current scene.");
            }
        }

        Debug.Log($"Progress loaded for level {levelName} from {filePath}");
    }

    public static void ResetProgress()
    {
        string filePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Progress reset. File deleted at {filePath}");
        }
        else
        {
            Debug.LogWarning($"No progress file found to reset at {filePath}");
        }
    }
}
