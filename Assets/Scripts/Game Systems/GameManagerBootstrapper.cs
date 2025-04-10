using UnityEngine;
public class GameManagerBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject gameManagerPrefab;

    void Awake()
    {
        if (GameManager.Instance == null)
        {
            Instantiate(gameManagerPrefab);
        }
    }
}
