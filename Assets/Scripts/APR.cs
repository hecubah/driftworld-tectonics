using UnityEngine;

[System.Serializable]
public class SimulationSettings // single object for simulation parameters, makes it foldable in GUI Editor
{
    public float PlanetRadius = 6.37f; // multiple of 1000 km

    public int PlateInitNumberOfCentroids = 40; // number of initial tectonic plates (Voronoi centers)
    public float TectonicIterationStepTime = 2; // simulation time-step in My, def: 2
    public float MaximumPlateSpeed = 0.0157f; // maximum plate angular speed
    public float InitialOceanicDepth = -0.004f; // elevation parameter for initially created oceanic plates
    public float InitialContinentalAltitude = 0.001f; // elevation parameter for initially created continental plates
    public float InitialContinentalProbability = 0.0f; // probability a new plate is continental for new crust initialization
    public float NewCrustThickness = 0.01f; // initial crust thickness on generated points
    public int VectorNoiseAveragingIterations = 3; // number of iterations in averaging the vector noise
    public int VoronoiBorderNoiseIterations = 6; // number of iterations in plate border perturbations

    public int BVHConstructionRadius = 20; // array range for Morton code nearest neighbour look-up

    public float AbyssalPlainsElevation = -0.006f; // natural elevation for oceanic points far from oceanic ridge
    public float AverageOceanicDepth = -0.004f; // currently it is used as a threshold elevation for sediment accretion
    public float HighestContinentalAltitude = 0.01f; // highest possible continental elevation
    public float HighestOceanicRidgeElevation = -0.001f; // divergent plate ridge top elevation
    public float OceanicRidgeElevationFalloff = 0.05f; // new oceanic crust ridge distance exponential scaling parameter
    public float OceanicTrenchElevation = -0.01f; // lowest possible oceanic elevation

    public float ContinentalCollisionGlobalDistance = 0.659f; // global terrane collision influence parameter
    public float ContinentalCollisionCoefficient = 0.013f; // global collision elevation parameter

    public float SubductionDistanceTransferControlDistance = 0.10f; // shaping subduction cubic distance parameter
    public float SubductionDistanceTransferMaxDistance = 0.283f; // maximum subduction distance
    public float SubductionUplift = 6e-4f; // global subduction uplift parameter
    public float SlabPullPerturbation = 0.1f; // global rotation axis slab pull perturbation parameter
    public float OceanicElevationDamping = 4e-5f; // global oceanic elevation damping parameter
    public float ContinentalErosion = 3e-5f; // global continental erosion parameter
    public float SedimentAccretion = 3e-4f; // global sediment accretion parameter

    public float PlateRiftsPerTimeUnit = 0.1f; // global average rifting frequency of plates per My

    public int FractalTerrainIterations = 10000; // how many times the fractal terrain generator (FTG) shader should iterate
    public float FractalTerrainElevationStep = 0.003f; // elementary elevation change per FTG iteration
    public float NeighbourSmoothWeight = 0.1f; // weight for neighbour elevations for smoothing
    public float TerrainHeightIncrement = 0.0001f; // Forced terrain height change increment

}

[System.Serializable]
public class SimulationShaders
{
    public ComputeShader m_FractalTerrainShader = null; // computes fractal terrain elevations
    public ComputeShader m_BVHNearestNeighbourShader = null; // searches for nearest neighbours in sorted morton code sequences
    public ComputeShader m_VertexDataInterpolationShader = null; // interpolates from crust layer to data and from data layer to render
    public ComputeShader m_PlateInteractionsShader = null; // tectonic step kernel collection
    public ComputeShader m_OverlayTextureShader = null; // returns different overlays
}
