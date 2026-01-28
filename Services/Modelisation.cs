using System.Globalization;
using System.Numerics;
using System.Text;
using Elevation.Models;

namespace Elevation.Services;

public sealed class Modelisation : IModelisation
{
  /// <summary>
  /// Crée un mesh OBJ à partir d'un raster représenté sous forme de grille 2D.
  /// </summary>
  /// <param name="heightMap">Grille 2D des altitudes (double).</param>
  /// <param name="options"></param>
  /// <param name="parameters"></param>
  /// <returns>Chaîne contenant le contenu du fichier OBJ.</returns>
  public string CreateMesh(double[,] heightMap, MeshOptions options, MeshParameters parameters)
  {
    var obj = new StringBuilder();

    var vertexIndex = 1;
    var vertexIndices = new int[parameters.Size.Width, parameters.Size.Height];

    // Boucle sur la grille avec filtrage
    for (var x = 0; x < parameters.Size.Width - options.LateralStep; x += options.LateralStep)
    {
      for (var y = 0; y < parameters.Size.Height - options.LateralStep; y += options.LateralStep)
      {
        // Conversion en coordonnées relatives centrées
        var xf = (x - parameters.Center.X);
        var yf = (y - parameters.Center.Y);
        var zf = (RoundElevation(heightMap[x, y], options.TopographyStep) - parameters.ElevationScale.Min) /
                 parameters.MetersPerPixel *
                 options.Exaggeration;

        obj.AppendLine(
          $"v {xf.ToString(CultureInfo.InvariantCulture)} " +
          $"{zf.ToString(CultureInfo.InvariantCulture)} " +
          $"{yf.ToString(CultureInfo.InvariantCulture)}");
        vertexIndices[x, y] = vertexIndex++;
      }
    }


    Console.WriteLine("topo step : " + options.TopographyStep);
    Console.WriteLine("elevation min " + parameters.ElevationScale.Min);
    Console.WriteLine("mpp : " + parameters.MetersPerPixel);
    Console.WriteLine("ex : " + options.Exaggeration);
    
    for (var x = 0; x < parameters.Size.Width - options.LateralStep; x += options.LateralStep)
    {
      for (var y = 0; y < parameters.Size.Height - options.LateralStep; y += options.LateralStep)
      {
        int v1 = vertexIndices[x, y];
        int v2 = vertexIndices[x + options.LateralStep, y];
        int v3 = vertexIndices[x, y + options.LateralStep];
        int v4 = vertexIndices[x + options.LateralStep, y + options.LateralStep];

        // Triangle 1
        obj.AppendLine($"f {v1} {v2} {v3}");

        // Triangle 2
        obj.AppendLine($"f {v2} {v4} {v3}");
      }
    }
    

    // Sauvegarde du fichier
    File.WriteAllText("mesh.obj", obj.ToString());

    return obj.ToString();
  }


  public string CreateMesh2(Voxel[,,] voxels, MeshOptions options, MeshParameters parameters)
  {
    Console.WriteLine("Création du mesh.");
    var obj = new StringBuilder();
    //var vertexIndex = 1;
    //var vertexIndices = new int[parameters.Size.Width, parameters.Size.Height];
    var index = 1;

    for (var z = (int)parameters.ElevationScale.Min; z < parameters.ElevationScale.Max; z += options.TopographyStep)
    {
      for (var x = 0; x < parameters.Size.Width - options.LateralStep; x += options.LateralStep)
      {
        for (var y = 0; y < parameters.Size.Height - options.LateralStep; y += options.LateralStep)
        {
          if (voxels[x, y, z] is Voxel.Air) continue;
          if (voxels[x, y, z] is Voxel.Void) continue;

          var elevation = ComputeElevation(z, options, parameters);


          var xf = (x - parameters.Center.X);
          var yf = (y - parameters.Center.Y);

          //obj.AppendLine(
          //  $"v {xf.ToString(CultureInfo.InvariantCulture)} " +
          //  $"{elevation.ToString(CultureInfo.InvariantCulture)} " +
          //  $"{yf.ToString(CultureInfo.InvariantCulture)}");

          //if (x % 2 == 0 && y % 2 == 0) CreateVoxel((xf, yf, elevation), obj, ref index);
          CreateCube((xf, yf, elevation), voxels, obj, ref index);
        }
      }
    }

    File.WriteAllText("mesh.obj", obj.ToString());

    return obj.ToString();
  }

  private static void CreateCube((int x, int y, int z) center, Voxel[,,] voxels, StringBuilder obj, ref int index)
  {
    const float halfSize = .5f;

    var offsets = new[]
    {
      new Vector3(-halfSize, -halfSize, -halfSize), // BLB  Bottom Left Back
      new Vector3(halfSize, -halfSize, -halfSize), // BRB  Bottom Right Back
      new Vector3(-halfSize, halfSize, -halfSize), // TLB  Top Left Back
      new Vector3(halfSize, halfSize, -halfSize), // TRB  Top Right Back
      new Vector3(-halfSize, -halfSize, halfSize), // BLF  Bottom Left Front
      new Vector3(halfSize, -halfSize, halfSize), // BRF  Bottom Right Front
      new Vector3(-halfSize, halfSize, halfSize), // TLF  Top Left Front
      new Vector3(halfSize, halfSize, halfSize) // TRF  Top Right Front
    };


    // tableau pour stocker les indices des 8 sommets
    var vertexIndices = new int[8];

    for (var i = 0; i < 8; i++)
    {
      var px = center.x + offsets[i].X;
      var py = center.y + offsets[i].Y;
      var pz = center.z + offsets[i].Z;

      obj.AppendLine(
        $"v {px.ToString(CultureInfo.InvariantCulture)} " +
        $"{pz.ToString(CultureInfo.InvariantCulture)} " +
        $"{py.ToString(CultureInfo.InvariantCulture)}");

      vertexIndices[i] = index;
      index++; // incrémente l’index global pour le prochain sommet
    }
    
    //if (center.z == 0) obj.AppendLine($"f {vertexIndices[0]} {vertexIndices[1]} {vertexIndices[4]} {vertexIndices[5]}"); 

    /*
    obj.AppendLine($"f {vertexIndices[4]} {vertexIndices[5]} {vertexIndices[7]} {vertexIndices[6]}"); // Front
    obj.AppendLine($"f {vertexIndices[0]} {vertexIndices[1]} {vertexIndices[3]} {vertexIndices[2]}"); // Back
    obj.AppendLine($"f {vertexIndices[2]} {vertexIndices[3]} {vertexIndices[7]} {vertexIndices[6]}"); // Top
    obj.AppendLine($"f {vertexIndices[0]} {vertexIndices[1]} {vertexIndices[5]} {vertexIndices[4]}"); // Bottom
    obj.AppendLine($"f {vertexIndices[0]} {vertexIndices[2]} {vertexIndices[6]} {vertexIndices[4]}"); // Left
    obj.AppendLine($"f {vertexIndices[1]} {vertexIndices[3]} {vertexIndices[7]} {vertexIndices[5]}"); // Right
    */
  }


  // Triangle 1
  //obj.AppendLine($"f {v1} {v2} {v3}");

  // Triangle 2
  //obj.AppendLine($"f {v2} {v4} {v3}");


  public Voxel[,,] CreateVoxels(double[,] heightmap, int step)
  {
    Console.WriteLine("Conversion height map -> voxels.");
    var width = heightmap.GetLength(0);
    var height = heightmap.GetLength(1);
    var scale = GetElevationsScale(heightmap, width, height, step);

    var results = new Voxel[width, height, (int)scale.Max];

    for (var z = (int)scale.Min; z < scale.Max; z += step)
    {
      for (var x = 0; x < width; x++)
      {
        for (var y = 0; y < height; y++)
        {
          var elevation = (int)RoundElevation(heightmap[x, y], step);

          if (elevation == z) results[x, y, z] = Voxel.Ground;
          else if (elevation < z) results[x, y, z] = Voxel.Air;
          else results[x, y, z] = Voxel.Underground;
        }
      }
    }


    results = Extrusion(results, step, scale);

    return results;
  }


  public Voxel[,,] Extrusion(Voxel[,,] points, int step, (double Min, double Max) scale)
  {
    Console.WriteLine("Extrusion.");
    var width = points.GetLength(0);
    var height = points.GetLength(1);
    //var scale = GetElevationsScale(points, width, height, step);

    for (var x = 1; x < width - 2; x++)
    {
      for (var y = 1; y < height - 2; y++)
      {
        for (var z = (int)scale.Min + step; z < scale.Max - 2 * step; z += step)
        {
          var c = (X: x, Y: y, Z: z + 2 * step);
          var n = (X: x, Y: y + 1, Z: z + step);
          var e = (X: x + 1, Y: y, Z: z + step);
          var s = (X: x, Y: y - 1, Z: z + step);
          var o = (X: x - 1, Y: y, Z: z + step);

          // La foreuse transforme les Voxels Underground en Void jusqu'à ce qu'elle détecte de l'air.
          points[x, y, z] = Voxel.Void;

          if (
            points[c.X, c.Y, c.Z] == Voxel.Air ||
            points[n.X, n.Y, n.Z] == Voxel.Air ||
            points[e.X, e.Y, e.Z] == Voxel.Air ||
            points[s.X, s.Y, s.Z] == Voxel.Air ||
            points[o.X, o.Y, o.Z] == Voxel.Air) break;
        }
      }
    }

    return points;
  }


  /// <summary>
  /// Arrondit une valeur à un multiple donné.
  /// </summary>
  /// <param name="value"></param>
  /// <param name="step"></param>
  /// <returns></returns>
  private static double RoundElevation(double value, double step = 25.0) => Math.Round(value / step) * step;


  /// <summary>
  /// Retourne les bornes minamale et maximal d'une height map
  /// </summary>
  /// <param name="heightMap"></param>
  /// <param name="width"></param>
  /// <param name="height"></param>
  /// <param name="step"></param>
  /// <returns></returns>
  private static (double Min, double Max) GetElevationsScale(double[,] heightMap, int width, int height, int step)
  {
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


  private static int ComputeElevation(int z, MeshOptions options, MeshParameters parameters)
  {
    var roundedLevel = RoundElevation(z, options.TopographyStep);

    var elevation = (roundedLevel - parameters.ElevationScale.Min)
                    / parameters.MetersPerPixel
                    * options.Exaggeration;

    return (int)Math.Round(elevation);
  }
}