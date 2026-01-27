namespace Elevation.Models;

public static class Faces
{
  public static readonly (int x, int y, int z)[] Values =
  [
    ( 0, -1,  0), // Front / -Y
    ( 0,  1,  0), // Back  / +Y
    (-1,  0,  0), // Left  / -X
    ( 1,  0,  0), // Right / +X
    ( 0,  0,  1), // Top   / +Z
    ( 0,  0, -1) // Bottom/ -Z
  ];
}