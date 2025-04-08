using System;
using UnityEngine;

[Serializable]
public class TutorialStep
{
    public string text;
    public Transform worldTarget; //What the arrow UI should point to
    public Func<bool> completionCondition;
    public bool enableButton = true; // Flag to enable/disable the continue button
    public bool placementEnabled = true; // Flag to enable/disable controls
    public bool cameraEnabled = true; // Flag to enable/disable camera movement
}
