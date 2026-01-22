using Elevation.Models;

namespace Elevation.Services;

public interface IModelisation
{
  string CreateMesh(List<Coordinates> coordinates);
  Dictionary<double, List<Coordinates>> CreateLevel(List<Coordinates> coordinates);
  void CreateTerrain(Dictionary<double, List<Coordinates>> levels);
}