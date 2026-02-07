using Elevation.Models;

namespace Elevation.Services;

public interface IModelisation
{
  string CreateMesh(double[,] elevationGrid, MeshOptions options, MeshParameters parameters);
  string CreateMinecraftMesh(Voxel[,,] elevationGrid, MeshOptions options, MeshParameters parameters);
  Voxel[,,] CreateVoxels(double[,] heightmap, int step);
  Voxel[,,] Extrusion(Voxel[,,] points, int step, (double Min, double Max) scale);
}