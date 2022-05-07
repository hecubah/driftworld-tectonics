using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetManager))]
public class PlanetEditor : Editor
{
    PlanetManager m_PlanetManager; // respective planet manager to operate on

    public override void OnInspectorGUI() // editor tools for manipulating the simulation
    {
        base.OnInspectorGUI();
        GUILayout.BeginVertical("box"); // info box begin
        bool planet_is_loaded = (m_PlanetManager.m_Planet != null); // planet load check variable
        GUILayout.Label("Planet: " + (planet_is_loaded ? "present" : "none")); // check if a planet is loaded
        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Label("Mesh/RenderMesh vertices count: " + (m_PlanetManager.m_Planet.m_DataVertices != null ? m_PlanetManager.m_Planet.m_VerticesCount.ToString() : "null") + "/" + (m_PlanetManager.m_Planet.m_RenderVertices != null ? m_PlanetManager.m_Planet.m_RenderVerticesCount.ToString() : "null")); // number of vertices
            GUILayout.Label("Mesh/RenderMesh triangles count: " + (m_PlanetManager.m_Planet.m_DataTriangles != null ? m_PlanetManager.m_Planet.m_TrianglesCount.ToString() : "null") + "/" + (m_PlanetManager.m_Planet.m_RenderTriangles != null ? m_PlanetManager.m_Planet.m_RenderTrianglesCount.ToString() : "null")); // number of triangles
            GUILayout.Label("Render mode: " + m_PlanetManager.m_RenderMode.ToString());
            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0) // if there are tectonic plates initialized
            {
                GUILayout.Label("Tectonic plates count: " + m_PlanetManager.m_Planet.m_TectonicPlates.Count.ToString()); // number of tectonic plates
                GUILayout.Label("Tectonic steps taken without resample: " + m_PlanetManager.m_Planet.m_TectonicStepsTakenWithoutResample); // days since last metric mess
                GUILayout.Label("Total tectonic steps taken: " + m_PlanetManager.m_Planet.m_TotalTectonicStepsTaken);
            }
        }
        GUILayout.EndVertical(); // info box end
        if (GUILayout.Button("Load new planet")) // start button to load fresh planet
        {
            m_PlanetManager.LoadNewPlanet(); // manager call for new mesh and topology
        }
        if (planet_is_loaded)
        {
            if (GUILayout.Button("Save planet to file"))
            {
                SaveManager.SavePlanet(m_PlanetManager);
            }

        }
        if (GUILayout.Button("Load planet from file"))
        {
            SaveManager.LoadPlanet(m_PlanetManager);
        }

        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Space(10); // component spacing
            m_PlanetManager.m_FoldoutRenderOptions = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutRenderOptions, "Render options:");
            if (m_PlanetManager.m_FoldoutRenderOptions)
            {
                GUILayout.BeginVertical("box");
                if (m_PlanetManager.m_Planet.m_TectonicPlatesCount > 0)
                {
                    m_PlanetManager.m_PropagateCrust = GUILayout.Toggle(m_PlanetManager.m_PropagateCrust, "Auto-propagate crust"); // toggle if the crust data should propagate automatically to main data
                }
                m_PlanetManager.m_PropagateData = GUILayout.Toggle(m_PlanetManager.m_PropagateData, "Auto-propagate data"); // toggle if the main data should propagate automatically to render data
                m_PlanetManager.m_ClampToOceanLevel = GUILayout.Toggle(m_PlanetManager.m_ClampToOceanLevel, "Clamp to ocean level"); // toggle if the elevation is to be clamped to ocean level
                m_PlanetManager.m_OverlayOnRender = GUILayout.Toggle(m_PlanetManager.m_OverlayOnRender, "Paint overlay on render");
                GUILayout.BeginHorizontal(); // render mode switching box begin
                GUILayout.Label("Overlay: ");
                m_PlanetManager.m_TextureOverlay = (TexOverlay)EditorGUILayout.EnumPopup(m_PlanetManager.m_TextureOverlay);
                GUILayout.EndHorizontal(); // render mode switching box begin

                GUILayout.BeginHorizontal(); // render mode switching box begin
                GUILayout.Label("Render mode: ");
                m_PlanetManager.m_RenderMode = (RenderMode)EditorGUILayout.EnumPopup(m_PlanetManager.m_RenderMode);
                GUILayout.EndHorizontal(); // render mode switching box begin

                if (GUILayout.Button("Wash Texture"))
                {
                    m_PlanetManager.m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the planet texture
                    GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the plane texture
                }
                GUILayout.EndVertical();
            }

            if (GUILayout.Button("Render surface"))
            {
                m_PlanetManager.RenderPlanet();
            }


            GUILayout.Space(10); // component spacing
            m_PlanetManager.m_FoldoutTectonics = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutTectonics, "Tectonic tools:");
            if (m_PlanetManager.m_FoldoutTectonics)
            {
                GUILayout.BeginVertical("box"); // tectonic plates tools box begin
                if (GUILayout.Button("Initialize tectonic plates")) // new tectonic plates system
                {
                    m_PlanetManager.m_Planet.InitializeRandomCrust(); // random plates initialization
                    m_PlanetManager.RenderPlanet(); // draw the mesh according to set render mode
                }
                if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0)
                {
                    m_PlanetManager.m_StepMovePlates = GUILayout.Toggle(m_PlanetManager.m_StepMovePlates, "Move plates");
                    m_PlanetManager.m_StepSubductionUplift = GUILayout.Toggle(m_PlanetManager.m_StepSubductionUplift, "Subduction uplift");
                    if (m_PlanetManager.m_StepSubductionUplift)
                    {
                        m_PlanetManager.m_StepSlabPull = GUILayout.Toggle(m_PlanetManager.m_StepSlabPull, "Slab pull");
                    }
                    m_PlanetManager.m_StepErosionDamping = GUILayout.Toggle(m_PlanetManager.m_StepErosionDamping, "Erosion and damping");
                    if (m_PlanetManager.m_StepErosionDamping)
                    {
                        m_PlanetManager.m_SedimentAccretion = GUILayout.Toggle(m_PlanetManager.m_SedimentAccretion, "Sediment accretion");
                    }
                    m_PlanetManager.m_ContinentalCollisions = GUILayout.Toggle(m_PlanetManager.m_ContinentalCollisions, "Continental collisions");
                    m_PlanetManager.m_PlateRifting = GUILayout.Toggle(m_PlanetManager.m_PlateRifting, "Plate rifting");


                    if (GUILayout.Button("Resample crust"))
                    {
                        m_PlanetManager.m_Planet.ResampleCrust(); // recalculate data mesh filling in missing samples
                        m_PlanetManager.RenderPlanet(); // draw the mesh according to set render mode
                    }
                }
                GUILayout.EndVertical(); // tectonic plates tools box end
            }

            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0)
            {
                if (GUILayout.Button("Tectonic step")) // iterate tectonic motion
                {
                    for (int i = 0; i < m_PlanetManager.m_TectonicIterationSteps; i++)
                    {
                        m_PlanetManager.m_Planet.TectonicStep(); // do stuff
                    }
                    m_PlanetManager.RenderPlanet(); // draw the mesh according to set render mode
                }
            }

            GUILayout.Space(10); // component spacing
            m_PlanetManager.m_FoldoutDataManipulation = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutDataManipulation, "Data manipulation:");
            if (m_PlanetManager.m_FoldoutDataManipulation)
            {
                GUILayout.BeginVertical("box"); // main data tools box begin
                if (GUILayout.Button("Clear/reinitialize buffers"))
                {
                    m_PlanetManager.m_Planet.InitializeCBuffers();
                }
                if (GUILayout.Button("Generate Fractal Terrain"))
                {
                    m_PlanetManager.m_Planet.GenerateFractalTerrain();
                    m_PlanetManager.RenderPlanet(); // draw the mesh according to set render mode
                }
                if (GUILayout.Button("Smooth elevation"))
                {
                    m_PlanetManager.m_Planet.SmoothElevation();
                    m_PlanetManager.RenderPlanet(); // draw the mesh according to set render mode
                }
                if (GUILayout.Button("Laplacian smooth elevation"))
                {
                    m_PlanetManager.m_Planet.LaplacianSmoothElevation();
                    m_PlanetManager.RenderPlanet(); // draw the mesh according to set render mode
                }
                if (GUILayout.Button("Initialize RNG"))
                {
                    m_PlanetManager.m_Random.RandomInit(m_PlanetManager.m_RandomSeed); // initialize Mersenne RNG using given seed
                }
                if (GUILayout.Button("Calculate thickness"))
                {
                    m_PlanetManager.m_Planet.CalculateThickness();
                }
                GUILayout.EndVertical(); // main data tools box end
            }

            GUILayout.Space(10); // component spacing
            m_PlanetManager.m_FoldoutDiagnostics = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutDiagnostics, "Diagnostics:");
            if (m_PlanetManager.m_FoldoutDiagnostics)
            {
                GUILayout.BeginVertical("box"); // main data tools box begin
                if (GUILayout.Button("BVH Diagnostics"))
                {
                    m_PlanetManager.m_Planet.BVHDiagnostics();
                }
                if (GUILayout.Button("Elevation value diagnostics"))
                {
                    m_PlanetManager.m_Planet.ElevationValueDiagnostics();
                }
                if (GUILayout.Button("Triangle Collision Test"))
                {
                    m_PlanetManager.CAPTriangleCollisionTestTexture(); // test triangle collision of random two triangles
                }
                GUILayout.EndVertical(); // main data tools box end
            }
        }

        GUILayout.Space(10); // component spacing
        m_PlanetManager.m_FoldoutWIPTools = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutWIPTools, "WIP tools:");
        if (m_PlanetManager.m_FoldoutWIPTools)
        {
            GUILayout.Space(10); // component spacing
            GUILayout.BeginVertical("box"); // main data tools box begin

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
            GUILayout.EndVertical(); // main data tools box end
        }



    }
    private void OnEnable()
    {
        m_PlanetManager = (PlanetManager) target; // set the manager object to the editor target
    }
}
