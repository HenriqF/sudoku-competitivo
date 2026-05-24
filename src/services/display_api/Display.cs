using System.Net;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.HttpResults;


using contracts;
namespace Displayapi;

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
        
        app.MapGet("/online", () =>
        {
            return Results.Ok("sim!");
        }).WithName("online");

        app.MapGet("/sudoku", async () => {
            HttpClient? client = cf.CreateClient("sudoku_s");
            new_sudokus? response = await client.GetFromJsonAsync<new_sudokus>("new");

            return Results.Ok(response?.boards);
        }).WithName("sudokus");

        app.MapGet("/leaderboard", async () =>
        {
            HttpClient? client = cf.CreateClient("bd_s");
            var response = await client.GetAsync("leaderboard");

            if (response.StatusCode == HttpStatusCode.NoContent || ! response.IsSuccessStatusCode)
            {
                return Results.NoContent();
            }

            var dados = await client.GetFromJsonAsync<List<player_elo_rel>>("/leaderboard");

            var formatado = dados!.Select(u => new object[] {u.nome, u.elo, u.foto}).ToList();

            return Results.Ok(formatado);
        }).WithName("leaderboard");

        app.MapGet("/stats/{nome}", async (string nome) => {
            var client = cf.CreateClient("bd_s");

            var response = await client.GetAsync($"/stats/{nome}");

            if (! response.IsSuccessStatusCode)
            {
                return Results.NotFound("jogador não existe");
            }


            var stats = await client.GetFromJsonAsync<user_stats>($"/stats/{nome}");

            return Results.Ok(stats);
        }).WithName("stats");

        app.MapGet("/nomeexiste/{nome}", async (string nome) =>
        {
            HttpClient? client = cf.CreateClient("bd_s");
            var response = await client.GetFromJsonAsync<int>($"/existe/{nome}");
            return Results.Ok(response); 
        }).WithName("existe");;

        app.Run();

    }
}

public partial class Program { }