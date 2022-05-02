using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimulationSettings
{
    public int FractalTerrainIterations = 10000; // in bunches of 64
    public float FractalTerrainElevationStep = 0.003f; // minimal elevation in every fractal terrain generation iteration
    public float MarkupElevation = 0.1f; // uniform elevation for mark-ups
    public int PlateInitNumberOfCentroids = 7; // number of initial tectonic plates (Voronoi centers)

    public float NewCrustThickness = 0.01f; // initial crust thickness on generated points
    public float CrustElevationRandomThicknessRange = 0.01f; // how much the crust is varied in thickness across samples
    public float OceanBaseFloor = -0.004f; // base floor level when rendering without sample interpolation outside of tectonic plates
    public float OceanicRidgeElevationFalloff = 0.05f;
    public int BVHConstructionRadius = 20; // interval radius for BVH construction
    public float AverageOceanicDepth = -0.004f;
    public float InitialOceanicDepth = -0.004f;
    public float InitialContinentalAltitude = 0.001f;
    public float InitialContinentalProbability = 0.3f;

    public float TectonicIterationStepTime = 2; // simulation time-step in My, def: 2
    public float PlanetRadius = 6.37f; // multiple of 1000 km
    public float HighestOceanicRidgeElevation = -0.001f;
    public float OceanicTrenchElevation = -0.01f;
    public float AbyssalPlainsElevation = -0.006f;
    public float HighestContinentalAltitude = 0.01f;
    public float SubductionDistanceTransferControlDistance = 0.10f;
    public float SubductionDistanceTransferMaxDistance = 0.28f;
    public float CollisionDistance = 0.66f;
    public float CollisionCoefficient = 0.013f;
    public float MaximumPlateSpeed = 0.0157f;
    public float OceanicElevationDamping = 4e-5f;
    public float ContinentalErosion = 3e-5f;
    public float SedimentAccretion = 3e-4f;
    public float SubductionUplift = 6e-4f;
    public float SlabPullPerturbation = 0.1f;

    public float NeighbourSmoothWeight = 0.1f;
    public float ContinentalCollisionGlobalDistance = 0.659f;
    public float ContinentalCollisionCoefficient = 82e-3f;
}

[System.Serializable]
public class SimulationShaders
{
    public ComputeShader m_DefaultTerrainTextureCShader = null;
    public ComputeShader m_PlatesAreaTextureCShader = null;
    public ComputeShader m_FractalTerrainCShader = null;
    public ComputeShader m_TriangleCollisionTestCShader = null;
    public ComputeShader m_CircleMergeShader = null;
    public ComputeShader m_BVHNearestNeighbourShader = null;
    public ComputeShader m_VertexDataInterpolationShader = null;
    public ComputeShader m_PlateInteractionsShader = null;
    public ComputeShader m_DebugTextureShader = null;
    public ComputeShader m_OverlayTextureShader = null;
}
