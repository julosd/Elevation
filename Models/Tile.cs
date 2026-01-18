namespace Elevation.Models;

public record Tile(int X, int Y, int Z)
{
  public Tile(double lat, double lon, int zoom = 14) : 
    this(
      (int)Math.Floor((lon + 180.0) / 360.0 * Math.Pow(2.0, zoom)),
      (int)Math.Floor((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom)),
      zoom
    )
  {
  }
}