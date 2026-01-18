using System.Drawing;

namespace Elevation.Models;

public class Pixel(int x, int y, Color value)
{
  public int X { get; init; } = x;
  public int Y { get; init; } = y;
  public Color Color { get; init; } = value;
}