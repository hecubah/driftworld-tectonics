using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetManager))]
public class PlanetEditor : Editor
{
    PlanetManager m_PlanetManager; // respective planet manager to operate on

    public override void OnInspectorGUI() // editor tools for manipulating the simulation
    {
        base.OnInspectorGUI();
        GUILayout.Space(20);
        GUILayout.BeginVertical("box"); // info box begin
        bool planet_is_loaded = (m_PlanetManager.m_Planet != null); // planet load check variable
        GUILayout.Label("Planet: " + (planet_is_loaded ? "none" : "present")); // check if a planet is loaded
        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Label("Mesh/RenderMesh vertices count: " + (m_PlanetManager.m_Planet.m_DataVertices != null ? m_PlanetManager.m_Planet.m_VerticesCount.ToString() : "null") + "/" + (m_PlanetManager.m_Planet.m_RenderVertices != null ? m_PlanetManager.m_Planet.m_RenderVerticesCount.ToString() : "null")); // number of vertices
            GUILayout.Label("Mesh/RenderMesh triangles count: " + (m_PlanetManager.m_Planet.m_DataTriangles != null ? m_PlanetManager.m_Planet.m_TrianglesCount.ToString() : "null") + "/" + (m_PlanetManager.m_Planet.m_RenderTriangles != null ? m_PlanetManager.m_Planet.m_RenderTrianglesCount.ToString() : "null")); // number of triangles
            GUILayout.Label("Render mode: " + (m_PlanetManager.m_RenderMode.Length == 0 ? "None" : m_PlanetManager.m_RenderMode)); // in what mode the planet is to be rendered - string
            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0) // if there are tectonic plates initialized
            {
                GUILayout.Label("Tectonic plates count: " + m_PlanetManager.m_Planet.m_TectonicPlates.Count.ToString()); // number of tectonic plates
            }
            m_PlanetManager.m_PropagateCrust = GUILayout.Toggle(m_PlanetManager.m_PropagateCrust, "Auto-propagate crust"); // toggle if the crust data should propagate automatically to main data
            m_PlanetManager.m_PropagateData = GUILayout.Toggle(m_PlanetManager.m_PropagateData, "Auto-propagate data"); // toggle if the main data should propagate automatically to render data
            m_PlanetManager.m_ClampToOceanLevel = GUILayout.Toggle(m_PlanetManager.m_ClampToOceanLevel, "Clamp to ocean level"); // toggle if the elevation is to be clamped to ocean level
        }
        GUILayout.EndVertical(); // info box end
        if (GUILayout.Button("Load new planet")) // start button to load fresh planet
        {
            m_PlanetManager.m_PropagateCrust = false;
            m_PlanetManager.m_PropagateData = false;
            m_PlanetManager.m_ClampToOceanLevel = false;
            m_PlanetManager.LoadNewPlanet(); // manager call for new mesh and topology
        }
        bool plates_render, data_render, default_render; // variables for button switching between render modes

        GUILayout.BeginHorizontal(); // render mode switching box begin
        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Label("Set render mode:");
            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0) // if there is at least one tectonic plate
            {
                plates_render = GUILayout.Button("Plates"); // switch rendering mode to direct plate rendering
                if (plates_render)
                {
                    m_PlanetManager.m_RenderMode = "plates"; // set the appropriate render mode string
                }
            }
            data_render = GUILayout.Button("Data"); // switch rendering mode to main data rendering
            if (data_render)
            {
                m_PlanetManager.m_RenderMode = "data"; // set the appropriate render mode string
            }
            default_render = GUILayout.Button("Normal"); // switch rendering mode to normal rendering
            if (default_render)
            {
                m_PlanetManager.m_RenderMode = "normal"; // set the appropriate render mode string
            }
        }
        GUILayout.EndHorizontal(); // render mode switching box end
        if ((planet_is_loaded) && (m_PlanetManager.m_RenderMode.Length!=0)) // if a planet is loaded and a rendering mode is set
        {
            if (GUILayout.Button("Render surface"))
            {
                m_PlanetManager.RenderSurfaceMesh(); // draw the mesh according to set render mode
            }
        }

        GUILayout.Space(20); // component spacing

        GUILayout.BeginVertical("box"); // tectonic plates tools box begin
        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Label("Tectonic surface tools:");
            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0) // if there is at least one tectonic plate
            {
                if (GUILayout.Button("Move tectonic plates")) // iterate tectonic motion
                {
                    m_PlanetManager.m_Planet.MovePlates(); // move plates
                    m_PlanetManager.RenderSurfaceMesh(); // draw the mesh according to set render mode
                }
                if (GUILayout.Button("Paint plate borders"))
                {
                    m_PlanetManager.CAPPlatesAreaTexture(m_PlanetManager.m_Planet); // paint plate border texture over the mesh
                }
                if (GUILayout.Button("Recalculate missing samples"))
                {
                    m_PlanetManager.m_Planet.CrustToDataRecalculateSamples(); // recalculate data mesh filling in missing samples
                    m_PlanetManager.RenderSurfaceMesh(); // draw the mesh according to set render mode
                }
                if (GUILayout.Button("Resample crust"))
                {
                    m_PlanetManager.m_Planet.ResampleCrust(); // recalculate data mesh filling in missing samples
                    m_PlanetManager.RenderSurfaceMesh(); // draw the mesh according to set render mode
                }
            }
            if (GUILayout.Button("Initialize tectonic plates")) // new tectonic plates system
            {
                m_PlanetManager.m_Planet.InitializeRandomCrust(); // random plates initialization
                m_PlanetManager.RenderSurfaceMesh(); // draw the mesh according to set render mode
            }
        }
        GUILayout.EndVertical(); // tectonic plates tools box end

        GUILayout.Space(20); // component spacing

        GUILayout.BeginVertical("box"); // main data tools box begin
        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Label("Data surface tools:");
            if (GUILayout.Button("Markup Terrain"))
            {
                m_PlanetManager.m_Planet.MarkupTerrain(); // basic terrain elevation mark-up for debugging purposes
                m_PlanetManager.RenderSurfaceMesh(); // draw the mesh according to set render mode

            }
            if (GUILayout.Button("Generate Fractal Terrain"))
            {
                m_PlanetManager.m_Planet.GenerateFractalTerrain();
                m_PlanetManager.RenderSurfaceMesh(); // draw the mesh according to set render mode
            }
            if (GUILayout.Button("Create and paint terrain texture"))
            {
                m_PlanetManager.CAPTerrainTexture(m_PlanetManager.m_Planet); // paint default terraing texture over the mesh
            }


        }
        GUILayout.EndVertical(); // main data tools box end

        GUILayout.Space(20); // component spacing

        GUILayout.BeginVertical("box"); // work in progress tools box begin
        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Label("WIP tools:");
            if (GUILayout.Button("Debug Function"))
            {
                m_PlanetManager.DebugFunction(); // placeholder debug function
            }
            if (GUILayout.Button("Debug Function 2"))
            {
                m_PlanetManager.DebugFunction2(); // placeholder debug function
            }
            if (GUILayout.Button("Debug Function 3"))
            {
                m_PlanetManager.DebugFunction3(); // placeholder debug function
            }
            if (GUILayout.Button("Debug Function 4"))
            {
                m_PlanetManager.DebugFunction4(); // placeholder debug function
            }
            if (GUILayout.Button("Initialize RNG"))
            {
                m_PlanetManager.m_Random.RandomInit(m_PlanetManager.m_RandomSeed); // initialize Mersenne RNG using given seed
            }
        }
        GUILayout.EndVertical(); // work in progress tools box end

        GUILayout.BeginVertical("box"); // general tools box begin
        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Label("General tools:");
            if (GUILayout.Button("Triangle Collision Test"))
            {
                m_PlanetManager.CAPTriangleCollisionTestTexture(); // test triangle collision of random two triangles
            }
            if (GUILayout.Button("Wash Texture"))
            {
               m_PlanetManager.m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the planet texture
               GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the plane texture
            }
        }
        GUILayout.EndVertical(); // general tools box end


    }
    private void OnEnable()
    {
        m_PlanetManager = (PlanetManager) target; // set the manager object to the editor target
    }
}
