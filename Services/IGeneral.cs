using System.Drawing;
using Elevation.Models;

namespace Elevation.Interfaces.Services;

public interface IGeneral
{
  Tile GetTile(double lat, double lon, int zoom);
  Task<Bitmap> GetRaster(Tile tile);
  double GetElevation(Bitmap bitmap);
}