namespace Elevation.Models;

public readonly struct MeshOptions(int topographyStep = 1, double exaggeration = 1.0, int step = 1)
{
  public int TopographyStep { get; init; } = topographyStep;
  public double Exaggeration { get; init; } = exaggeration;
  public int LateralStep { get; init; } = step;
}
