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
}