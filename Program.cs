using Elevation.Interfaces.Services;
using Elevation.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IGeneral, General>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/elevation", async (IGeneral general, double latitude, double longitude, int zoom) =>
{
  try
  {
    var tile = general.GetTile(latitude, longitude, zoom);
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










/*
app.MapGet("/tiles", (IGeneral general, double latitude, double longitude, int zoom) =>
{
  var tile = general.GetTile(latitude, longitude, zoom);
  var url =
    $"curl https://api.mapbox.com/v4/mapbox.satellite/{tile.Z}/{tile.X}/{tile.Y}@2x.png?access_token=pk.eyJ1IjoiZ3pvciIsImEiOiJjbTIwczk2MG8waGdqMmpzOHR2cjd2MDkwIn0.MC7S7t14bEbVQ7Tf3NdvVg --output \"test.png\"";
  return Results.Ok(url);
});
*/



app.Run();