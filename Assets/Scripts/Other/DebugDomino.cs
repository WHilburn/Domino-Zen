using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEditor;

public class DebugDomino : DominoLike
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            SnapToGround();
        }
    }
}
