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
        var v1 = vertexIndices[x, y];
        var v2 = vertexIndices[x + options.LateralStep, y];
        var v3 = vertexIndices[x, y + options.LateralStep];
        var v4 = vertexIndices[x + options.LateralStep, y + options.LateralStep];

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


  public string CreateMinecraftMesh(Voxel[,,] voxels, MeshOptions options, MeshParameters parameters)
  {
    Console.WriteLine("Création du mesh.");
    var obj = new StringBuilder();
    //var vertexIndex = 1;
    //var vertexIndices = new int[parameters.Size.Width, parameters.Size.Height];
    var xMax = parameters.Size.Width;
    var yMax = parameters.Size.Height;
    var lateralStep = options.LateralStep;
    var zMin = (int)parameters.ElevationScale.Min;
    var zMax = (int)parameters.ElevationScale.Max;


    obj.AppendLine($"v -0.5 0 {xMax}");
    obj.AppendLine($"v {xMax + .5} 0 {xMax + .5}");
    obj.AppendLine($"v -0.5 0 -0.5");
    obj.AppendLine($"v {xMax + .5} 0 -0.5");
    //obj.AppendLine("f 1 2 4 3");

    var index = 5;

    var test = true;

    for (var x = 0; x < xMax - lateralStep; x += lateralStep)
    {
      for (var y = 0; y < yMax - lateralStep; y += lateralStep)
      {
        for (var z = zMin; z < zMax; z += options.TopographyStep)
        {
          if (voxels[x, y, z] is Voxel.Void) continue;
          if (voxels[x, y, z] is Voxel.Air)
          {
            // il faudrait ici marquer les deux points les plus haut de la colonne.
            // il n'y a peut etre pas besoin de créer un cube sur les faces latérales
            break;
          }

          var elevation = ComputeElevation(z, options, parameters);

          var xf = (x - parameters.Center.X);
          var yf = (y - parameters.Center.Y);

          if (test) CreateCube((x, y, elevation), voxels, obj, ref index, zMin);
          //test = false;
        }
      }
    }


    File.WriteAllText("mesh.obj", obj.ToString());

    return obj.ToString();
  }


  private static void CreateCube((int x, int y, int z) p, Voxel[,,] voxels, StringBuilder obj, ref int index, int zMin)
  {
    CreateCubeVertex(obj, p, ref index);

    var v1 = index - 8;
    var v2 = index - 7;
    var v3 = index - 6;
    var v4 = index - 5;
    var v5 = index - 4;
    var v6 = index - 3;
    var v7 = index - 2;
    var v8 = index - 1;

    /*
        obj.AppendLine($"f {v5} {v6} {v8} {v7}"); //top
        obj.AppendLine($"f {v1} {v2} {v4} {v3}"); //bottom
        obj.AppendLine($"f {v1} {v3} {v7} {v5}"); //left
        obj.AppendLine($"f {v2} {v4} {v8} {v6}"); //right
        obj.AppendLine($"f {v3} {v4} {v8} {v7}"); //front
        obj.AppendLine($"f {v1} {v2} {v6} {v5}"); //back
        */
  }


  /// <summary>
  /// Creer les sommets pour les cubes.
  /// Les points <b>left</b> ne sont créés que pour la ligne x égale à zéro (donc dans la 1ere profondeur)
  /// Les points <b>front</b> ne sont créés que pour la ligne y égale à zéro
  /// </summary>
  /// <param name="obj"></param>
  /// <param name="p"></param>
  /// <param name="i"></param>
  private static void CreateCubeVertex(StringBuilder obj, (int x, int y, int z) p, ref int i)
  {
    const float half = .5f;

    if (p is { x: 0, y: 0 })
    {
      obj.AppendLine(
        $"v {(p.x - half).ToString(CultureInfo.InvariantCulture)} " + // left
        $"{(p.z - half).ToString(CultureInfo.InvariantCulture)} " + // bottom
        $"{(p.y - half).ToString(CultureInfo.InvariantCulture)}"); // front
      i++;
    }

    if (p.x == 0)
    {
      obj.AppendLine(
        $"v {(p.x + half).ToString(CultureInfo.InvariantCulture)} " + // right
        $"{(p.z - half).ToString(CultureInfo.InvariantCulture)} " + // bottom
        $"{(p.y - half).ToString(CultureInfo.InvariantCulture)}"); // front
      i++;
    }

    if (p.y == 0)
    {
      obj.AppendLine(
        $"v {(p.x - half).ToString(CultureInfo.InvariantCulture)} " + // left
        $"{(p.z - half).ToString(CultureInfo.InvariantCulture)} " + // bottom
        $"{(p.y + half).ToString(CultureInfo.InvariantCulture)}"); // back
      i++;
    }

    obj.AppendLine(
      $"v {(p.x + half).ToString(CultureInfo.InvariantCulture)} " + // right
      $"{(p.z - half).ToString(CultureInfo.InvariantCulture)} " + // bottom
      $"{(p.y + half).ToString(CultureInfo.InvariantCulture)}"); // back
    i++;

    if (p is { x: 0, y: 0 })
    {
      obj.AppendLine(
        $"v {(p.x - half).ToString(CultureInfo.InvariantCulture)} " + // left
        $"{(p.z + half).ToString(CultureInfo.InvariantCulture)} " + // top
        $"{(p.y - half).ToString(CultureInfo.InvariantCulture)}"); // front
      i++;
    }

    if (p.x == 0)
    {
      obj.AppendLine(
        $"v {(p.x + half).ToString(CultureInfo.InvariantCulture)} " + // right
        $"{(p.z + half).ToString(CultureInfo.InvariantCulture)} " + // top
        $"{(p.y - half).ToString(CultureInfo.InvariantCulture)}"); // front
      i++;
    }

    if (p is { x: 0, y: 0 })
    {
      obj.AppendLine(
        $"v {(p.x - half).ToString(CultureInfo.InvariantCulture)} " + // left
        $"{(p.z + half).ToString(CultureInfo.InvariantCulture)} " + // top
        $"{(p.y + half).ToString(CultureInfo.InvariantCulture)}"); // back
      i++;
    }

    obj.AppendLine(
      $"v {(p.x + half).ToString(CultureInfo.InvariantCulture)} " + // right
      $"{(p.z + half).ToString(CultureInfo.InvariantCulture)} " + // top
      $"{(p.y + half).ToString(CultureInfo.InvariantCulture)}"); // back
    i++;
  }


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
  /// Retourne les bornes minimale et maximale d'une height map
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
    var elevation = (roundedLevel - parameters.ElevationScale.Min) / parameters.MetersPerPixel * options.Exaggeration;

    return (int)Math.Round(elevation);
  }
}