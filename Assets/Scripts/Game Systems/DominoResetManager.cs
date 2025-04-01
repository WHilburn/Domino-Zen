using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class DominoResetManager : MonoBehaviour
{
    public static DominoResetManager Instance { get; private set; }

    private Dictionary<Rigidbody, (Vector3 position, Quaternion rotation)> dominoes = new Dictionary<Rigidbody, (Vector3, Quaternion)>();
    private float resetDelay = 5f;
    private float resetDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterDomino(Rigidbody rb, Vector3 stablePos, Quaternion stableRot)
    {
        if (!dominoes.ContainsKey(rb))
        {
            dominoes[rb] = (stablePos, stableRot);
        }

        CancelInvoke(nameof(ResetAllDominoes));
        Invoke(nameof(ResetAllDominoes), resetDelay);
    }

    private void ResetAllDominoes()
    {
        foreach (var kvp in dominoes)
        {
            Rigidbody rb = kvp.Key;
            (Vector3 startPos, Quaternion startRot) = kvp.Value;

            rb.isKinematic = true; // Prevent physics interference
            rb.transform.DOMove(startPos, resetDuration);
            rb.transform.DORotateQuaternion(startRot, resetDuration).OnComplete(() => rb.isKinematic = false);
        }

        dominoes.Clear();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllDominoes();
        }
    }
}
