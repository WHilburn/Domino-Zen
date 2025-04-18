using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlapMesh : MonoBehaviour
{
    public bool isOverlapping = false; // Flag to indicate if the object is overlapping with another object
    public Color NotColidingColor = Color.blue;
    public Color ColidingColor = Color.red; // Color to indicate collision
    public Renderer objectRenderer; // Reference to the object's Renderer component

    void Start()
    {
        objectRenderer = GetComponent<Renderer>(); // Initialize the Renderer component
        objectRenderer.material.color = NotColidingColor; // Set initial color
    }

    void Update()
    {
        if (isOverlapping)
        {
            objectRenderer.material.color = ColidingColor; // Change to collision color
        }
        else
        {
            objectRenderer.material.color = NotColidingColor; // Change to non-collision color
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Bucket"))
        {
            isOverlapping = true; // Set the flag to true if overlapping with an object tagged "Bucket"
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Bucket"))
        {
            isOverlapping = false; // Reset the flag when exiting the overlap with an object tagged "Bucket"
        }
    }
}
