using System.Globalization;
using System.Text;
using Elevation.Interfaces.Services;
using Elevation.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Elevation.Services;

public class General(HttpClient httpClient, IConfiguration configuration) : IGeneral
{
  #region GetTile
  /// <summary>
  /// Renvoi une tuile x, y , zoom d'après des coordonnées
  /// </summary>
  /// <param name="lat"></param>
  /// <param name="lon"></param>
  /// <param name="zoom"></param>
  /// <returns></returns>
  public Tile GetTile(double lat, double lon, int zoom) => new (lat, lon, zoom);
  #endregion
  
  
  
  /// <summary>
  /// Renvoi un raster <c>Bitmap</c> d'après une tuile <c>Tile</c>
  /// </summary>
  /// <param name="tile"></param>
  /// <returns></returns>
  public async Task<Image<Rgba32>> GetRaster(Tile tile)
  {
    var token = configuration["Secrets:MapBoxToken"];
    var url =
      $"https://api.mapbox.com/v4/mapbox.terrain-rgb/{tile.Z}/{tile.X}/{tile.Y}.pngraw?access_token={token}";

    using var response = await httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();

    await using var stream = await response.Content.ReadAsStreamAsync();
    return await Image.LoadAsync<Rgba32>(stream);
  }
  
  
  /// <summary>
  /// Récupère le x et y d'un pixel en fonction des coordonnées géospatiales pour un zoom et un raster donné.
  /// </summary>
  /// <param name="raster"></param>
  /// <param name="latitude"></param>
  /// <param name="longitude"></param>
  /// <param name="zoom"></param>
  /// <returns></returns>
  public Pixel GetPixel(Image<Rgba32> raster, double latitude, double longitude, int zoom = 14)
  {
    // Longitude normalisée entre 0 et 1.
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

    // Conversion en coordonné pixel dans la tuile (0..255)
    var pixelX = (int)(localX * 256);
    var pixelY = (int)(localY * 256);

    // Sécurité : on borne les valeurs
    pixelX = Math.Clamp(pixelX, 0, 255);
    pixelY = Math.Clamp(pixelY, 0, 255);
    var color = raster[pixelX, pixelY];

    return new Pixel(pixelX, pixelY, color);
  }
  
  
  /// <summary>
  /// Renvoi une élévation en mètre en fonction d'une <c>Color</c>
  /// </summary>
  /// <param name="c"></param>
  /// <returns></returns>
  public double GetElevation(Rgba32 c) => Math.Round((-10000 + ((c.R * 256 * 256 + c.G * 256 + c.B) * 0.1)), 1);
  
  
  /// <summary>
  /// Renvoi une élévation en mètres à partir d'une image raster.
  /// </summary>
  /// <param name="raster"></param>
  /// <returns></returns>
  public double GetElevation(Image<Rgba32> raster)
  {
    var color = raster[1, 1];
    return GetElevation(color);
  }
  
  
  /// <summary>
  /// Renvoi une liste de coordonnées enrichies avec une élévation d'après une liste de coordonnées simples.
  /// </summary>
  /// <param name="coordinatesList"></param>
  /// <returns></returns>
  public async Task<List<Coordinates>> GetElevation(List<Coordinates> coordinatesList)
  {
    var nombreAppelMapBox = 0;
    var result = new List<Coordinates>(coordinatesList.Count);
    var rasterCache = new Dictionary<Tile, Image<Rgba32>>();

    foreach (var coord in coordinatesList)
    {
      var tile = new Tile(coord.Latitude, coord.Longitude);

      if (!rasterCache.TryGetValue(tile, out var raster))
      {
        raster = await GetRaster(tile);
        rasterCache[tile] = raster;
        nombreAppelMapBox++;
      }
      
      var pixel = GetPixel(raster, coord);
      var elevation = GetElevation(pixel.Color);

      result.Add(coord with { Altitude = elevation });
    }
    return result;
  }
  
  
  /// <summary>
  /// Renvoi une liste de coordonnées enrichie avec l'altitude. 
  /// </summary>
  /// <param name="raster"></param>
  /// <returns></returns>
  public List<Coordinates> GetAllElevationInRaster(Image<Rgba32> raster)
  {
    var coordinates = new List<Coordinates>(raster.Width * raster.Height);

    for (var i = 0; i < raster.Width; i++)
    {
      for (var u = 0; u < raster.Height; u++)
      {
        var color = raster[i, u];
        var elevation = GetElevation(color);
        coordinates.Add(new Coordinates(Latitude:i, Longitude:u, Altitude:elevation));
      }
    }
    return coordinates;
  }
  
  
  /// <summary>
  /// Renvoi un <c>Pixel</c> d'une image raster selon des coordonnées géographiques.
  /// </summary>
  /// <param name="raster"></param>
  /// <param name="coord"></param>
  /// <returns></returns>
  public Pixel GetPixel(Image<Rgba32> raster, Coordinates coord) => GetPixel(raster, coord.Latitude, coord.Longitude);
}

/*

    
    int width = 256 / 8;
    int height = 256 / 8;

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
    */