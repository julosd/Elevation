using Elevation.Models;

namespace Elevation.Services;

public interface IModelisation
{
  string CreateMesh(List<Coordinates> coordinates);
}