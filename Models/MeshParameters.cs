namespace Elevation.Models;

public readonly struct MeshParameters
{
  public double MetersPerPixel { get; }
  public (double Min, double Max) ElevationScale { get; }
  public (int Width, int Height) Size { get; }
  public (int X, int Y) Center { get; }

  public MeshParameters(
    double latitude,
    int zoom,
    double[,] heightMap,
    int topographyStep)
  {
    Size = GetSize(heightMap);
    Center = (Size.Width / 2, Size.Height / 2);
    MetersPerPixel = GetTileSize(latitude, zoom) / Size.Width;
    ElevationScale = GetElevationsScale(heightMap, topographyStep);
  }

  // -----------------------
  // Helpers
  // -----------------------

  private static (int Width, int Height) GetSize(double[,] heightMap)
    => (heightMap.GetLength(0), heightMap.GetLength(1));

  private static double DegreesToRadians(double deg)
    => deg * Math.PI / 180.0;

  private static double GetTileSize(double latitude, int zoom)
    => 40075016.686 * Math.Cos(DegreesToRadians(latitude)) / Math.Pow(2, zoom);

  private static (double Min, double Max) GetElevationsScale(double[,] heightMap, int step)
  {
    var width = heightMap.GetLength(0);
    var height = heightMap.GetLength(1);

    var min = double.MaxValue;
    var max = double.MinValue;

    for (var x = 0; x < width; x++)
    {
      for (var y = 0; y < height; y++)
      {
        var z = heightMap[x, y];

        var zMinStep = Math.Floor(z / step) * step;
        if (zMinStep < min) min = zMinStep;

        var zMaxStep = Math.Ceiling(z / step) * step;
        if (zMaxStep > max) max = zMaxStep;
      }
    }

    return (min, max);
  }
}