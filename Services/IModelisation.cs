using Elevation.Models;

namespace Elevation.Services;

public interface IModelisation
{
  string CreateMesh(double[,] elevationGrid, double tileSize, int step = 1);
  Dictionary<double, List<Coordinates>> CreateLevel(List<Coordinates> coordinates);
  void CreateTerrain(Dictionary<double, List<Coordinates>> levels);
  Dictionary<(int X, int Y), int> IndexElevationByXY(IEnumerable<Coordinates> coordinates);
}