using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class DominoResetManager : MonoBehaviour
{
    public static DominoResetManager Instance { get; private set; }

    private Dictionary<Domino, (Vector3 position, Quaternion rotation)> dominoes = new Dictionary<Domino, (Vector3, Quaternion)>();
    public float resetDelay = 3f;
    public float resetDuration = 1f;

    private void Awake()
    {
        DOTween.SetTweensCapacity(10000, 100);
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterDomino(Domino domino, Vector3 stablePos, Quaternion stableRot)
    {
        if (!dominoes.ContainsKey(domino))
        {
            dominoes[domino] = (stablePos, stableRot);
        }

        CancelInvoke(nameof(ResetAllDominoes));
        Invoke(nameof(ResetAllDominoes), resetDelay);
    }

    private void ResetAllDominoes()
    {
        foreach (var kvp in dominoes)
        {
            kvp.Key.ResetDomino();
            // Domino domino = kvp.Key;
            // domino.transform.DOKill();
            // if (!domino.stablePositionSet) {
            //     Destroy(domino.gameObject);
            //     continue;
            // }
            // Rigidbody rb = domino.GetComponent<Rigidbody>();
            // (Vector3 startPos, Quaternion startRot) = kvp.Value;

            // rb.isKinematic = true; // Prevent physics interference
            // //domino.GetComponent<BoxCollider>().enabled = false; // Disable collider for smooth transition
            // rb.transform.DOMove(startPos, resetDuration);
            // rb.transform.DORotateQuaternion(startRot, resetDuration).OnComplete(() => domino.TogglePhysics());
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
