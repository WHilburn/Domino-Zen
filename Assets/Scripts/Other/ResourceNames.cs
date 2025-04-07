using UnityEngine;

[ExecuteInEditMode]
public class ResourceNames : MonoBehaviour
{
    public static ResourceNames Instance { get; private set; }
    public const string DominoTag = "DominoTag"; // Tag for domino objects
    public const string IndicatorTag = "IndicatorTag"; // Tag for placement indicators
    public DominoMaterialList blackDominoSkin;
    public DominoMaterialList whiteDominoSkin;
    public DominoMaterialList glassDominoSkin;
    public DominoSoundList dominoClickSounds;
    public DominoSoundList dominoPianoSounds;
    public GameObject dominoPrefab;
    public GameObject indicatorPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            DestroyImmediate(gameObject); // Ensure only one instance exists
        }
    }
}

