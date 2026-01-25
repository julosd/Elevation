using Elevation.Models;

namespace Elevation.Services;

public interface IModelisation
{
  string CreateMesh(double[,] elevationGrid, MeshOptions options, MeshParameters parameters);
  string CreateMesh2((bool HasPoint, bool HasPointAbove)[,,] elevationGrid, MeshOptions options,
    MeshParameters parameters);
  

  (bool HasPoint, bool HasPointAbove)[,,] CreateLevel(double[,] heightmap, int step);

  (bool HasPoint, bool HasPointAbove)[,,] Extrusion((bool HasPoint, bool HasPointAbove)[,,] points, int step,
    (double Min, double Max) scale);
}