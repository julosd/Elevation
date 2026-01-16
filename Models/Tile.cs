namespace Elevation.Models;

public class Tile(double lon, double lat, int zoom)
{
  public int X { get; init; } = (int)Math.Floor((lon + 180.0) / 360.0 * Math.Pow(2.0, zoom));
  public int Y { get; init; } = 
    (int)Math.Floor(
      (1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom)
    );
  public int Z { get; init; } = zoom; // 18 = 32x32px
}