using UnityEngine;
using System.Collections;

public class DominoThrobber : MonoBehaviour
{
    private int dominoCount = 12; // Number of dominoes in the circle
    public float delayBetweenFalls = 0.02f; // Time between each fall
    public Domino[] dominoes; // Array of dominoes in the circle
    private int index = 0; // Current domino index

    void Start()
    {
        //populate the list of dominoes with object's existing children
        dominoes = GetComponentsInChildren<Domino>();
        dominoCount = dominoes.Length; // Update the domino count based on the number of children
        // Save the stable positions of all dominoes
        foreach (var domino in dominoes)
        {
            domino.SaveStablePosition(domino.transform);
        }

        // Start the throbber loop
        StartCoroutine(ThrobberLoop());
    }

    public void BeginLoop()
    {
        // Start the throbber loop
        StartCoroutine(ThrobberLoop());
    }

    public void StopLoop()
    {
        // Stop the throbber loop
        StopAllCoroutines();
    }

    public IEnumerator ThrobberLoop()
    {
        yield return new WaitForSeconds(0.25f); // Initial delay before starting the loop
        while (true)
        {
            // Make the current domino fall
            MakeDominoFall(dominoes[index]);

            // Reset the opposite domino
            int oppositeIndex = (index + dominoCount / 2) % dominoCount;
            dominoes[oppositeIndex].AnimateDomino(Domino.DominoAnimation.Rotate, 0.3f);

            // Move to the next domino
            index = (index + 1) % dominoCount;

            // Wait before making the next domino fall
            yield return new WaitForSeconds(delayBetweenFalls);
        }
    }

    private void MakeDominoFall(Domino domino)
    {
        Rigidbody rb = domino.GetComponent<Rigidbody>();
        if (rb)
        {
            // Ensure the Rigidbody is not kinematic so physics can affect it
            rb.isKinematic = false;

            // Get the holdPoint position (top of the domino)
            Vector3 holdPoint = DominoLike.holdPoint;

            // Convert the local holdPoint to world space
            Vector3 worldHoldPoint = domino.transform.TransformPoint(holdPoint);

            // Calculate the force direction based on the domino's forward direction
            Vector3 forceDirection = -domino.transform.forward;

            // Apply the force at the holdPoint in the forward direction
            rb.AddForceAtPosition(forceDirection * 1f, worldHoldPoint, ForceMode.Impulse);
        }
    }
}