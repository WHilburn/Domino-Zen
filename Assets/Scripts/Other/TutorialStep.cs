using System;
using UnityEngine;

[Serializable]
public class TutorialStep
{
    public string text;
    public Transform worldTarget; // What the arrow UI should point to
    public CompletionCondition completionCondition; // Enum to define completion condition
    public bool placementEnabled = true; // Flag to enable/disable controls
    public bool flickEnables = true; // Flag to enable/disable flicks
    public bool cameraEnabled = true; // Flag to enable/disable camera movement
    public bool visible = true; // Flag to show/hide the arrow UI
    public bool limitedPlacement = false; // Flag to limit placement to a specific area
}

public enum CompletionCondition
{
    CompleteIndicatorRow1,
    CompleteIndicatorRow2,
    SpawnADomino,
    DropADomino,
    FillOneIndicator,
    ClickButton,
    WaitforCascadeStart,
    WaitForCascadeEnd,
    FillFourIndicators
}
