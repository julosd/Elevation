using System.Drawing;
using Elevation.Interfaces.Services;
using Elevation.Models;

namespace Elevation.Services;

public class General(HttpClient httpClient, IConfiguration configuration) : IGeneral
{
  public Tile GetTile(double lat, double lon, int zoom)
  {
    return new Tile(lon, lat, zoom);
  }

  public async Task<Bitmap> GetRaster(Tile tile)
  {
    var token = configuration["Secrets:MapBoxToken"];
    var url = $"https://api.mapbox.com/v4/mapbox.terrain-rgb/{tile.Z}/{tile.X}/{tile.Y}.pngraw?access_token={token}";
    using var response = await httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();

    // Lire le flux
    using var stream = await response.Content.ReadAsStreamAsync();
    return new Bitmap(stream);
  }

  public double GetElevation(Bitmap bitmap)
  {
    Color c = bitmap.GetPixel(1, 1);
    int r = c.R;
    int g = c.G;
    int b = c.B;
    
    return Math.Round((-10000 + ((r * 256 * 256 + g * 256 + b) * 0.1)), 1);
  }
  
  
  public double GetElevation(Color c)
  {
    int r = c.R;
    int g = c.G;
    int b = c.B;
    
    return Math.Round((-10000 + ((r * 256 * 256 + g * 256 + b) * 0.1)), 1);
  }

  
  
  /// <summary>
  /// Récupère le x et y d'un pixel en fonction des coordonnées géospatioales pour un zoom et une raster donné.
  /// </summary>
  /// <param name="bitmap"></param>
  /// <param name="latitude"></param>
  /// <param name="longitude"></param>
  /// <param name="zoom"></param>
  /// <returns></returns>
  public Pixel GetPixel(Bitmap bitmap, double latitude, double longitude, int zoom)
  {
    // Longitude normalisée entre 0 et 1
    var u = (longitude + 180.0) / 360.0;

    // Latitude convertie en radians
    var latRad = latitude * Math.PI / 180.0;

    // Latitude normalisée (projection Web Mercator)
    var v =
      (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI)
      / 2.0;

    // Nombre de tuiles pour ce niveau de zoom
    var n = 1 << zoom;

    // Position du point en coordonnées de tuile (valeur flottante)
    var tileXf = u * n;
    var tileYf = v * n;

    // Indices entiers de la tuile contenant le point
    var tileX = (int)Math.Floor(tileXf);
    var tileY = (int)Math.Floor(tileYf);

    // Position relative du point à l’intérieur de la tuile (0..1)
    var localX = tileXf - tileX;
    var localY = tileYf - tileY;

    // Conversion en coordonnées pixel dans la tuile (0..255)
    var pixelX = (int)(localX * 256);
    var pixelY = (int)(localY * 256);

    // Sécurité : on borne les valeurs
    pixelX = Math.Clamp(pixelX, 0, 255);
    pixelY = Math.Clamp(pixelY, 0, 255);
    var pixelValue = bitmap.GetPixel(pixelX, pixelY);

    return new Pixel(pixelX, pixelY, pixelValue);
  }

}