using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class APR
{
    public static int FractalTerrainIterations = 10000; // in bunches of 64
    public static float FractalTerrainElevationStep = 0.003f; // minimal elevation in every fractal terrain generation iteration
    public static float MarkupElevation = 0.1f; // uniform elevation for mark-ups
    public static float PlateInitElevationRange = 0.1f; // maximum interval of uniform elevation for new plates
    public static int PlateInitNumberOfCentroids = 20; // number of initial tectonic plates (Voronoi centers)
    public static float PlateInitElevation = -0.05f; // 
    public static float MaxPlateAngularSpeed = 0.03f; // per million years;
    public static float TectonicIterationStepTime = 5; // simulation time-step in My
}
