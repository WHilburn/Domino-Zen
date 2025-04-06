using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class DominoResetManager : MonoBehaviour
{
    public static DominoResetManager Instance { get; private set; }

    private Dictionary<Domino, (Vector3 position, Quaternion rotation)> dominoes = new Dictionary<Domino, (Vector3, Quaternion)>();
    public float resetDelay = 3f;
    public float resetDuration = 1f;
    public Domino.DominoAnimation resetAnimation = Domino.DominoAnimation.Rotate;

    private void Awake()
    {
        DOTween.SetTweensCapacity(10000, 10000);
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;

        Domino.OnDominoFall.AddListener(RegisterDominoOnFall);
        Domino.OnDominoStopMoving.AddListener(RemoveDominoOnStop);
        Domino.OnDominoDeleted.AddListener(RemoveDomino); // Subscribe to domino deletion event
    }

    public void RegisterDomino(Domino domino, Vector3 stablePos, Quaternion stableRot)
    {
        if (Instance == null) return; // Ensure the instance is not null
        if (!dominoes.ContainsKey(domino))
        {
            dominoes[domino] = (stablePos, stableRot);
        }

        CancelInvoke(nameof(ResetAllDominoes));
        Invoke(nameof(ResetAllDominoes), resetDelay);
    }

    public void RemoveDomino(Domino domino)
    {
        if (dominoes.ContainsKey(domino))
        {
            dominoes.Remove(domino);
        }
    }

    private void RegisterDominoOnFall(Domino domino)
    {
        if (!dominoes.ContainsKey(domino))
        {
            dominoes[domino] = (domino.transform.position, domino.transform.rotation);
        }

        CancelInvoke(nameof(ResetAllDominoes));
        Invoke(nameof(ResetAllDominoes), resetDelay);
    }

    private void RemoveDominoOnStop(Domino domino)
    {
        if (dominoes.ContainsKey(domino))
        {
            dominoes.Remove(domino);
        }
    }

    private void ResetAllDominoes()
    {
        if (dominoes.Count < 1000) resetAnimation = Domino.DominoAnimation.Jump;
        else if (dominoes.Count < 2000) resetAnimation = Domino.DominoAnimation.Rotate;
        else resetAnimation = Domino.DominoAnimation.Teleport;
        foreach (var kvp in dominoes)
        {
            kvp.Key.AnimateDomino(resetAnimation);
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
