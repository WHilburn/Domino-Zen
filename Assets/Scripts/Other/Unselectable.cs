using UnityEngine;

public class Unselectable : MonoBehaviour
{
    void Awake()
    {
        gameObject.hideFlags = HideFlags.NotEditable;
    }
}
