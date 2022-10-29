using System;
using UnityEngine;

public enum TexOverlay
{
    None, BasicTerrain, CrustPlates, CrustAge, Orogeny, ElevationLaplacian, DebugVectorNoise, DebugDataIndividualTriangles, DebugDataTriangles, DebugDataFailedTriangles, DebugCrustIndividualTriangles, DebugCrustTriangles, DebugCrustFailedTriangles
}

public enum RenderMode
{
    Crust, Data, Render
}

/// <summary>
/// Unity script holding the whole project together. Keeps track of planet data, settings and imported shaders. Relies on GUI Editor controls.
/// Attached to a GameObject named 'Planet'
/// </summary>
[Serializable]
public class PlanetManager : MonoBehaviour
{
    [HideInInspector] public GameObject m_Surface = null; // Rendered GameObject in Unity
    public TectonicPlanet m_Planet = null; // Object containing all planetary data

    public string m_DataMeshFilename = ""; // filename with sphere Delauney triangulation & topology - data layer
    public string m_RenderMeshFilename = ""; // filename with sphere Delauney triangulation & topology - render layer
    public string m_SaveFilename = ""; // filename to save & restore planetary data
    public string m_TextureSaveFilenamePNG = ""; // filename to save & restore planetary data

    public uint m_RandomSeed = 0; // RNG initialization seed
    public int m_TectonicIterationSteps = 10; // number of tectonic steps taken at a time
    public float m_ElevationScaleFactor = 1; // rendering option to emphasize terrain

    public RandomMersenne m_Random; // RNG instance

    public SimulationSettings m_Settings = new SimulationSettings(); // settings instance for the editor
    public SimulationShaders m_Shaders = new SimulationShaders(); // shader import object instance for the editor
    public DRFileManager m_FileManager = null; // file manager instance - initialized in PlanetEditor

    [HideInInspector] public TexOverlay m_TextureOverlay = TexOverlay.None; // Texture switch
    [HideInInspector] public RenderMode m_RenderMode = RenderMode.Render; // Render switch - keep at RenderMode.Render unless testing
    [HideInInspector] public bool m_OverlayOnRender = true; // paint texture when rendering m_Surface
    [HideInInspector] public bool m_PropagateCrust = true; // propagate data from crust layer to data layer
    [HideInInspector] public bool m_PropagateData = true; // propagate data from data layer to render layer
    [HideInInspector] public bool m_ClampToOceanLevel = false; // render ocean as a zero elevation terrain

    [HideInInspector] public bool m_StepMovePlates = true; // move tectonic plates on TectonicStep (oTS)
    [HideInInspector] public bool m_StepSubductionUplift = true; // calculate and apply subduction of plates oTS
    [HideInInspector] public bool m_StepSlabPull = true; // plate rotation axis drag by subduction oTS
    [HideInInspector] public bool m_StepErosionDamping = false; // erode continents and damp ocean floors oTS
    [HideInInspector] public bool m_SedimentAccretion = false; // sediment accretion on ocean floors (changed to trenches) oTS
    [HideInInspector] public bool m_ContinentalCollisions = false; // calculate and apply continental collisins oTS
    [HideInInspector] public bool m_PlateRifting = false; // occasional plate rifts on largest plates oTS

    // editor variables - foldouts for GUI compartmentalization
    [HideInInspector] public bool m_FoldoutRenderOptions = false;
    [HideInInspector] public bool m_FoldoutTectonics = false;
    [HideInInspector] public bool m_FoldoutDataManipulation = false;
    [HideInInspector] public bool m_FoldoutDiagnostics = false;
    [HideInInspector] public bool m_FoldoutWIPTools = false;

    /// <summary>
    /// Testing function - slot 1.
    /// </summary>
    public void DebugFunction()
    {
    }

    /// <summary>
    /// Testing function - slot 2.
    /// </summary>
    public void DebugFunction2()
    {
    }

    /// <summary>
    /// Testing function - slot 3.
    /// </summary>
    public void DebugFunction3()
    {
    }

    /// <summary>
    /// Testing function - slot 4.
    /// </summary>
    public void DebugFunction4()
    {
    }

    /// <summary>
    /// Placeholder because MonoBehaviour.
    /// </summary>
    void Start()
    {
    }

    /// <summary>
    /// Placeholder because MonoBehaviour.
    /// </summary>
    void Update()
    {
        
    }

    /// <summary>
    /// Set up a new render object unless already existing, load topology from a template file and create basic vector noise over triangles.
    /// </summary>
    public void LoadNewPlanet()
    {
        if (m_Surface == null) // if no render object
        {
            m_Surface = new GameObject("Surface"); // create new GameObject to render
            m_Surface.transform.parent = transform; // set to Planet GameObject transform
            MeshFilter newMeshFilter = m_Surface.AddComponent<MeshFilter>(); // new mesh reference because the rendered object is created from topology data
            m_Surface.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Custom/SphereTextureShader")); // custom shader for correct texturing
            newMeshFilter.sharedMesh = new Mesh(); // set a new mesh
        }
        if (m_Random == null) // initialize a new RNG if it does not exist
        {
            m_Random = new RandomMersenne(m_RandomSeed);
        }
        m_Planet = new TectonicPlanet(m_Settings.PlanetRadius); // create a new planetary object
        m_Planet.LoadDefaultTopology(m_DataMeshFilename, m_RenderMeshFilename); // load a template file
        m_Planet.CreateVectorNoise(); // create a vector noise overlay
        m_Planet.InitializeCBuffers(); // initialize persistent shader buffers
        RenderPlanet(); // show planet
    }

    /// <summary>
    /// Unified function for planet rendering, with switched overlays and different layers.
    /// </summary>
    public void RenderPlanet()
    {
        MeshFilter meshFilter = m_Surface.GetComponent<MeshFilter>(); // mesh to be altered
        meshFilter.sharedMesh.Clear(); // clear old mesh
        Vector3[] vertices; // vertex array to be fed to the mesh
        int[] triangles; // triangle array to be fed to the mesh
        switch (m_RenderMode) // render is driven by the RenderMode switch variable
        {
            case RenderMode.Crust:
                if (m_Planet.m_TectonicPlates.Count > 0) // render crust layer only when there are tectonic plates
                {
                    m_Planet.CrustMesh(out vertices, out triangles);
                }
                else
                {
                    Debug.Log("No tectonic plates, rendering data layer.");
                    m_RenderMode = RenderMode.Data; // switch if no crust layer
                    m_Planet.DataMesh(out vertices, out triangles, m_PropagateCrust);
                }
                break;
            case RenderMode.Data:
                m_Planet.DataMesh(out vertices, out triangles, m_PropagateCrust);
                break;
            case RenderMode.Render:
                m_Planet.NormalMesh(out vertices, out triangles, m_PropagateData, m_PropagateCrust);
                break;
            default:
                m_Planet.NormalMesh(out vertices, out triangles, m_PropagateData, m_PropagateCrust);
                break;
        }
        meshFilter.sharedMesh.vertices = vertices; // assigned vertices from mesh functions
        meshFilter.sharedMesh.triangles = triangles; // assigned triangles from mesh functions
        meshFilter.sharedMesh.RecalculateNormals(); // recalculate normals on the readied mesh
        if (m_OverlayOnRender) // if an overlay is allowed, switch between functions that create a texture
        {
            Texture2D tex = null; // value assigned by the texturing functions - all use compute shaders
            bool problem = false; // detect missing crust on crust plates overlay etc.
            bool no_overlay = false; // overlay presence switch - on false remove texture
            switch (m_TextureOverlay)
            {
                case TexOverlay.None: // wash texture
                    no_overlay = true;
                    break;
                case TexOverlay.BasicTerrain: // green on points above or equal to zero elevation, blue below
                    if (m_RenderMode == RenderMode.Crust)
                    {
                        if (m_PropagateCrust)
                        {
                            m_Planet.CrustToData(); // interpolate crust layer to data
                        }
                        else
                        {
                            problem = true; // no interpolation available or disabled

                        }
                    }
                    if (problem)
                    {
                        tex = MissingDataTexture(); // return default missing data texture
                    }
                    else
                    {
                        tex = OverlayBasicTerrain(); // return texture normally
                    }
                    break;
                case TexOverlay.DebugDataIndividualTriangles: // paint individual triangles on data layer
                    tex = OverlayDebugDataIndividualTriangles();
                    break;
                case TexOverlay.DebugDataTriangles: // paint individual triangles on data layer
                    tex = OverlayDebugDataTriangles();
                    break;
                case TexOverlay.DebugDataFailedTriangles: // paint individual contrast points on data layer
                    tex = OverlayDebugDataFailedTriangles();
                    break;
                case TexOverlay.DebugCrustIndividualTriangles: // paint individual transformed crust plate triangles on data layer
                    if (m_Planet.m_TectonicPlates.Count == 0)
                    {
                        Debug.LogError("Debug Crust Triangle overlay impossible when there are no plates.");
                        problem = true;
                    }
                    if (problem)
                    {
                        tex = MissingDataTexture();
                    }
                    else
                    {
                        tex = OverlayDebugCrustIndividualTriangles();
                    }
                    break;
                case TexOverlay.DebugCrustTriangles: // paint individual transformed crust plate triangles on data layer
                    if (m_Planet.m_TectonicPlates.Count == 0)
                    {
                        Debug.LogError("Debug Crust Triangle overlay impossible when there are no plates.");
                        problem = true;
                    }
                    if (problem)
                    {
                        tex = MissingDataTexture();
                    }
                    else
                    {
                        tex = OverlayDebugCrustTriangles();
                    }
                    break;
                case TexOverlay.DebugCrustFailedTriangles: // paint contrast points
                    if (m_Planet.m_TectonicPlates.Count == 0)
                    {
                        Debug.LogError("Debug Crust Triangle overlay impossible when there are no plates.");
                        problem = true;
                    }
                    if (problem)
                    {
                        tex = MissingDataTexture();
                    }
                    else
                    {
                        tex = OverlayDebugCrustFailedTriangles();
                    }
                    break;
                case TexOverlay.CrustPlates: // paint transformed crust plates on data layer
                    if (m_Planet.m_TectonicPlates.Count == 0)
                    {
                        Debug.LogError("Plate borders overlay impossible when there are no plates.");
                        problem = true;
                    }
                    if (problem)
                    {
                        tex = MissingDataTexture();
                    }
                    else
                    {
                        tex = OverlayCrustPlates();
                    }
                    break;
                case TexOverlay.CrustAge: // paint crust age on data layer
                    if (m_Planet.m_TectonicPlates.Count == 0)
                    {
                        Debug.LogError("Crust age overlay impossible when there are no plates.");
                        problem = true;
                    }
                    if (problem)
                    {
                        tex = MissingDataTexture();
                    }
                    else
                    {
                        tex = OverlayCrustAge();
                    }
                    break;
                case TexOverlay.Orogeny: // paint crust orogeny on data layer
                    if (m_Planet.m_TectonicPlates.Count == 0)
                    {
                        Debug.LogError("Crust age overlay impossible when there are no plates.");
                        problem = true;
                    }
                    if (problem)
                    {
                        tex = MissingDataTexture();
                    }
                    else
                    {
                        tex = OverlayOrogeny();
                    }
                    break;
                case TexOverlay.ElevationLaplacian: // paint elevation values graph laplacian on data layer
                    if (m_RenderMode == RenderMode.Crust)
                    {
                        if (m_PropagateCrust)
                        {
                            m_Planet.CrustToData();
                        }
                        else
                        {
                            problem = true;

                        }
                    }
                    if (problem)
                    {
                        tex = MissingDataTexture();
                    }
                    else
                    {
                        tex = OverlayElevationLaplacian();
                    }
                    break;
                case TexOverlay.DebugVectorNoise: // paint a representation of vector noise on data layer - hue is longitude angle, saturation length
                    tex = OverlayDebugVectorNoise();
                    break;
                default: // wash texture if unknown overlay
                    no_overlay = true;
                    break;
            }
            if (no_overlay) // wash when no overlay
            {
                m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the planet texture
                GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the plane texture
            } else // normal function - texture the object, missing data or error texture included
            {
                GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", tex); // set the planet texture
                m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", tex); // set the plane texture
            }
        } else // if overlay is switched off, wash texture
        {
            m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the planet texture
            GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null); // remove the plane texture
        }
    }

    /// <summary>
    /// Single color texture if data is missing or other unspecified error.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D MissingDataTexture()
    {
        Texture2D tex = new Texture2D(4096,4096); // all textures are 4096x4096
        for (int i = 0; i < tex.height; i++)
        {
            for (int j = 0; j < tex.width; j++)
            {
                tex.SetPixel(i, j, Color.red); // red color by default
            }
        }
        tex.Apply ();
        return tex;

    }

    /// <summary>
    /// Simple green on non-negative elevation, blue for negative.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayBasicTerrain()
    {

        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader; // unified shader variable

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureBasicTerrain"); // kernel handle switch

        m_Planet.UpdateCBBuffers(); // update persistent buffers
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]); // basic data vertices positions for interpolating
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_Planet.m_CBuffers["data_vertex_data"]); // PointData information 

        work_shader.SetInt("n_data_vertices", m_Planet.m_VerticesCount); // number of data vertices, used for determining thread IDs bijection to data vertices

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_Planet.m_CBuffers["data_BVH"]); // bounding volume hiearchy array for point look-up
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_Planet.m_CBuffers["data_triangles"]); // data triangles for look-up and final interpolation

        RenderTexture com_tex = new RenderTexture(4096, 4096, 24); // initialize a uniform 4096 x 4096 texture
        com_tex.enableRandomWrite = true; // pixels will be rewritten by the shader output
        com_tex.Create(); // initialize texture in memory


        work_shader.SetTexture(kernelHandle, "Result", com_tex); // link texture to shader output
        work_shader.Dispatch(kernelHandle, 256, 1024, 1); // run the shader - ids correspond to thread batches to correctly identify individual pixels (1 pixel per thread)

        RenderTexture.active = com_tex; // assign the texture to currently active render texture
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height); // create new Texture2D
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0); // read the shader output into the new texture
        RenderTexture.active = null; // unassign the currently active  render texture
        com_tex.Release(); // release the shader texture
        tex.Apply(); // apply the new texture changes
        return tex; // return texture

    }

    /// <summary>
    /// Colored data triangles. Sorting visible on hue patterns as the hue is rotated in order.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayDebugDataIndividualTriangles()
    {

        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureDebugDataIndividualTriangles");

        m_Planet.UpdateCBBuffers();
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_Planet.m_CBuffers["data_vertex_data"]);

        work_shader.SetInt("n_data_vertices", m_Planet.m_VerticesCount);

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_Planet.m_CBuffers["data_BVH"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_Planet.m_CBuffers["data_triangles"]);


        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;

    }

    /// <summary>
    /// Colored data triangles. Hue and saturation correspond to longitude and latitude.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayDebugDataTriangles()
    {

        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureDebugDataTriangles");

        m_Planet.UpdateCBBuffers();
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_Planet.m_CBuffers["data_vertex_data"]);

        work_shader.SetInt("n_data_vertices", m_Planet.m_VerticesCount);

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_Planet.m_CBuffers["data_BVH"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_Planet.m_CBuffers["data_triangles"]);


        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;

    }

    /// <summary>
    /// Contrast points where data triangle look-up failed.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayDebugDataFailedTriangles()
    {

        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureDebugDataFailedTriangles");

        m_Planet.UpdateCBBuffers();
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_Planet.m_CBuffers["data_vertex_data"]);

        work_shader.SetInt("n_data_vertices", m_Planet.m_VerticesCount);

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_Planet.m_CBuffers["data_BVH"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_Planet.m_CBuffers["data_triangles"]);


        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;

    }

    /// <summary>
    /// Paint top crust plates on data layer, hue scaled to number of plates.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayCrustPlates()
    {
        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureCrustPlates");

        m_Planet.UpdateCBBuffers();
        work_shader.SetBuffer(kernelHandle, "crust_vertex_locations", m_Planet.m_CBuffers["crust_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "crust_triangles", m_Planet.m_CBuffers["crust_triangles"]);
        work_shader.SetBuffer(kernelHandle, "crust_vertex_data", m_Planet.m_CBuffers["crust_vertex_data"]);

        /// CHECK!!!
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_Planet.m_CBuffers["data_triangles"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_Planet.m_CBuffers["data_vertex_data"]);
        work_shader.SetInt("n_plates", m_Planet.m_TectonicPlatesCount);

        work_shader.SetBuffer(kernelHandle, "overlap_matrix", m_Planet.m_CBuffers["overlap_matrix"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH_sps", m_Planet.m_CBuffers["crust_BVH_sps"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH", m_Planet.m_CBuffers["crust_BVH"]);
        work_shader.SetBuffer(kernelHandle, "plate_transforms", m_Planet.m_CBuffers["plate_transforms"]);

        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetInt("trianglesNumber", m_Planet.m_TrianglesCount);
        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;

    }

    /// <summary>
    /// Paint hue scaled crust age on data layer, red is older
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayCrustAge()
    {
        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureCrustAge");

        m_Planet.UpdateCBBuffers();

        work_shader.SetFloat("tectonic_iteration_step_time", m_Settings.TectonicIterationStepTime);
        work_shader.SetInt("total_tectonic_steps_taken", m_Planet.m_TotalTectonicStepsTaken);
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_Planet.m_CBuffers["data_vertex_data"]);

        work_shader.SetInt("n_data_vertices", m_Planet.m_VerticesCount);

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_Planet.m_CBuffers["data_BVH"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_Planet.m_CBuffers["data_triangles"]);


        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        //work_shader.SetInt("trianglesNumber", m_Planet.m_TrianglesCount);
        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;

    }

    /// <summary>
    /// Paint hue differentiated orogeny, red is unknown, green Andean and blue Himalayan
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayOrogeny()
    {
        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureOrogeny");

        m_Planet.UpdateCBBuffers();

        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_Planet.m_CBuffers["data_vertex_data"]);

        work_shader.SetInt("n_data_vertices", m_Planet.m_VerticesCount);

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_Planet.m_CBuffers["data_BVH"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_Planet.m_CBuffers["data_triangles"]);


        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Colored crust triangles. Sorting visible on hue patterns as the hue is rotated in order.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayDebugCrustIndividualTriangles()
    {

        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureDebugCrustIndividualTriangles");

        m_Planet.UpdateCBBuffers();
        work_shader.SetBuffer(kernelHandle, "crust_vertex_locations", m_Planet.m_CBuffers["crust_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "crust_triangles", m_Planet.m_CBuffers["crust_triangles"]);
        work_shader.SetBuffer(kernelHandle, "crust_vertex_data", m_Planet.m_CBuffers["crust_vertex_data"]);
        work_shader.SetInt("n_plates", m_Planet.m_TectonicPlatesCount);

        work_shader.SetBuffer(kernelHandle, "overlap_matrix", m_Planet.m_CBuffers["overlap_matrix"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH_sps", m_Planet.m_CBuffers["crust_BVH_sps"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH", m_Planet.m_CBuffers["crust_BVH"]);
        work_shader.SetBuffer(kernelHandle, "plate_transforms", m_Planet.m_CBuffers["plate_transforms"]);


        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetInt("trianglesNumber", m_Planet.m_TrianglesCount);
        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Colored crust triangles. Hue and saturation correspond to longitude and latitude.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayDebugCrustTriangles()
    {

        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureDebugCrustTriangles");

        m_Planet.UpdateCBBuffers();
        work_shader.SetBuffer(kernelHandle, "crust_vertex_locations", m_Planet.m_CBuffers["crust_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "crust_triangles", m_Planet.m_CBuffers["crust_triangles"]);
        work_shader.SetBuffer(kernelHandle, "crust_vertex_data", m_Planet.m_CBuffers["crust_vertex_data"]);
        work_shader.SetInt("n_plates", m_Planet.m_TectonicPlatesCount);

        work_shader.SetBuffer(kernelHandle, "overlap_matrix", m_Planet.m_CBuffers["overlap_matrix"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH_sps", m_Planet.m_CBuffers["crust_BVH_sps"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH", m_Planet.m_CBuffers["crust_BVH"]);
        work_shader.SetBuffer(kernelHandle, "plate_transforms", m_Planet.m_CBuffers["plate_transforms"]);


        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetInt("trianglesNumber", m_Planet.m_TrianglesCount);
        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Contrast points where crust triangle look-up failed.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayDebugCrustFailedTriangles()
    {

        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureDebugCrustFailedTriangles");

        m_Planet.UpdateCBBuffers();
        work_shader.SetBuffer(kernelHandle, "crust_vertex_locations", m_Planet.m_CBuffers["crust_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "crust_triangles", m_Planet.m_CBuffers["crust_triangles"]);
        work_shader.SetBuffer(kernelHandle, "crust_vertex_data", m_Planet.m_CBuffers["crust_vertex_data"]);
        work_shader.SetInt("n_plates", m_Planet.m_TectonicPlatesCount);

        work_shader.SetBuffer(kernelHandle, "overlap_matrix", m_Planet.m_CBuffers["overlap_matrix"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH_sps", m_Planet.m_CBuffers["crust_BVH_sps"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH", m_Planet.m_CBuffers["crust_BVH"]);
        work_shader.SetBuffer(kernelHandle, "plate_transforms", m_Planet.m_CBuffers["plate_transforms"]);


        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetInt("trianglesNumber", m_Planet.m_TrianglesCount);
        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Paint elevation values graph laplacian. Redder hue indicates more extreme values.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayElevationLaplacian()
    {
        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureElevationLaplacian");

        m_Planet.UpdateCBBuffers();

        int n_vertices = m_Planet.m_DataVertices.Count;
        float[] el_values = new float[n_vertices];
        float el, el_max, el_min;
        el_max = Mathf.NegativeInfinity;
        el_min = Mathf.Infinity;
        for (int i = 0; i < n_vertices; i++)
        {
            el = 0;
            foreach (int it in m_Planet.m_DataVerticesNeighbours[i])
            {
                el += m_Planet.m_DataPointData[it].elevation - m_Planet.m_DataPointData[i].elevation;
            }
            el_values[i] = el;
            el_max = el > el_max ? el : el_max;
            el_min = el < el_min ? el : el_min;
        }

        ComputeBuffer el_values_buffer = new ComputeBuffer(n_vertices, 4);

        el_values_buffer.SetData(el_values);

        work_shader.SetBuffer(kernelHandle, "el_values", el_values_buffer);

        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_Planet.m_CBuffers["data_vertex_data"]);

        work_shader.SetInt("n_data_vertices", n_vertices);
        work_shader.SetFloat("el_min", el_min);
        work_shader.SetFloat("el_max", el_max);

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_Planet.m_CBuffers["data_BVH"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_Planet.m_CBuffers["data_triangles"]);


        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        el_values_buffer.Release();

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;

    }

    /// <summary>
    /// Paint vector noise values at triangles. hue represents relative angle, saturation length value.
    /// It is flawed, there is a hue bias. However, it gives sufficient representation of the noise.
    /// </summary>
    /// <returns>Texture2D to be applied to the surface of the sphere and the plane.</returns>
    public Texture2D OverlayDebugVectorNoise()
    {

        ComputeShader work_shader = m_Shaders.m_OverlayTextureShader;

        int kernelHandle = work_shader.FindKernel("CSOverlayTextureDebugVectorNoise");

        Vector3[] vector_noise = m_Planet.m_VectorNoise.ToArray();

        ComputeBuffer vector_noise_buffer = new ComputeBuffer(vector_noise.Length, 12, ComputeBufferType.Default);

        vector_noise_buffer.SetData(vector_noise);

        m_Planet.UpdateCBBuffers();
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_Planet.m_CBuffers["data_vertex_locations"]);

        work_shader.SetInt("n_data_vertices", m_Planet.m_VerticesCount);

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_Planet.m_CBuffers["data_BVH"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_Planet.m_CBuffers["data_triangles"]);

        work_shader.SetBuffer(kernelHandle, "vector_noise", vector_noise_buffer);

        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        work_shader.SetTexture(kernelHandle, "Result", com_tex);
        work_shader.Dispatch(kernelHandle, 256, 1024, 1);

        vector_noise_buffer.Release();

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        return tex;
    }
}

