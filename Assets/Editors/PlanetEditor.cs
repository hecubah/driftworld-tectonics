using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetManager))]
public class PlanetEditor : Editor
{
    PlanetManager m_PlanetManager;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Space(20);
        GUILayout.BeginVertical("box");
        GUILayout.Label("Planet: " + (m_PlanetManager.m_Planet == null ? "none" : "present"));
        if (m_PlanetManager.m_Planet != null)
        {
            GUILayout.Label("Mesh/RenderMesh vertices count: " + (m_PlanetManager.m_Planet.m_DataVertices != null ? m_PlanetManager.m_Planet.m_VerticesCount.ToString() : "null") + "/" + (m_PlanetManager.m_Planet.m_RenderVertices != null ? m_PlanetManager.m_Planet.m_RenderVerticesCount.ToString() : "null"));
            GUILayout.Label("Mesh/RenderMesh triangles count: " + (m_PlanetManager.m_Planet.m_DataTriangles != null ? m_PlanetManager.m_Planet.m_TrianglesCount.ToString() : "null") + "/" + (m_PlanetManager.m_Planet.m_RenderTriangles != null ? m_PlanetManager.m_Planet.m_RenderTrianglesCount.ToString() : "null"));
            GUILayout.Label("Render mode: " + (m_PlanetManager.m_RenderMode.Length == 0 ? "None" : m_PlanetManager.m_RenderMode));
            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0)
            {
                GUILayout.Label("Tectonic plates count: " + m_PlanetManager.m_Planet.m_TectonicPlates.Count.ToString());
            }
            m_PlanetManager.m_PropagateCrust = GUILayout.Toggle(m_PlanetManager.m_PropagateCrust, "Auto-propagate crust");
            m_PlanetManager.m_PropagateData = GUILayout.Toggle(m_PlanetManager.m_PropagateData, "Auto-propagate data");
        }
        GUILayout.EndVertical();
        if (GUILayout.Button("Load new planet"))
        {
            m_PlanetManager.LoadNewPlanet();
        }
        bool m1, m2, m3;

        GUILayout.BeginHorizontal();
        if (m_PlanetManager.m_Planet != null)
        {
            GUILayout.Label("Set render mode:");
            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0)
            {
                m1 = GUILayout.Button("Plates");
                if (m1)
                {
                    m_PlanetManager.m_RenderMode = "plates";
                }
            }
            m2 = GUILayout.Button("Data");
            if (m2)
            {
                m_PlanetManager.m_RenderMode = "data";
            }
            m3 = GUILayout.Button("Normal");
            if (m3)
            {
                m_PlanetManager.m_RenderMode = "normal";
            }
        }
        GUILayout.EndHorizontal();
        if ((m_PlanetManager.m_Planet != null) && (m_PlanetManager.m_RenderMode.Length!=0))
        {
            if (GUILayout.Button("Render surface"))
            {
                m_PlanetManager.RenderSurfaceMesh();
            }
        }

        GUILayout.Space(20);

        GUILayout.BeginVertical("box");
        if ((m_PlanetManager.m_Planet != null))
        {
            GUILayout.Label("Tectonic surface tools:");
            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0)
            {
                if (GUILayout.Button("Move tectonic plates"))
                {
                    m_PlanetManager.m_Planet.MovePlates();
                    m_PlanetManager.RenderSurfaceMesh();
                }
            }
            if (GUILayout.Button("Paint plate borders"))
            {
                m_PlanetManager.CAPPlatesBorderTexture(m_PlanetManager.m_Planet);
            }
            if (GUILayout.Button("Initialize tectonic plates"))
            {
                m_PlanetManager.m_Planet.InitializeRandomCrust();
                m_PlanetManager.RenderSurfaceMesh();
            }


        }
        GUILayout.EndVertical();

        GUILayout.Space(20);

        GUILayout.BeginVertical("box");
        if ((m_PlanetManager.m_Planet != null))
        {
            GUILayout.Label("Data surface tools:");
            if (GUILayout.Button("Markup Terrain"))
            {
                m_PlanetManager.m_Planet.MarkupTerrain();
                m_PlanetManager.RenderSurfaceMesh();

            }
            if (GUILayout.Button("Generate Fractal Terrain"))
            {
                m_PlanetManager.m_Planet.GenerateFractalTerrain();
                m_PlanetManager.RenderSurfaceMesh();
            }
            if (GUILayout.Button("Create and paint terrain texture"))
            {
                m_PlanetManager.CAPTerrainTexture(m_PlanetManager.m_Planet);
            }


        }
        GUILayout.EndVertical();

        GUILayout.Space(20);

        GUILayout.BeginVertical("box");
        if ((m_PlanetManager.m_Planet != null))
        {
            GUILayout.Label("WIP tools:");
            if (GUILayout.Button("Debug Function"))
            {
                m_PlanetManager.DebugFunction();
            }
            if (GUILayout.Button("Debug Function 2"))
            {
                m_PlanetManager.DebugFunction2();
            }
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        if ((m_PlanetManager.m_Planet != null))
        {
            GUILayout.Label("General tools:");
            if (GUILayout.Button("Triangle Collision Test"))
            {
                m_PlanetManager.CAPTriangleCollisionTestTexture();
            }
            if (GUILayout.Button("Wash Texture"))
            {
               m_PlanetManager.m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null);
               GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null);
            }
        }
        GUILayout.EndVertical();


    }
    private void OnEnable()
    {
        m_PlanetManager = (PlanetManager) target;
    }
}
