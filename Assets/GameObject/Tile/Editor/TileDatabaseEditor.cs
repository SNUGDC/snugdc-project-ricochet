﻿using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(TileDatabase))]
public class TileDatabaseEditor : Editor
{
    private TileDatabase m_Target;

    public void OnEnable()
    {
        m_Target = (TileDatabase) target;
    }

    public override void OnInspectorGUI()
    {
	    base.OnInspectorGUI();
        if (GUILayout.Button("Rebuild"))
            m_Target.Rebuild();
	}
	
}
