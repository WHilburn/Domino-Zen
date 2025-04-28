using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class VFXManager : MonoBehaviour
{
    public Material gleamWipeMaterial;

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
            List<Material> materials = new List<Material>(renderer.materials);
            materials.Add(gleamWipeMaterial);
            renderer.materials = materials.ToArray();
        }

        gleamWipeMaterial.SetFloat("_Gleam_Opacity", 0f);
        Debug.Log("Gleam Wipe Opacity set to 0");

        Sequence gleamSequence = DOTween.Sequence();
        gleamSequence.Append(DOTween.To(() => gleamWipeMaterial.GetFloat("_Gleam_Opacity"),
                                        value => gleamWipeMaterial.SetFloat("_Gleam_Opacity", value),
                                        1f, 0.1f))
                     .AppendInterval(0.3f)
                     .Append(DOTween.To(() => gleamWipeMaterial.GetFloat("_Gleam_Opacity"),
                                        value => gleamWipeMaterial.SetFloat("_Gleam_Opacity", value),
                                        0f, 0.1f)
                                  .OnStart(() => Debug.Log("Gleam Wipe Opacity set to 1")))
                     .OnComplete(() =>
                     {
                        Debug.Log("Gleam Wipe Opacity tween completed");
                         foreach (Renderer renderer in childRenderers)
                         {
                             List<Material> materials = new List<Material>(renderer.materials);
                             materials.Remove(gleamWipeMaterial);
                             renderer.materials = materials.ToArray();
                         }
                         Debug.Log("Gleam Wipe Material removed from renderers");
                     });
    }
}