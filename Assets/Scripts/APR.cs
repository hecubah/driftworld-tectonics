using UnityEngine;

[System.Serializable]
public class SimulationSettings // single object for simulation parameters, makes it foldable in GUI Editor
{
    public int FractalTerrainIterations = 10000; // how many times the fractal terrain generator (FTG) shader should iterate
    public float FractalTerrainElevationStep = 0.003f; // elementary elevation change per FTG iteration
    public int PlateInitNumberOfCentroids = 7; // number of initial tectonic plates (Voronoi centers)

    public float NewCrustThickness = 0.01f; // initial crust thickness on generated points
    public float OceanicRidgeElevationFalloff = 0.05f; // new ocean crust ridge distance exponential scaling parameter
    public int BVHConstructionRadius = 20; // array range for Morton code nearest neighbour look-up
    public float AverageOceanicDepth = -0.004f; // currently it is used as a threshold elevation for sediment accretion
    public float InitialOceanicDepth = -0.004f; // elevation parameter for initially created oceanic plates
    public float InitialContinentalAltitude = 0.001f; // elevation parameter for initially created continental plates
    public float InitialContinentalProbability = 0.3f; // probability a new plate is continental for new crust initialization

    public float TectonicIterationStepTime = 2; // simulation time-step in My, def: 2
    public float PlanetRadius = 6.37f; // multiple of 1000 km
    public float HighestOceanicRidgeElevation = -0.001f; // divergent plate ridge top elevation
    public float OceanicTrenchElevation = -0.01f; // lowest possible ocean elevation
    public float AbyssalPlainsElevation = -0.006f; // natural elevation for ocean points far from ocean ridge
    public float HighestContinentalAltitude = 0.01f; // highest possible continental elevation
    public float SubductionDistanceTransferControlDistance = 0.10f; // shaping subduction cubic distance parameter
    public float SubductionDistanceTransferMaxDistance = 0.28f; // maximum subduction distance
    public float MaximumPlateSpeed = 0.0157f; // maximum plate angular speed
    public float OceanicElevationDamping = 4e-5f; // global oceanic elevation damping parameter
    public float ContinentalErosion = 3e-5f; // global continental erosion parameter
    public float SedimentAccretion = 3e-4f; // global sediment accretion parameter
    public float SubductionUplift = 6e-4f; // global subduction uplift parameter
    public float SlabPullPerturbation = 0.1f; // global rotation axis slab pull perturbation parameter

    public float NeighbourSmoothWeight = 0.1f; // weight for neighbour elevations for smoothing
    public float ContinentalCollisionGlobalDistance = 0.659f; // global terrane collision influence parameter
    public float ContinentalCollisionCoefficient = 82e-3f; // global collision elevation parameter

    public float PlateRiftsPerTectonicIterationStep = 0.01f; // global average rifting frequency of plates per tectonic step
    public int VectorNoiseAveragingIterations = 3; // number of iterations in averaging the vector noise
    public int VoronoiBorderNoiseIterations = 6; // number of iterations in plate border perturbations
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
