using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
public static class APR
{
    public static int FractalTerrainIterations = 10000; // in bunches of 64
    public static float FractalTerrainElevationStep = 0.003f; // minimal elevation in every fractal terrain generation iteration
    public static float MarkupElevation = 0.1f; // uniform elevation for mark-ups
    public static float PlateInitElevationRange = 0.1f; // maximum interval of uniform elevation for new plates
    public static int PlateInitNumberOfCentroids = 7; // number of initial tectonic plates (Voronoi centers)
    public static float PlateInitLandRatio = 0.0f;// 0.33f; // land to sea ratio
    //public static float MaxPlateAngularSpeed = 0.03f; // per million years;

    public static float CrustThicknessMin = 0.04f; // minimum random crust thickness parameter
    public static float CrustThicknessMax = 0.06f; // maximum random crust thickness parameter
    public static float CrustElevationRandomThicknessRange = 0.01f; // how much the crust is varied in thickness across samples
    public static float OceanBaseFloor = -0.004f; // base floor level when rendering without sample interpolation outside of tectonic plates
    public static float OceanicRidgeElevationFalloff = 0.05f;
    public static int BVHConstructionRadius = 20; // interval radius for BVH construction
    public static float AverageContinentalElevation = 0.001f; // landmass initial elevation
    public static float AverageOceanicDepth = -0.004f;
    //public static float SubductionDistance = 0.29f; // max distance that causes subduction
    public static int MaxBorderTrianglesCount = 1500;

    public static float TectonicIterationStepTime = 2; // simulation time-step in My, def: 2
    public static float PlanetRadius = 6.37f; // multiple of 1000 km
    public static float HighestOceanicRidgeElevation = -0.001f;
    public static float OceanicTrenchElevation = -0.01f;
    public static float AbyssalPlainsElevation = -0.006f;
    public static float HighestContinentalAltitude = 0.01f;
    public static float SubductionDistanceTransferControlDistance = 0.10f;
    public static float SubductionDistanceTransferMaxDistance = 0.28f;
    public static float CollisionDistance = 0.66f;
    public static float CollisionCoefficient = 0.013f;
    public static float MaximumPlateSpeed = 0.0157f;
    public static float OceanicElevationDamping = 4e-5f;
    public static float ContinentalErosion = 3e-5f;
    public static float SedimentAccretion = 3e-4f;
    public static float SubductionUplift = 6e-4f;
    public static float SlabPullPerturbation = 0.1f;


    //public static float SubductionSpeedTransferParameter = 0.21f;
}
*/
[System.Serializable]
public class SimulationSettings
{
    public int FractalTerrainIterations = 10000; // in bunches of 64
    public float FractalTerrainElevationStep = 0.003f; // minimal elevation in every fractal terrain generation iteration
    public float MarkupElevation = 0.1f; // uniform elevation for mark-ups
    public float PlateInitElevationRange = 0.1f; // maximum interval of uniform elevation for new plates
    public int PlateInitNumberOfCentroids = 7; // number of initial tectonic plates (Voronoi centers)
    public float PlateInitLandRatio = 0.0f;// 0.33f; // land to sea ratio
    //public static float MaxPlateAngularSpeed = 0.03f; // per million years;

    public float CrustThicknessMin = 0.04f; // minimum random crust thickness parameter
    public float CrustThicknessMax = 0.06f; // maximum random crust thickness parameter
    public float CrustElevationRandomThicknessRange = 0.01f; // how much the crust is varied in thickness across samples
    public float OceanBaseFloor = -0.004f; // base floor level when rendering without sample interpolation outside of tectonic plates
    public float OceanicRidgeElevationFalloff = 0.05f;
    public int BVHConstructionRadius = 20; // interval radius for BVH construction
    public float AverageContinentalElevation = 0.001f; // landmass initial elevation
    public float AverageOceanicDepth = -0.004f;
    public float InitialOceanicDepth = -0.004f;
    //public static float SubductionDistance = 0.29f; // max distance that causes subduction
    public int MaxBorderTrianglesCount = 1500;

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
    public ComputeShader m_TerrainesConstructShader = null;
    public ComputeShader m_PlateInteractionsShader = null;
    public ComputeShader m_DebugTextureShader = null;
}
