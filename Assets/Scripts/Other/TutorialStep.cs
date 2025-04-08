using System;
using UnityEngine;

[Serializable]
public class TutorialStep
{
    public string text;
    public Transform worldTarget; // What the arrow UI should point to

    public CompletionCondition completionCondition; // Enum to define completion condition

    public bool placementEnabled = true; // Flag to enable/disable controls
    public bool cameraEnabled = true; // Flag to enable/disable camera movement
}

public enum CompletionCondition
{
    CompleteIndicatorRow,
    SpawnADomino,
    DropADomino,
    FillOneIndicator,
    ClickButton
}
