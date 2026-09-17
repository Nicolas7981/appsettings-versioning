using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/config", (IConfiguration c) => c["ConnectionStrings:DefaultConnection"]);

app.MapGet("/db-check", async (IConfiguration c) =>
{
    var connStr = c["ConnectionStrings:DefaultConnection"];
    try
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync();
        return Results.Ok(new { status = "connected" });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();
