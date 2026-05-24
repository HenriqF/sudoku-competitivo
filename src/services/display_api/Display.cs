using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;


using contracts;

class Display
{
    static void Main(string[] args)
    {
        Console.WriteLine("===============");
        Console.WriteLine("DISPLAY DISPLAY");
        Console.WriteLine("===============");


        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHttpClient("sudoku_s", client => { client.BaseAddress = new Uri("http://localhost:5121/");});
        builder.Services.AddHttpClient("bd_s", client => { client.BaseAddress = new Uri("http://localhost:5127/"); });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        
        IHttpClientFactory cf = app.Services.GetRequiredService<IHttpClientFactory>();
        

 
        app.MapGet("/sudoku", async () => {
            HttpClient? client = cf.CreateClient("sudoku_s");
            new_sudokus? response = await client.GetFromJsonAsync<new_sudokus>("new");

            return Results.Ok(response?.boards);
        }).WithName("GetNewBoards");

        app.MapGet("/leaderboard", async () =>
        {
            return Results.Ok("wow");
        });

        app.MapGet("/stats/{nome}", async (string nome) => {
            var client = cf.CreateClient("bd_s");

            var response = await client.GetAsync($"/stats/{nome}");

            if (! response.IsSuccessStatusCode)
            {
                return Results.NotFound("jogador não existe");
            }


            var stats = await client.GetFromJsonAsync<user_stats>($"/stats/{nome}");

            return Results.Ok(stats);
        }).WithName("GetUserStats");

        app.Run();

    }
}
