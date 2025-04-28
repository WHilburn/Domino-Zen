using UnityEngine;
using DG.Tweening;

public class VFXManager : MonoBehaviour
{
    void OnEnable()
    {
        Domino.OnDominoPlacedCorrectly.AddListener(HandleDominoPlacedCorrectly);
    }

    void OnDisable()
    {
        Domino.OnDominoPlacedCorrectly.RemoveListener(HandleDominoPlacedCorrectly);
    }

    private void HandleDominoPlacedCorrectly(Domino domino)
    {
        Renderer[] childRenderers = domino.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            Material[] materials = renderer.materials;

            // Handle gleam effect for material slot 1
            if (materials.Length > 1)
            {
                Material gleamMaterialInstance = materials[1];
                gleamMaterialInstance.SetFloat("_Gleam_Opacity", 0f);

                Sequence gleamSequence = DOTween.Sequence();
                gleamSequence.Append(DOTween.To(() => gleamMaterialInstance.GetFloat("_Gleam_Opacity"),
                                                value => gleamMaterialInstance.SetFloat("_Gleam_Opacity", value),
                                                1f, 0.15f))
                             .AppendInterval(0.3f)
                             .Append(DOTween.To(() => gleamMaterialInstance.GetFloat("_Gleam_Opacity"),
                                                value => gleamMaterialInstance.SetFloat("_Gleam_Opacity", value),
                                                0f, 0.15f));
            }

            // Handle color tweening for material slot 0
            if (materials.Length > 0)
            {
                Material colorMaterialInstance = materials[0];
                Color initialColor = colorMaterialInstance.color;
                Color targetColor = domino.GetComponent<DominoSkin>().colorOverride;
                colorMaterialInstance.DOColor(targetColor, 1f).OnComplete(() =>
                {
                    domino.GetComponent<DominoSkin>().colorOverride = targetColor;
                });
            }
        }
    }
}