using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class DominoResetManager : MonoBehaviour
{
    public static DominoResetManager Instance { get; private set; }
    public List<Domino> dominoes = new List<Domino>();
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

        Domino.OnDominoFall.AddListener(RegisterDomino);
        Domino.OnDominoPlacedCorrectly.AddListener(RemoveDomino);
        Domino.OnDominoDeleted.AddListener(RemoveDomino); // Subscribe to domino deletion event
    }

    public void RegisterDomino(Domino domino)
    {
        if (Instance == null) return; // Ensure the instance is not null
        if (!dominoes.Contains(domino))
        {
            dominoes.Add(domino);
        }

        CancelInvoke(nameof(ResetAllDominoes));
        Invoke(nameof(ResetAllDominoes), resetDelay);
    }

    public void RemoveDomino(Domino domino)
    {
        Debug.Log("Removing domino: " + domino.name);
        if (dominoes.Contains(domino))
        {
            dominoes.Remove(domino);
        }
    }

    private void ResetAllDominoes()
    {
        Debug.Log("Resetting all dominoes. Count: " + dominoes.Count);
        if (dominoes.Count < 1000) resetAnimation = Domino.DominoAnimation.Jump;
        else if (dominoes.Count < 2000) resetAnimation = Domino.DominoAnimation.Rotate;
        else resetAnimation = Domino.DominoAnimation.Teleport;
        foreach (var domino in dominoes)
        {
            domino.AnimateDomino(resetAnimation);
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
