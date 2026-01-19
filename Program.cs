using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Elevation.Interfaces.Services;
using Elevation.Models;
using Elevation.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IGeneral, General>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/elevation", async (IGeneral general, double latitude, double longitude) =>
{
  try
  {
    var tile = general.GetTile(latitude, longitude, 30);
    var raster = await general.GetRaster(tile);
    var elevation = general.GetElevation(raster);
    return Results.Ok(elevation);
  }
  catch (HttpRequestException ex)
  {
    return Results.Problem(
      detail: ex.Message,
      statusCode: 503,
      title: "Erreur lors de la récupération de la tuile"
    );
  }
  catch (Exception ex)
  {
    return Results.Problem(
      statusCode: 500,
      title: "Erreur serveur."
    );
  }
});

app.MapGet("/track", async (IGeneral general) =>
{
  try
  {
    var doc = XDocument.Load("../../.temp/kml.kml");

    var coordinatesText = doc
      .Descendants()
      .FirstOrDefault(e => e.Name.LocalName == "coordinates")
      ?.Value;

    if (string.IsNullOrWhiteSpace(coordinatesText))
      throw new InvalidOperationException("Aucune coordonnée trouvée dans le KML.");
    
    var coordonnees = new List<Coordinates>();

    foreach (var c in coordinatesText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
    {
      var parts = c.Split(',');
      if (parts.Length < 2)
        throw new FormatException($"Coordonnée invalide : {c}");

      var lon = double.Parse(parts[0], CultureInfo.InvariantCulture);
      var lat = double.Parse(parts[1], CultureInfo.InvariantCulture);
      
      coordonnees.Add(new Coordinates(lat, lon));

    }
    Console.WriteLine(coordonnees.Count);
    var result = await general.GetElevation(coordonnees);
    return Results.Ok(result);
  }
  catch (Exception ex)
  {
    return Results.Problem(
      detail: ex.Message,
      statusCode: 500,
      title: "Erreur serveur"
    );
  }
});




app.MapGet("/elevation2", async (IGeneral general, double latitude, double longitude, int zoom = 14) =>
{
  try
  {
    var tile = general.GetTile(latitude, longitude, 14);
    var raster = await general.GetRaster(tile);
    var pixel = general.GetPixel(raster, latitude, longitude, 14);
    var elevation = general.GetElevation(pixel.Color);
    return Results.Ok(elevation);
  }
  catch (HttpRequestException ex)
  {
    return Results.Problem(
      detail: ex.Message,
      statusCode: 503,
      title: "Erreur lors de la récupération de la tuile"
    );
  }
  catch (Exception ex)
  {
    return Results.Problem(
      statusCode: 500,
      title: "Erreur serveur."
    );
  }
});

app.MapGet("/test/", async (IGeneral general, double latitude, double longitude, int zoom = 14) =>
{
  var tile = general.GetTile(latitude, longitude, zoom);
  var raster = await general.GetRaster(tile);
  var coordinates = general.GetAllElevationInRaster(raster);
  general.Create3dObjects(coordinates);

  return Results.Ok("ok");
});









app.MapGet("/tiles", (IGeneral general, double latitude, double longitude, int zoom) =>
{
  var tile = general.GetTile(latitude, longitude, zoom);
  var url =
    $"curl https://api.mapbox.com/v4/mapbox.satellite/{tile.Z}/{tile.X}/{tile.Y}@2x.png?access_token=pk.eyJ1IjoiZ3pvciIsImEiOiJjbTIwczk2MG8waGdqMmpzOHR2cjd2MDkwIn0.MC7S7t14bEbVQ7Tf3NdvVg --output \"test.png\"";
  return Results.Ok(url);
});




app.Run();