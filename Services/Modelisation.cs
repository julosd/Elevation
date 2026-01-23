using System.Globalization;
using System.Text;
using Elevation.Models;

namespace Elevation.Services;

public sealed class Modelisation : IModelisation
{
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

    // Boucle sur la grille avec filtrage
    for (var x = 0; x < width; x += step)
    {
      for (var y = 0; y < height; y += step)
      {
        var z = elevationGrid[x, y];

        // Conversion en coordonnées relatives centrées
        var xf = (x - x0);
        var yf = (y - y0);
        var zf = (z - zMin) / metersPerPixel * exageration;

        obj.AppendLine($"v {xf.ToString(CultureInfo.InvariantCulture)} {zf.ToString(CultureInfo.InvariantCulture)} {yf.ToString(CultureInfo.InvariantCulture)}");
      }
    }

    // Sauvegarde du fichier
    File.WriteAllText("mesh.obj", obj.ToString());

    return obj.ToString();
  }






  public Dictionary<double, List<Coordinates>> CreateLevel(List<Coordinates> coordinates)
  {
    const double step = 10.0;

    return coordinates
      .GroupBy(c => Math.Floor(c.Altitude / step) * step)
      .OrderBy(g => g.Key)
      .ToDictionary(
        g => g.Key,
        g => g.ToList()
      );
  }
  
  
  
  /// <summary>
  /// Renvoi les coordonnées identifiables avec un (x et y) 
  /// </summary>
  /// <param name="coordinates"></param>
  /// <returns></returns>
  public Dictionary<(int X, int Y), int> IndexElevationByXY(IEnumerable<Coordinates> coordinates)
  {
    return coordinates.ToDictionary(
      c => ((int)c.Latitude, (int)c.Longitude),
      c => (int)c.Altitude
    );
  }

  
  
  

  public void CreateTerrain(Dictionary<double, List<Coordinates>> levels)
  {
    var obj = new StringBuilder();
    var inv = CultureInfo.InvariantCulture;
    Console.WriteLine(levels.Keys.Count);
    foreach (var (altitude, coordonnees) in levels.OrderBy(c => c.Key))
    {
      if (altitude == 1200.0)
      {
        var coordinatesEnumerable = coordonnees.OrderBy(c => c.Latitude).ThenBy(c => c.Longitude);
        foreach (var c in coordinatesEnumerable)
        {
          var x = (c.Latitude - 128);
          var y = (c.Longitude - 128);
          var z = (c.Altitude - 1000.0) / 64;

          obj.AppendLine($"v {(x).ToString(inv)} {z.ToString(inv)} {y.ToString(inv)}");
        }
      }
    }
    File.WriteAllText("levels.obj", obj.ToString());
  }
}