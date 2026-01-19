

using SixLabors.ImageSharp.PixelFormats;

namespace Elevation.Models;

public class Pixel(int x, int y, Rgba32 value)
{
  public int X { get; init; } = x;
  public int Y { get; init; } = y;
  public Rgba32 Color { get; init; } = value;
}