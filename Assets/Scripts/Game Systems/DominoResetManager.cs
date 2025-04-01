using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class DominoResetManager : MonoBehaviour
{
    public static DominoResetManager Instance { get; private set; }

    private Dictionary<Domino, (Vector3 position, Quaternion rotation)> dominoes = new Dictionary<Domino, (Vector3, Quaternion)>();
    public float resetDelay = 3f;
    public float resetDuration = 1f;
    public Domino.ResetAnimation resetAnimation = Domino.ResetAnimation.Rotate;

    private void Awake()
    {
        DOTween.SetTweensCapacity(10000, 10000);
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

    public void RemoveDomino(Domino domino)
    {
        if (dominoes.ContainsKey(domino))
        {
            dominoes.Remove(domino);
        }
    }

    private void ResetAllDominoes()
    {
        if (dominoes.Count < 1000) resetAnimation = Domino.ResetAnimation.Jump;
        else if (dominoes.Count < 2000) resetAnimation = Domino.ResetAnimation.Rotate;
        else resetAnimation = Domino.ResetAnimation.Teleport;
        foreach (var kvp in dominoes)
        {
            kvp.Key.ResetDomino(resetAnimation);
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
