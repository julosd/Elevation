using System.Globalization;
using System.Text;
using Elevation.Models;

namespace Elevation.Services;

public class Modelisation : IModelisation
{
  /// <summary>
  /// Crée un mesh à partir de coordonnées géographiques.
  /// </summary>
  /// <param name="coordinates"></param>
  public string CreateMesh(List<Coordinates> coordinates)
  {
    var obj = new StringBuilder();
    var inv = CultureInfo.InvariantCulture;
    
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
        
        //var y = (coordonnee.Longitude - lon0) * Math.Cos(lat0Rad) * metersPerDegree;
        var x = (coordonnee.Latitude -  lat0) * metersPerDegree;
        var y = (coordonnee.Longitude - lon0) * Math.Cos(lat0Rad) * metersPerDegree;
        //var z = (coordonnee.Altitude - zMin) / (zMax - zMin) * 10.0;
        var z = (coordonnee.Altitude - zMin) / 64; // ICI il faut connaitre la largeur de la tuile pour que la hauteur corresponde

        z ??= 0;
      
        obj.AppendLine($"v {(x).ToString(inv)} {z!.Value.ToString(inv)} {y.ToString(inv)}");
        //obj.AppendLine($"v {(x + 0.5).ToString(inv)} {z} {y.ToString(inv)}");
      }
    }
    
    File.WriteAllText("mesh.obj", obj.ToString());
    
    return obj.ToString();
  }
}