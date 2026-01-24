using Elevation.Models;

namespace Elevation.Services;

public interface IModelisation
{
  string CreateMesh(double[,] elevationGrid, MeshOptions options, MeshParameters parameters);

  Dictionary<int, double[,]> CreateLevel(double[,] heightmap, int step);
}