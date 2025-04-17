using UnityEngine;
public class Bucket : MonoBehaviour
{
    public static Bucket Instance { get; private set; } // Singleton instance of the Bucket class
    public Transform spawnLocation;
}