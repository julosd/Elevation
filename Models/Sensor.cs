namespace Elevation.Models;

public struct Sensor
{
  public bool IsExposed(Voxel[,,] matrix, (int x, int y, int z) _)
  {
    var xMax = matrix.GetLength(0);
    var yMax = matrix.GetLength(1);
    var zMax = matrix.GetLength(2);

    if (_.x == 0) return true;

    if (matrix[_.x - 1, _.y, _.z] == Voxel.Air) return true;

    return false;
  }

  public static bool IsExposed(int face, Voxel[,,] matrix, (int x, int y, int z) c)
  {
    var (dx, dy, dz) = Faces.Values[face];

    var xTarget = c.x + dx;
    var yTarget = c.y + dy;
    var zTarget = c.z + dz;

    // extérieur de la grille = exposé
    if (xTarget < 0 || yTarget < 0 || zTarget < 0 ||
        xTarget >= matrix.GetLength(0) ||
        yTarget >= matrix.GetLength(1) ||
        zTarget >= matrix.GetLength(2))
      return true;

    // exposé si le voxel voisin est de l'air
    return matrix[xTarget, yTarget, zTarget] == Voxel.Air;
  }

}