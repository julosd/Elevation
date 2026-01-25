using System.Globalization;
using System.Xml.Linq;
using Elevation.Interfaces.Services;
using Elevation.Models;
using Elevation.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IGeneral, General>();
builder.Services.AddScoped<IModelisation, Modelisation>();

var app = builder.Build();

app.UseHttpsRedirection();

#region elevation
app.MapGet("/elevation", async (IGeneral general, double latitude, double longitude, int zoom = 14) =>
{
  try
  {
    var tile = general.GetTile(latitude, longitude, zoom);
    var raster = await general.GetRaster(tile);
    var pixel = general.GetPixel(raster, latitude, longitude, zoom);
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
#endregion







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



app.MapGet("/mesh/", async (IGeneral general, IModelisation modelisation, double latitude, double longitude, int zoom = 14, int topographyStep = 1) =>
{
  var tile = general.GetTile(latitude, longitude, zoom);
  var raster = await general.GetRaster(tile);
  var coordinates = general.ExtractCoordinatesFromRaster(raster);
  //coordinates = general.AddCoordinates(coordinates);
  //coordinates = general.AddCoordinates(coordinates);
  //coordinates = general.AddCoordinates(coordinates);
  
  var options = new MeshOptions(topographyStep, exaggeration: 1, 1);
  var parameters = new MeshParameters(latitude, zoom, coordinates, topographyStep);
  var mesh = modelisation.CreateMesh(coordinates, options, parameters);

  return Results.Ok("ok");
});


app.MapGet("/mesh-z/", async (IGeneral general, IModelisation modelisation, double latitude, double longitude, int zoom = 14, int topographyStep = 1) =>
{
  var tile = general.GetTile(latitude, longitude, zoom);
  var tileSize = general.GetTileSize(latitude, zoom);
  var raster = await general.GetRaster(tile);
  var coordinates = general.ExtractCoordinatesFromRaster(raster);
  var levels = modelisation.CreateLevel(coordinates, topographyStep);
  
  var options = new MeshOptions(topographyStep, exaggeration: 1, 1);
  var parameters = new MeshParameters(latitude, zoom, coordinates, topographyStep);
  var mesh = modelisation.CreateMesh2(levels, options, parameters);
  
  Console.WriteLine("ok");

  return Results.Ok("ok");
});



app.MapGet("/test2/", async (IGeneral general, IModelisation modelisation, double latitude, double longitude, int zoom = 14) =>
{
  var tile = general.GetTile(latitude, longitude, zoom);
  var raster = await general.GetRaster(tile);
  var coordinates = general.ExtractCoordinatesFromRaster(raster);
  //var levels = modelisation.IndexElevationByXY(coordinates);
  //modelisation.CreateTerrain(levels);
  

  return Results.Ok("ok");
});





app.MapGet("/tiles", (IGeneral general, IConfiguration configuration, double latitude, double longitude, int zoom) =>
{
  var token = configuration["Secrets:MapBoxToken"];
  var tile = general.GetTile(latitude, longitude, zoom);
  var url = $"curl https://api.mapbox.com/v4/mapbox.satellite/{tile.Z}/{tile.X}/{tile.Y}@2x.png?access_token={token} --output test.png";
  return Results.Ok(url);
});




app.Run();











/*
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
*/