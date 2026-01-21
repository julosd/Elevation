using Elevation.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Elevation.Interfaces.Services;

public interface IGeneral
{
  Tile GetTile(double lat, double lon, int zoom);
  Task<Image<Rgba32>> GetRaster(Tile tile);
  double GetElevation(Image<Rgba32> raster);
  double GetElevation(Rgba32 c);
  Pixel GetPixel(Image<Rgba32> raster, double latitude, double longitude, int zoom);
  Task<List<Coordinates>> GetElevation(List<Coordinates> coordinatesList);
  List<Coordinates> GetAllElevationInRaster(Image<Rgba32> raster);
  string CreateMesh(List<Coordinates> coordinates);
}