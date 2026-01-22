using System.Globalization;
using System.Text;
using Elevation.Models;

namespace Elevation.Services;

public sealed class Modelisation : IModelisation
{
  /// <summary>
  /// Crée un mesh à partir de coordonnées géographiques.
  /// </summary>
  /// <param name="coordinates"></param>
  public string CreateMesh(List<Coordinates> coordinates)
  {
    var obj = new StringBuilder();
    
    var zMin = coordinates.Min(c => c.Altitude);
    var zMax = coordinates.Max(c => c.Altitude);

    var lat0 = coordinates[0].Latitude;
    var lon0 = coordinates[0].Longitude;

    var lat0Rad = lat0 * Math.PI / 180.0;
    const double metersPerDegree = 1;

    
    foreach (var coordonnee in coordinates)
    {
      if (coordonnee.Latitude % 8 == 0 && coordonnee.Longitude % 8 == 0)
      {
        var x = (coordonnee.Latitude -  lat0) * metersPerDegree;
        var y = (coordonnee.Longitude - lon0) * Math.Cos(lat0Rad) * metersPerDegree;
        var z = (coordonnee.Altitude - zMin) / 64; // ICI il faut connaitre la largeur de la tuile pour que la hauteur corresponde
        
        obj.AppendLine($"v {(x).ToString(CultureInfo.InvariantCulture)} {z.ToString(CultureInfo.InvariantCulture)} {y.ToString(CultureInfo.InvariantCulture)}");
        //obj.AppendLine($"v {(x + 0.5).ToString(inv)} {z} {y.ToString(inv)}");
      }
    }
    
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