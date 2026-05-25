using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Drawing;
using System.Net.Http.Json; /* eu MARCELO botei isso */
using System.Threading.Tasks; /* eu MARCELO botei isso */
using System.Net.Http; /* eu MARCELO botei isso */
using System.Security.Cryptography; /* eu Marcelo botei isso */
using Microsoft.IdentityModel.Tokens; /* eu Marcelo botei isso */

using contracts;
using System.Collections.Specialized;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using System.Threading.Channels;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices; /* eu MARCELO botei isso */
namespace WebSocketServer;




public class MatchMaker : BackgroundService, IHostedService
{
    public Action<string, string>? _onMatch {get; set;}


    private readonly Channel<mm_player_info> _queue;


    private List<mm_player_info> _jogadores;


    public MatchMaker()
    {
        _queue = Channel.CreateUnbounded<mm_player_info>();
        _jogadores = [];
    }



    public async ValueTask entrar_queue(mm_player_info jogador)
    {
        await _queue.Writer.WriteAsync(jogador);
    }


    private bool match_make()
    {
        //
        foreach (mm_player_info j in _jogadores)
        {
            int diff = (int)(DateTime.Now - j.entrada).TotalMilliseconds;
            j.range = ((diff / 6000) * 100) + 100;
            Console.WriteLine($"{j.nome} procurando: ({j.elo+j.range} - {j.elo-j.range})elo - espera {diff}ms");
        } 


        for (int i = 0 ;i < _jogadores.Count; i++)
        {
            mm_player_info jog = _jogadores[i];

            mm_player_info? match = _jogadores.Where(j => 
                j != jog && 
                Math.Abs(j.elo - jog.elo) <= j.range &&
                Math.Abs(j.elo - jog.elo) <= jog.range 
            ).MinBy(j => j.entrada);

            if (match != null)
            {
                _jogadores.Remove(jog);
                _jogadores.Remove(match);

                _onMatch?.Invoke(jog.nome, match.nome);
                return true;
            }
        }
        return false;
    }




    protected override async Task ExecuteAsync(CancellationToken cancelar)
    {   
        while (!cancelar.IsCancellationRequested)
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancelar);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            try
            {
                if (await _queue.Reader.WaitToReadAsync(cts.Token))
                {
                    
                    while (_queue.Reader.TryRead(out mm_player_info? jog))
                    {
                        if (jog == null)continue;

                        if (jog.saiu)
                        {
                            lock (_jogadores)
                            {
                                _jogadores.RemoveAll(j => j.nome == jog.nome);
                            }
                            continue;
                        }

                        lock (_jogadores)
                        {
                            _jogadores.Add(jog);
                        }
                    }



                    while(match_make());
                }


            }
            catch (OperationCanceledException)
            {
                if (cancelar.IsCancellationRequested) break;

                if (_jogadores.Count > 0) {
                    while(match_make());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"MM: deu grandes merdas {e}");
            }
        }
    }
}




public class WebSocketServer
{
    private static MatchMaker mm = null!;

    //variaveis    
    private static IHttpClientFactory? cf = null;
    private static ConcurrentDictionary<string, WebSocket> _clients_sockets = new(); //playre weebsocket
    private static ConcurrentDictionary<string, user_stats> _clients_stats = new(); //player stats ()
    private static ConcurrentDictionary<string, pc_info> _playing_client_info = new();
    //-------


    private static async Task MessageClientAsync(string message, WebSocket webSocket)
    {
        try
        {
            byte[] response = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"erro ao mandar mensagem: {e}");
            return;
        }
    }




    private static async Task<bool> UpdateStats(string jog)
    {
        HttpClient? client = cf!.CreateClient("bd_s");
        var get_stats_response = await client.GetAsync($"http://localhost:5127/stats/{jog}");
        if (! get_stats_response.IsSuccessStatusCode)return false;

        user_stats? stats = await client.GetFromJsonAsync<user_stats>($"http://localhost:5127/stats/{jog}");
        if (stats == null)return false;

        _clients_stats.AddOrUpdate(jog, stats, (k, e) => stats);
        return true;
    }

    private static async void MatchFound(string p1, string p2)
    {
        HttpClient? client = cf!.CreateClient("sudoku_s");
        new_sudokus? sudoku = await client.GetFromJsonAsync<new_sudokus>(
            "/new"
        );

        if (sudoku == null)
        {
            await MessageClientAsync("falha ao gerar sudokus...", _clients_sockets[p1]);
            await MessageClientAsync("falha ao gerar sudokus...", _clients_sockets[p2]);
            return;
        }
        await MessageClientAsync($"opp: {p2} {_clients_stats[p2].elo} {_clients_stats[p2].foto_link}", _clients_sockets[p1]);
        await MessageClientAsync($"opp: {p1} {_clients_stats[p1].elo} {_clients_stats[p1].foto_link}", _clients_sockets[p2]);
        await MessageClientAsync($"timer:", _clients_sockets[p1]);
        await MessageClientAsync($"timer:", _clients_sockets[p2]);
        await Task.Delay(3000);
        await MessageClientAsync($"sudoku: {sudoku.boards[0]} 0", _clients_sockets[p1]);
        await MessageClientAsync($"sudoku: {sudoku.boards[0]} 0", _clients_sockets[p2]);


        DateTime inicio_jogo = DateTime.Now;

        _playing_client_info.TryAdd(p1, new pc_info(
            boards: sudoku.boards,
            opp: p2,
            inicio: inicio_jogo
        ));
        _playing_client_info.TryAdd(p2, new pc_info(
            boards: sudoku.boards,
            opp: p1,
            inicio: inicio_jogo
        ));


        Console.WriteLine($"{p1} vs {p2} - tabuleiros: {sudoku.boards[0]}, {sudoku.boards[1]}");
    }

    private static async Task MatchEnd(string gan, string perd, bool abandono = false)//gangnamstyle
    {
        if (!_playing_client_info.TryRemove(gan, out pc_info? gan_info)) return;
        _playing_client_info.TryRemove(perd, out _);
        

        DateTime fim = DateTime.Now;
        TimeSpan duracao = fim - gan_info!.inicio;
        int dur_total_ms = (int) duracao.TotalMilliseconds;


        int winner_elo = _clients_stats[gan].elo;
        int loser_elo = _clients_stats[perd].elo;

        int prob_w_ganhar = (int) ( 1.0 /( 1 + Math.Pow(10, (loser_elo-winner_elo)/350.0)) *100 );
        int prob_l_ganhar = 100-prob_w_ganhar;

        //Console.WriteLine($"{prob_w_ganhar}%, {prob_l_ganhar}%");

        // int k_factor_w = Math.Max(10, 40-((winner_elo-850)/80));
        // int k_factor_l = Math.Max(10, 40-((loser_elo-850)/80));
        int k_factor_w = 40;
        int k_factor_l = 40;


        int elo_diff_w = (int) (k_factor_w * ((100 - prob_w_ganhar)/100.0));
        int elo_diff_l = (int) (k_factor_l * ((0 - prob_l_ganhar)/100.0));

        int new_elo_w = winner_elo + elo_diff_w;
        int new_elo_l = loser_elo + elo_diff_l;

        string boards = gan_info.boards[0] + gan_info.boards[1]; 

        fim_partida fp = new fim_partida(
            ganhador: gan,
            perdedor: perd,
            tabuleiros: boards,
            elo_diff_ganhador: new_elo_w,
            elo_diff_perdedor: new_elo_l,
            duracao_ms: dur_total_ms,
            abandonou: abandono
        );

        HttpClient? client = cf!.CreateClient("bd_s");
        await client.PutAsJsonAsync("fimpartida", fp);

        _clients_sockets.TryGetValue(perd, out WebSocket? lws);
        _clients_sockets.TryGetValue(gan, out WebSocket? ganws);

        if (lws != null) await MessageClientAsync($"perdeu: {elo_diff_l} {perd} {dur_total_ms}" , lws);
        if (ganws != null) await MessageClientAsync($"ganhou: {elo_diff_w} {gan} {dur_total_ms}" , ganws);

        await UpdateStats(gan);
        await UpdateStats(perd);
    }


    private static async Task HandleClientAsync(string id, WebSocket webSocket)
    {
        var buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close) break;

            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"{id}: {message}");


            if (_playing_client_info.TryGetValue(id, out pc_info? info))
            {
                if (message == info.boards[1])
                {
                    await MatchEnd(id, info.opp);
                }
                
                else if (message.StartsWith("abandonar"))
                {
                    await MatchEnd(info.opp, id, true);
                }
                else if (!message.StartsWith("jogar"))
                {
                    if (info.strikes == 2)
                    {
                        await MatchEnd(info.opp, id, true);
                    }
                    else
                    {
                        info.strikes += 1;
                        await MessageClientAsync($"strike: {info.strikes}" , webSocket); 
                    }
                    
                }
                await MessageClientAsync($"echo: {message}" , webSocket);
            }


            else if (message.StartsWith("jogar") && message.Length == 5)
            {
                
                mm_player_info nj = new mm_player_info(
                    nome: id,
                    elo: _clients_stats[id].elo,
                    range:50,
                    entrada: DateTime.Now
                );

                await mm.entrar_queue(nj);
                await MessageClientAsync("procurando por oponente..." , webSocket);

            }


            else
            {
                await MessageClientAsync($"echo: {message}" , webSocket);
            }
        }
    
    }




    public static void Main(string[] args)
    {
        Console.WriteLine("===================");
        Console.WriteLine("WEBSOCKET WEBSOCKET");
        Console.WriteLine("===================");


        var builder = WebApplication.CreateBuilder(args);
        builder.Services.Configure<HostOptions>(opts =>
        {
            opts.ShutdownTimeout = TimeSpan.FromSeconds(1);
        });
        builder.Services.AddHttpClient("bd_s", client => { client.BaseAddress = new Uri("http://localhost:5127/");});
        builder.Services.AddHttpClient("interface_s", client => { client.BaseAddress = new Uri("http://localhost:5269/");});
        builder.Services.AddHttpClient("sudoku_s", client => { client.BaseAddress = new Uri("http://localhost:5121/");});

        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);

        builder.Services.AddSingleton<MatchMaker>();
        builder.Services.AddHostedService(servp => servp.GetRequiredService<MatchMaker>());

        var app = builder.Build();
        app.UseHttpsRedirection();
        cf = app.Services.GetRequiredService<IHttpClientFactory>();

        mm = app.Services.GetRequiredService<MatchMaker>();
        mm._onMatch = MatchFound;
        
        app.UseWebSockets();

        app.Map("/online", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest) return;
            using var web_socket = await context.WebSockets.AcceptWebSocketAsync();
            await MessageClientAsync($"sim!", web_socket);
            return;
        });

        app.Map("/ws/{token}", async (HttpContext context) =>
        {
            if (!context.WebSockets.IsWebSocketRequest) return;

            string? token = context.Request.RouteValues["token"]?.ToString();
            if (token == null) return;


            HttpClient? client = cf.CreateClient("interface_s");
            var response = await client.GetAsync($"/confirmar/{token}");

            if (response.StatusCode == HttpStatusCode.Unauthorized) return;
            string usuario = (await response.Content.ReadFromJsonAsync<string>())!;


            using var web_socket = await context.WebSockets.AcceptWebSocketAsync();
            string client_id = usuario;
            

            // var get_stats_response = await client.GetAsync($"http://localhost:5127/stats/{client_id}");
            // if (! get_stats_response.IsSuccessStatusCode)return;

            // user_stats? stats = await client.GetFromJsonAsync<user_stats>($"http://localhost:5127/stats/{client_id}");
            // if (stats == null)return;



            if (_clients_sockets.ContainsKey(client_id)){
                await MessageClientAsync($"recusado:", web_socket);
                Console.WriteLine($"CONECXAO RECUSADA POR JA TA JOGANDO: {client_id}");
                return;
            }


            if (_playing_client_info.TryGetValue(client_id, out pc_info? info))
            {   
                Console.WriteLine($"CLIENTE JGOANDO VOLTOU MEU DEUS É CALASEWING! {client_id}");
                await MessageClientAsync($"sudoku: {info.boards[0]} {(int)(DateTime.Now - info.inicio).TotalMilliseconds}", web_socket);
                var oponente = _playing_client_info[client_id].opp;
                await MessageClientAsync($"opp: {oponente} {_clients_stats[oponente].elo} {_clients_stats[oponente].foto_link}", web_socket);
                //await MessageClientAsync($"tempopassado: {(int)(DateTime.Now - info.inicio).TotalMilliseconds}", web_socket);
            }


            try
            {
                _clients_sockets.TryAdd(client_id, web_socket);
                if (! await UpdateStats(client_id)) return;

                // _clients_stats.TryAdd(client_id, stats);

                Console.WriteLine($"novo cliente: {client_id}");
                await MessageClientAsync($"voce é {client_id}", web_socket);
                await HandleClientAsync(client_id, web_socket);
            }
            catch (Exception e) 
            {
                Console.WriteLine($"erro fatal: {client_id} -> {e}");
            }
            finally
            {
                mm_player_info saindo = new mm_player_info(
                    nome: client_id,
                    elo: 6769,
                    range: 61,
                    entrada: DateTime.Now
                );
                saindo.saiu = true;
                await mm.entrar_queue(saindo);

                //_clients_stats.TryRemove(client_id, out _);
                _clients_sockets.TryRemove(client_id, out _);
                Console.WriteLine($"saiu: {client_id}");
            }
        });

        app.Run();
    }
}




public record pc_info
{
    public string[] boards {get; set;}
    public string opp {get; set;}
    public DateTime inicio {get; set;}

    public int strikes {get; set;}

    public pc_info(string[] boards, string opp, DateTime inicio)
    {
        this.boards = boards;
        this.opp = opp;
        this.inicio = inicio;
        strikes = 0;
    }
}

public record mm_player_info{
    public string nome { get; set; }
    public int elo { get; set; }
    public int range { get; set; }
    public DateTime entrada { get; set; }

    public bool saiu { get; set; }

    public mm_player_info(string nome, int elo, int range, DateTime entrada)
    {
        this.nome = nome;
        this.elo = elo;
        this.range = range;
        this.entrada = entrada;        
        saiu = false;
    }
};

public partial class Program { }