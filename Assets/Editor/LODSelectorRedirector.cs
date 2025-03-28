// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;

// #if UNITY_EDITOR
// [InitializeOnLoad]
// public static class LODSelectionRedirector
// {
//     static LODSelectionRedirector()
//     {
//         Debug.Log("LODSelectionRedirector initialized");
//         Selection.selectionChanged += HandleSelection;
//     }

//     private static void HandleSelection()
//     {
//         if (Selection.gameObjects.Length == 0) return;

//         List<GameObject> newSelection = new List<GameObject>();

//         foreach (GameObject obj in Selection.gameObjects)
//         {
//             // Check if the object is part of an LOD group
//             LODGroup lodGroup = obj.GetComponentInParent<LODGroup>();
//             if (lodGroup != null)
//             {
//                 newSelection.Add(lodGroup.gameObject);
//                 Debug.Log("Selected LODGroup: " + lodGroup.gameObject.name);
//             }
//             else
//             {
//                 newSelection.Add(obj);
//                 Debug.Log("Selected object: " + obj.name);
//             }
//         }
//         Selection.objects = newSelection.ToArray();
//         // Selection.activeGameObject = newSelection[0];
//         Selection.SetActiveObjectWithContext(newSelection[0], null);
//         // Replace the selection only if something changed
//         // if (newSelection.Count != Selection.gameObjects.Length || !newSelection.TrueForAll(Selection.Contains))
//         // {
//         //     Selection.objects = newSelection.ToArray();
//         // }
//     }
// }
// #endif
