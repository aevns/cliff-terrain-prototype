using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainSystem))]
public class TerrainSystemEditor : Editor
{
    TerrainSystem terrainSystem;

    void OnEnable()
    {
        terrainSystem = (TerrainSystem)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Rebuild"))
        {
            terrainSystem.FullUpdate();
        }
    }
}