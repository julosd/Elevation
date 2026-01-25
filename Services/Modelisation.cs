using System.Globalization;
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
    /*
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
    */

    // Sauvegarde du fichier
    File.WriteAllText("mesh.obj", obj.ToString());

    return obj.ToString();
  }


  public string CreateMesh2((bool HasPoint, bool HasPointAbove)[,,] levels, MeshOptions options,
    MeshParameters parameters)
  {
    Console.WriteLine("Création du mesh.");
    var obj = new StringBuilder();
    var vertexIndex = 1;
    var vertexIndices = new int[parameters.Size.Width, parameters.Size.Height];

    for (var z = (int)parameters.ElevationScale.Min; z < parameters.ElevationScale.Max; z += options.TopographyStep)
    {
      for (var x = 0; x < parameters.Size.Width - options.LateralStep; x += options.LateralStep)
      {
        for (var y = 0; y < parameters.Size.Height - options.LateralStep; y += options.LateralStep)
        {
          if (levels[x, y, z] is (false, false)) continue;

          var elevation = ComputeElevation(z, options, parameters);


          var xf = (x - parameters.Center.X);
          var yf = (y - parameters.Center.Y);

          obj.AppendLine(
            $"v {xf.ToString(CultureInfo.InvariantCulture)} " +
            $"{elevation.ToString(CultureInfo.InvariantCulture)} " +
            $"{yf.ToString(CultureInfo.InvariantCulture)}");
          vertexIndices[x, y] = vertexIndex++;
        }
      }
    }

    for (var x = 0; x < parameters.Size.Width - options.LateralStep; x += options.LateralStep)
    {
      for (var y = 0; y < parameters.Size.Height - options.LateralStep; y += options.LateralStep)
      {
        int v1 = vertexIndices[x, y];
        int v2 = vertexIndices[x + options.LateralStep, y];
        int v3 = vertexIndices[x, y + options.LateralStep];
        int v4 = vertexIndices[x + options.LateralStep, y + options.LateralStep];

        // Triangle 1
        //obj.AppendLine($"f {v1} {v2} {v3}");

        // Triangle 2
        //obj.AppendLine($"f {v2} {v4} {v3}");
      }
    }

    File.WriteAllText("mesh.obj", obj.ToString());

    return obj.ToString();
  }


  public (bool HasPoint, bool HasPointAbove)[,,] CreateLevel(double[,] heightmap, int step)
  {
    Console.WriteLine("Création des palliers.");
    var width = heightmap.GetLength(0);
    var height = heightmap.GetLength(1);
    var scale = GetElevationsScale(heightmap, width, height, step);

    var results = new (bool HasPoint, bool HasPointAbove)[width, height, (int)scale.Max];

    for (var z = (int)scale.Min; z < scale.Max; z += step)
    {
      for (var x = 0; x < width; x++)
      {
        for (var y = 0; y < height; y++)
        {
          var elevation = (int)RoundElevation(heightmap[x, y], step);

          if (elevation == z) results[x, y, z] = (true, false);
          else if (elevation < z) results[x, y, z] = (false, false);
          else results[x, y, z] = (false, true);
        }
      }
    }


    results = Extrusion(results, step, scale);

    return results;
  }


  public (bool HasPoint, bool HasPointAbove)[,,] Extrusion((bool HasPoint, bool HasPointAbove)[,,] points, int step,
    (double Min, double Max) scale)
  {
    Console.WriteLine("Extrusion.");
    var width = points.GetLength(0);
    var height = points.GetLength(1);
    //var scale = GetElevationsScale(points, width, height, step);

    for (var x = 1; x < width - 2; x++)
    {
      for (var y = 1; y < height - 2; y++)
      {
        for (var z = (int)scale.Min; z < scale.Max; z += step)
        {
          if (z == (int)scale.Min) continue;
          var n = (X: x, Y: y + 1, Z: z + 1);
          var ne = (X: x + 1, Y: y + 1, Z: z + 1);
          var e = (X: x + 1, Y: y, Z: z + 1);
          var se = (X: x + 1, Y: y - 1, Z: z + 1);
          var s = (X: x, Y: y - 1, Z: z + 1);
          var so = (X: x - 1, Y: y - 1, Z: z + 1);
          var o = (X: x - 1, Y: y, Z: z + 1);
          var no = (X: x - 1, Y: y + 1, Z: z + 1);


          if (
            points[x, y, z] is (true, false) ||
            points[x, y, z + 1] is (true, false) ||
            points[n.X, n.Y, n.Z] is (true, false) ||
            points[ne.X, ne.Y, ne.Z] is (true, false) ||
            points[e.X, e.Y, e.Z] is (true, false) ||
            points[se.X, se.Y, se.Z] is (true, false) ||
            points[s.X, s.Y, s.Z] is (true, false) ||
            points[so.X, so.Y, so.Z] is (true, false) ||
            points[o.X, o.Y, o.Z] is (true, false) ||
            points[no.X, no.Y, no.Z] is (true, false) //
          )
          {
            Console.WriteLine($"Break à : {x}, {y}, {z}");
            break;
          }

          points[x, y, z] = (false, false);
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
  public static (double Min, double Max) GetElevationsScale(double[,] heightMap, int width, int height, int step)
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


  private static int ComputeElevation(
    int z,
    MeshOptions options,
    MeshParameters parameters)
  {
    // Applique l’arrondi selon TopographyStep
    var roundedLevel = RoundElevation(z, options.TopographyStep);

    // Normalisation et conversion en unités de mesh
    var elevation = (roundedLevel - parameters.ElevationScale.Min)
                    / parameters.MetersPerPixel
                    * options.Exaggeration;

    return (int)Math.Round(elevation);
  }
}


/*
/// </summary>
/// <param name="elevationGrid">Grille 2D des altitudes (double).</param>
/// <param name="step">Pas de filtrage pour le mesh (ex: 8 pour prendre un point sur 8).</param>
/// <param name="tileSize"></param>
/// <param name="exageration"></param>
/// <returns>Chaîne contenant le contenu du fichier OBJ.</returns>
public string CreateMesh(double[,] elevationGrid, double tileSize, double exageration = 1.0, int step = 1)
{
var obj = new StringBuilder();

var width = elevationGrid.GetLength(0);
var height = elevationGrid.GetLength(1);

// Calcul du minimum et maximum d'altitude pour normalisation
var zMin = double.MaxValue;
var zMax = double.MinValue;

for (var x = 0; x < width; x++)
{
for (var y = 0; y < height; y++)
{
var z = elevationGrid[x, y];
if (z < zMin) zMin = z;
if (z > zMax) zMax = z;
}
}

// Centre de la grille pour centrer le mesh
var x0 = width / 2.0;
var y0 = height / 2.0;

// Conversion en mètres pour chaque “pixel” de la grille
var metersPerPixel = tileSize / width;

// Échelle de la hauteur : on veut que la différence zMax-zMin corresponde
// à la même proportion que la taille horizontale d'un pixel
var heightScale = (zMax - zMin) / metersPerPixel; // ratio réel
if (heightScale == 0) heightScale = 1.0; // éviter division par zéro

Console.WriteLine($"Tile size (m)           : {tileSize}");
Console.WriteLine($"Altitude max (m)        : {zMax}");
Console.WriteLine($"Altitude range (m)      : {zMax - zMin}");
Console.WriteLine($"Height scale (unit/m)   : {heightScale}");
Console.WriteLine($"Meters per pixel (m/px) : {metersPerPixel}");

var previousElevation = 0.0;
// Boucle sur la grille avec filtrage
for (var x = 0; x < width; x += step)
{
for (var y = 0; y < height; y += step)
{
var currentElevation = elevationGrid[x, y];



previousElevation = currentElevation;

// Conversion en coordonnées relatives centrées
var xf = (x - x0);
var yf = (y - y0);
var zf = (currentElevation - zMin) / metersPerPixel * exageration;

obj.AppendLine($"v {xf.ToString(CultureInfo.InvariantCulture)} {zf.ToString(CultureInfo.InvariantCulture)} {yf.ToString(CultureInfo.InvariantCulture)}");
}
}







for (int y = 0; y < height - 1; y++)
{
for (int x = 0; x < width - 1; x++)
{
int i = y * width + x + 1; // +1 car OBJ est 1-based

int iRight = i + 1;
int iDown = i + width;
int iDownRight = iDown + 1;

// Triangle 1
obj.AppendLine($"f {i} {iDown} {iRight}");

// Triangle 2
obj.AppendLine($"f {iRight} {iDown} {iDownRight}");
}
}

File.WriteAllText("terrain.obj", obj.ToString());

// Sauvegarde du fichier
File.WriteAllText("mesh.obj", obj.ToString());

return obj.ToString();
}




















AVEC PALLIER
/// <summary>
  /// Crée un mesh OBJ à partir d'un raster représenté sous forme de grille 2D.
  /// </summary>
  /// <param name="elevationGrid">Grille 2D des altitudes (double).</param>
  /// <param name="step">Pas de filtrage pour le mesh (ex: 8 pour prendre un point sur 8).</param>
  /// <param name="tileSize"></param>
  /// <param name="exageration"></param>
  /// <returns>Chaîne contenant le contenu du fichier OBJ.</returns>
  public string CreateMesh(double[,] elevationGrid, double tileSize, double exageration = 1.0, int step = 1)
  {
    var obj = new StringBuilder();

    var width = elevationGrid.GetLength(0);
    var height = elevationGrid.GetLength(1);

    // Calcul du minimum et maximum d'altitude pour normalisation
    var zMin = double.MaxValue;
    var zMax = double.MinValue;

    for (var x = 0; x < width; x++)
    {
      for (var y = 0; y < height; y++)
      {
        var z = elevationGrid[x, y];
        if (z < zMin) zMin = z;
        if (z > zMax) zMax = z;
      }
    }

    // Centre de la grille pour centrer le mesh
    var x0 = width / 2.0;
    var y0 = height / 2.0;

    // Conversion en mètres pour chaque “pixel” de la grille
    var metersPerPixel = tileSize / width;

    // Échelle de la hauteur : on veut que la différence zMax-zMin corresponde
    // à la même proportion que la taille horizontale d'un pixel
    var heightScale = (zMax - zMin) / metersPerPixel; // ratio réel
    if (heightScale == 0) heightScale = 1.0; // éviter division par zéro

    Console.WriteLine($"Tile size (m)           : {tileSize}");
    Console.WriteLine($"Altitude max (m)        : {zMax}");
    Console.WriteLine($"Altitude range (m)      : {zMax - zMin}");
    Console.WriteLine($"Height scale (unit/m)   : {heightScale}");
    Console.WriteLine($"Meters per pixel (m/px) : {metersPerPixel}");

    var elevation = 0.0;
    // Boucle sur la grille avec filtrage
    for (var x = 0; x < width - step; x += step)
    {
      for (var y = 0; y < height - step; y += step)
      {
        var a = RoundElevation(elevationGrid[x, y]);
        var b = RoundElevation(elevationGrid[x + step, y]);
        var c = RoundElevation(elevationGrid[x + step, y + step]);
        var d = RoundElevation(elevationGrid[x, y + step]);
        /*
        var a = (elevationGrid[x, y]);
        var b = (elevationGrid[x + step, y]);
        var c = (elevationGrid[x + step, y + step]);
        var d = (elevationGrid[x, y + step]);
        * /
        if (a < b) continue;

        elevation = a;

        // Conversion en coordonnées relatives centrées
        var xf = (x - x0);
        var yf = (y - y0);
        var zf = (elevation - zMin) / metersPerPixel * exageration;

        obj.AppendLine(
          $"v {xf.ToString(CultureInfo.InvariantCulture)} {zf.ToString(CultureInfo.InvariantCulture)} {yf.ToString(CultureInfo.InvariantCulture)}");
      }
    }

    File.WriteAllText("terrain.obj", obj.ToString());

    // Sauvegarde du fichier
    File.WriteAllText("mesh.obj", obj.ToString());

    return obj.ToString();
  }







    Console.WriteLine($"Tile size (m)           : {tileSize}");
    Console.WriteLine($"Altitude max (m)        : {elevationsScale.Max}");
    Console.WriteLine($"Altitude range (m)      : {elevationsScale.Max - elevationsScale.Min}");
    Console.WriteLine($"Height scale (unit/m)   : {heightScale}");
    Console.WriteLine($"Meters per pixel (m/px) : {mpp}");
*/