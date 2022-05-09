using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetManager))]
public class PlanetEditor : Editor
{
    PlanetManager m_PlanetManager; // respective planet manager to operate on

    public override void OnInspectorGUI() // editor tools for manipulating the simulation
    {
        base.OnInspectorGUI(); // base function
        GUILayout.BeginVertical("box"); // component aligning
        bool planet_is_loaded = (m_PlanetManager.m_Planet != null); // planet load check variable
        GUILayout.Label("Planet: " + (planet_is_loaded ? "present" : "none")); // check if a planet is loaded
        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Label("Mesh/RenderMesh vertices count: " + (m_PlanetManager.m_Planet.m_DataVertices != null ? m_PlanetManager.m_Planet.m_VerticesCount.ToString() : "null") + "/" + (m_PlanetManager.m_Planet.m_RenderVertices != null ? m_PlanetManager.m_Planet.m_RenderVerticesCount.ToString() : "null")); // number of vertices
            GUILayout.Label("Mesh/RenderMesh triangles count: " + (m_PlanetManager.m_Planet.m_DataTriangles != null ? m_PlanetManager.m_Planet.m_TrianglesCount.ToString() : "null") + "/" + (m_PlanetManager.m_Planet.m_RenderTriangles != null ? m_PlanetManager.m_Planet.m_RenderTrianglesCount.ToString() : "null")); // number of triangles
            GUILayout.Label("Render mode: " + m_PlanetManager.m_RenderMode.ToString()); // render mode
            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0) // if there are tectonic plates initialized
            {
                GUILayout.Label("Tectonic plates count: " + m_PlanetManager.m_Planet.m_TectonicPlates.Count.ToString()); // number of tectonic plates
                GUILayout.Label("Tectonic steps taken without resample: " + m_PlanetManager.m_Planet.m_TectonicStepsTakenWithoutResample); // days since last topological mess
                GUILayout.Label("Total tectonic steps taken: " + m_PlanetManager.m_Planet.m_TotalTectonicStepsTaken); // total tectonic simulation steps
            }
        }
        GUILayout.EndVertical(); // component aligning
        if (GUILayout.Button("Load new planet")) // start button to load fresh planet
        {
            m_PlanetManager.LoadNewPlanet(); // manager call for new mesh and topology
        }
        if (planet_is_loaded)
        {
            if (GUILayout.Button("Save planet to file")) // save simulated data to a file - needed form memory leak purposes, at least at 500k sample size
            {
                SaveManager.SavePlanet(m_PlanetManager); // manager call - since SaveManager is static, provide a PlanetManager reference
            }

        }
        if (GUILayout.Button("Load planet from file")) // load simulated data from a file
        {
            SaveManager.LoadPlanet(m_PlanetManager); // static manager call
        }

        if (planet_is_loaded) // if a planet is loaded
        {
            GUILayout.Space(10); // component spacing
            m_PlanetManager.m_FoldoutRenderOptions = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutRenderOptions, "Render options:"); // foldout switch
            if (m_PlanetManager.m_FoldoutRenderOptions) // check internal foldout variable
            {
                GUILayout.BeginVertical("box");
                if (m_PlanetManager.m_Planet.m_TectonicPlatesCount > 0)
                {
                    m_PlanetManager.m_PropagateCrust = GUILayout.Toggle(m_PlanetManager.m_PropagateCrust, "Auto-propagate crust"); // toggle if the crust layer should interpolate automatically to data layer
                }
                m_PlanetManager.m_PropagateData = GUILayout.Toggle(m_PlanetManager.m_PropagateData, "Auto-propagate data"); // toggle if the data layer should interpolate automatically to render layer
                m_PlanetManager.m_ClampToOceanLevel = GUILayout.Toggle(m_PlanetManager.m_ClampToOceanLevel, "Clamp to ocean level"); // toggle if the elevation is to be clamped to ocean level
                m_PlanetManager.m_OverlayOnRender = GUILayout.Toggle(m_PlanetManager.m_OverlayOnRender, "Paint overlay on render"); // repaint chosen overlay every time the planet is rendered
                GUILayout.BeginHorizontal();
                GUILayout.Label("Overlay: ");
                m_PlanetManager.m_TextureOverlay = (TexOverlay)EditorGUILayout.EnumPopup(m_PlanetManager.m_TextureOverlay); // choose overlay
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Render mode: ");
                m_PlanetManager.m_RenderMode = (RenderMode)EditorGUILayout.EnumPopup(m_PlanetManager.m_RenderMode); // chose render mode
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Wash Texture")) // wash texture
                {
                    m_PlanetManager.m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the planet texture
                    GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the plane texture
                }
                GUILayout.EndVertical();
            }

            if (GUILayout.Button("Render surface")) // force planet render
            {
                m_PlanetManager.RenderPlanet(); // manager render call
            }


            GUILayout.Space(10);
            m_PlanetManager.m_FoldoutTectonics = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutTectonics, "Tectonic tools:");
            if (m_PlanetManager.m_FoldoutTectonics)
            {
                GUILayout.BeginVertical("box");
                if (GUILayout.Button("Initialize tectonic plates")) // fresh tectonic plates system
                {
                    m_PlanetManager.m_Planet.InitializeRandomCrust(); // random plates initialization
                    m_PlanetManager.RenderPlanet(); // render after changes
                }
                if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0)
                {
                    m_PlanetManager.m_StepMovePlates = GUILayout.Toggle(m_PlanetManager.m_StepMovePlates, "Move plates"); // change plate transform every step
                    m_PlanetManager.m_StepSubductionUplift = GUILayout.Toggle(m_PlanetManager.m_StepSubductionUplift, "Subduction uplift"); // perform subductions
                    if (m_PlanetManager.m_StepSubductionUplift)
                    {
                        m_PlanetManager.m_StepSlabPull = GUILayout.Toggle(m_PlanetManager.m_StepSlabPull, "Slab pull"); // perform slab pulls
                    }
                    m_PlanetManager.m_StepErosionDamping = GUILayout.Toggle(m_PlanetManager.m_StepErosionDamping, "Erosion and damping"); // perform erosion and damping
                    if (m_PlanetManager.m_StepErosionDamping)
                    {
                        m_PlanetManager.m_SedimentAccretion = GUILayout.Toggle(m_PlanetManager.m_SedimentAccretion, "Sediment accretion"); // perform sediment accretion
                    }
                    m_PlanetManager.m_ContinentalCollisions = GUILayout.Toggle(m_PlanetManager.m_ContinentalCollisions, "Continental collisions"); // perform continental collisions
                    m_PlanetManager.m_PlateRifting = GUILayout.Toggle(m_PlanetManager.m_PlateRifting, "Plate rifting"); // perform plate rifting


                    if (GUILayout.Button("Resample crust")) // copy data layer into crust layer - a saving grace on many occasions
                    {
                        m_PlanetManager.m_Planet.ResampleCrust();
                        m_PlanetManager.RenderPlanet();
                    }
                }
                GUILayout.EndVertical();
            }

            if (m_PlanetManager.m_Planet.m_TectonicPlates.Count > 0)
            {
                if (GUILayout.Button("Tectonic step")) // iterate tectonic motion
                {
                    for (int i = 0; i < m_PlanetManager.m_TectonicIterationSteps; i++) // repeat the basic tectonic step a number of iterations according to settings
                    {
                        m_PlanetManager.m_Planet.TectonicStep();
                    }
                    m_PlanetManager.RenderPlanet();
                }
            }

            GUILayout.Space(10);
            m_PlanetManager.m_FoldoutDataManipulation = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutDataManipulation, "Data manipulation:");
            if (m_PlanetManager.m_FoldoutDataManipulation)
            {
                GUILayout.BeginVertical("box");
                if (GUILayout.Button("Clear/reinitialize buffers")) // release old buffers and initialize new without setting them
                {
                    m_PlanetManager.m_Planet.InitializeCBuffers();
                }
                if (GUILayout.Button("Generate Fractal Terrain")) // generate a reference fractal terrain
                {
                    m_PlanetManager.m_Planet.GenerateFractalTerrain();
                    m_PlanetManager.RenderPlanet();
                }
                if (GUILayout.Button("Smooth elevation")) // smooth a terrain that is too rough
                {
                    m_PlanetManager.m_Planet.SmoothElevation();
                    m_PlanetManager.RenderPlanet();
                }
                if (GUILayout.Button("Laplacian smooth elevation")) // smooth extreme terrain elevation differentials
                {
                    m_PlanetManager.m_Planet.LaplacianSmoothElevation();
                    m_PlanetManager.RenderPlanet();
                }
                if (GUILayout.Button("Initialize RNG"))
                {
                    m_PlanetManager.m_Random.RandomInit(m_PlanetManager.m_RandomSeed); // set new RNG seed
                }
                if (GUILayout.Button("Calculate thickness")) // calculate thickness across crust, no other mechanism for that yet
                {
                    m_PlanetManager.m_Planet.CalculateThickness();
                }
                if (GUILayout.Button("Forced plate rift"))
                {
                    m_PlanetManager.m_Planet.ForcedPlateRift(); // force plate rift of the largest tectonic plate
                    m_PlanetManager.RenderPlanet();
                }
                if (GUILayout.Button("Export texture")) // save active texture to a file
                {
                    SaveManager.TextureSave(m_PlanetManager);
                }
                GUILayout.EndVertical();
            }

            GUILayout.Space(10);
            m_PlanetManager.m_FoldoutDiagnostics = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutDiagnostics, "Diagnostics:");
            if (m_PlanetManager.m_FoldoutDiagnostics)
            {
                GUILayout.BeginVertical("box");
                if (GUILayout.Button("BVH Diagnostics")) // bounding volume hiearchy diagnostics - references, subsets etc.
                {
                    m_PlanetManager.m_Planet.BVHDiagnostics();
                }
                if (GUILayout.Button("Elevation value diagnostics")) // test elevation values health - differences, inifinities
                {
                    m_PlanetManager.m_Planet.ElevationValueDiagnostics();
                }
                GUILayout.EndVertical();
            }
        }

        GUILayout.Space(10);
        m_PlanetManager.m_FoldoutWIPTools = EditorGUILayout.Foldout(m_PlanetManager.m_FoldoutWIPTools, "WIP tools:");
        if (m_PlanetManager.m_FoldoutWIPTools)
        {
            GUILayout.Space(10);
            GUILayout.BeginVertical("box");

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
            GUILayout.EndVertical();
        }



    }
    private void OnEnable()
    {
        m_PlanetManager = (PlanetManager) target; // set the manager object to the editor target - hack to initialize the manager variable
    }
}
