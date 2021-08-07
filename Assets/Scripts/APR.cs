﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class APR
{
    public static int FractalTerrainIterations = 10000; // in bunches of 64
    public static float FractalTerrainElevationStep = 0.003f; // minimal elevation in every fractal terrain generation iteration
    public static float MarkupElevation = 0.1f; // uniform elevation for mark-ups
    public static float PlateInitElevationRange = 0.1f; // maximum interval of uniform elevation for new plates
    public static int PlateInitNumberOfCentroids = 20; // number of initial tectonic plates (Voronoi centers)
    public static float PlateInitLandRatio = 0.33f; // land to sea ratio
    public static float MaxPlateAngularSpeed = 0.03f; // per million years;
    public static float TectonicIterationStepTime = 5; // simulation time-step in My
    public static float CrustThicknessMin = 0.04f; // minimum random crust thickness parameter
    public static float CrustThicknessMax = 0.06f; // maximum random crust thickness parameter
    public static float CrustElevationRandomThicknessRange = 0.01f; // how much the crust is varied in thickness across samples
    public static float OceanBaseFloor = -0.5f; // base floor level when rendering without sample interpolation outside of tectonic plates
    public static int BVHConstructionRadius = 20; // interval radius for BVH construction
    public static float AverageContinentalElevation = 0.1f; // landmass initial elevation
    public static float AverageOceanicDepth = -0.4f;
    public static float HighestOceanicRidgeElevation = -0.1f;
    public static float AbyssalPlainsElevation = -0.6f;
    public static float OceanicRidgeElevationFalloff = 0.05f;
    public static float SubductionDistance = 0.29f; // max distance that causes subduction
}
