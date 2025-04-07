using System;
using UnityEngine;

public class TutorialStep
{
    public string text;
    public Transform worldTarget; //What the arrow UI should point to
    public Func<bool> completionCondition;
    public bool enableButton = false; // Flag to enable/disable the continue button
}
