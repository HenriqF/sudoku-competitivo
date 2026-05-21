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
using System.Net.Http.Headers; /* eu MARCELO botei isso */
namespace Sockets.WebSocketServer;




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


    private async Task<bool> match_make(CancellationToken c)
    {
        //
        foreach (mm_player_info j in _jogadores)
        {
            int diff = (int)(DateTime.Now - j.entrada).TotalMilliseconds;
            j.range = ((diff / 3000) * 100) + 200;
            Console.WriteLine($"JOGADOR NA QUEUE MEU DUS OMG {j.nome}, {diff}, {j.range}, {j.elo}");
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
                        lock (_jogadores)
                        {
                            _jogadores.Add(jog);
                        }
                    }



                    while(await match_make(cancelar));
                }


            }
            catch (OperationCanceledException)
            {
                if (cancelar.IsCancellationRequested) break;

                Console.WriteLine("mm ping");
                if (_jogadores.Count > 0) {
                    await match_make(cancelar);
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
    private static ConcurrentDictionary<string, WebSocket> _clients_sockets = new(); //playre weebsocket
    private static ConcurrentDictionary<string, user_stats> _clients_stats = new(); //player stats ()

    private static int _elo_prestiege_size = 300;
    private static ConcurrentDictionary<int, string> _queued_clients = new();  //leo bucket, playre

    private static ConcurrentDictionary<string, string[]> _playing_clients_boards = new();  //player, sudoku_board
    private static ConcurrentDictionary<string, DateTime> _playing_clients_start = new();  //player, tempo_inicio
    private static ConcurrentDictionary<string, string> _playing_opponent = new(); //player opp

    //-------


    private static void RemovePlayingClient(string id)
    {   
        _playing_clients_start.TryRemove(id, out _);
        _playing_clients_boards.TryRemove(id, out _);
        _playing_opponent.TryRemove(id, out _);
    }

    private static async void MatchFound(string p1, string p2)
    {
        var client = new HttpClient();
        new_sudokus? sudoku = await client.GetFromJsonAsync<new_sudokus>(
            "http://localhost:5121/new"
        );

        if (sudoku == null)
        {
            await MessageClientAsync("falha ao gerar sudokus...", _clients_sockets[p1]);
            await MessageClientAsync("falha ao gerar sudokus...", _clients_sockets[p2]);
            return;
        }

        _playing_clients_boards.TryAdd(p1, sudoku.boards);
        _playing_clients_boards.TryAdd(p2, sudoku.boards);

        _playing_opponent.TryAdd(p1, p2);
        _playing_opponent.TryAdd(p2, p1);

        await MessageClientAsync("sudoku:" + sudoku.boards[1], _clients_sockets[p1]);
        await MessageClientAsync("sudoku:" + sudoku.boards[1], _clients_sockets[p2]);

        DateTime inicio = DateTime.Now;
        _playing_clients_start.TryAdd(p1, inicio);
        _playing_clients_start.TryAdd(p2, inicio);

        Console.WriteLine($"TABULEIROS: {sudoku.boards[0]}, {sudoku.boards[1]}");
    }




    private static async void FindMatch(string player_name, string id, WebSocket webSocket)
    {
        
        int elo_bucket = _clients_stats[player_name].elo/_elo_prestiege_size;
        if (_queued_clients.ContainsKey(elo_bucket))
        {
            if (_queued_clients[elo_bucket] == id)
            {
                await MessageClientAsync("já está dentro da queue...", webSocket);
                return;
            }

            if (_queued_clients.TryRemove(elo_bucket, out string? opp_id))
            {
                var client = new HttpClient();
                new_sudokus? sudoku = await client.GetFromJsonAsync<new_sudokus>(
                    "http://localhost:5121/new"
                );

                if (sudoku == null)
                {
                    await MessageClientAsync("falha ao gerar sudokus...", webSocket);
                    await MessageClientAsync("falha ao gerar sudokus...", _clients_sockets[opp_id]);
                    return;
                }

                _playing_clients_boards.TryAdd(id, sudoku.boards);
                _playing_clients_boards.TryAdd(opp_id, sudoku.boards);

                _playing_opponent.TryAdd(id, opp_id);
                _playing_opponent.TryAdd(opp_id, id);

                await MessageClientAsync("sudoku:" + sudoku.boards[1], webSocket);
                await MessageClientAsync("sudoku:" + sudoku.boards[1], _clients_sockets[opp_id]);

                DateTime inicio = DateTime.Now;
                _playing_clients_start.TryAdd(id, inicio);
                _playing_clients_start.TryAdd(opp_id, inicio);

                Console.WriteLine(sudoku.boards[0]);
                Console.WriteLine(sudoku.boards[1]);
                return;
            }
        }


        if (_queued_clients.TryAdd(elo_bucket, id))
        {
            await MessageClientAsync($"procurando por oponente... {elo_bucket}", webSocket);
            return;
        }
        else
        {
            await MessageClientAsync($"falha em entrar na queue.", webSocket);
            return;
        }
    }




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


    private static async Task HandleClientAsync(string id, WebSocket webSocket)
    {
        var buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close) break;

            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"{id}: {message}");


            if (_playing_clients_boards.ContainsKey(id))
            {
                if (message == _playing_clients_boards[id][1])
                {
                    DateTime fim = DateTime.Now;
                    TimeSpan duracao = fim - _playing_clients_start[id];
                    int dur_total_ms = (int) duracao.TotalMilliseconds;


                    int winner_elo = _clients_stats[id].elo;
                    int loser_elo = _clients_stats[_playing_opponent[id]].elo;

                    int prob_w_ganhar = (int) ( 1.0 /( 1 + Math.Pow(10, (loser_elo-winner_elo)/350.0)) *100 );
                    int prob_l_ganhar = 100-prob_w_ganhar;

                    //Console.WriteLine($"{prob_w_ganhar}%, {prob_l_ganhar}%");

                    // int k_factor_w = Math.Max(10, 40-((winner_elo-850)/80));
                    // int k_factor_l = Math.Max(10, 40-((loser_elo-850)/80));
                    int k_factor_w = 40;
                    int k_factor_l = 40;

                    int elo_diff_w = (int) (winner_elo + k_factor_w * ((100 - prob_w_ganhar)/100.0));
                    int elo_diff_l = (int) (loser_elo + k_factor_l * ((0 - prob_l_ganhar)/100.0));

                    string boards = _playing_clients_boards[id][0] +  _playing_clients_boards[id][1]; 



                    
                    fim_partida fp = new fim_partida(
                        ganhador: id,
                        perdedor: _playing_opponent[id],
                        tabuleiros: boards,
                        elo_diff_ganhador: elo_diff_w,
                        elo_diff_perdedor: elo_diff_l,
                        duracao_ms: dur_total_ms
                    );

                    var client = new HttpClient();
                    await client.PutAsJsonAsync("http://localhost:5127/fimpartida", fp);





                    _clients_sockets.TryGetValue(_playing_opponent[id], out WebSocket? oppws);
                    if (oppws != null)await MessageClientAsync($"perdeu: {elo_diff_l}elo" , oppws);
                    await MessageClientAsync($"ganhou: {elo_diff_w}elo" , webSocket);

                    RemovePlayingClient(_playing_opponent[id]);
                    RemovePlayingClient(id);
                }
                else
                {
                    await MessageClientAsync("echo:" + message , webSocket);
                }
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

                //string player_name = id;
                //FindMatch(player_name, id, webSocket);
            }


            else
            {
                await MessageClientAsync("echo:" + message , webSocket);
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
        builder.Services.AddSingleton<MatchMaker>();
        builder.Services.AddHostedService(servp => servp.GetRequiredService<MatchMaker>());
        var app = builder.Build();

        mm = app.Services.GetRequiredService<MatchMaker>();
        mm._onMatch = MatchFound;
        
        app.UseWebSockets();



        app.Map("/ws/{token}", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest) return;

            string? token = context.Request.RouteValues["token"]?.ToString();
            if (token == null) return;

            var client = new HttpClient();
            var response = await client.GetAsync($"http://localhost:5269/confirmar/{token}");

            if (response.StatusCode == HttpStatusCode.Unauthorized) return;
            string usuario = (await response.Content.ReadFromJsonAsync<string>())!;


            using var web_socket = await context.WebSockets.AcceptWebSocketAsync();
            string client_id = usuario;
            



            var get_stats_response = await client.GetAsync($"http://localhost:5127/stats/{client_id}");

            if (! get_stats_response.IsSuccessStatusCode)return;

            user_stats? stats = await client.GetFromJsonAsync<user_stats>($"http://localhost:5127/stats/{client_id}");
            if (stats == null)return;



            if (_clients_sockets.ContainsKey(client_id)){
                Console.WriteLine($"CONECXAO RECUSADA POR JA TA JOGANDO: {client_id}");
                return;
            }


            if (_playing_clients_boards.TryGetValue(client_id, out var boards))
            {   
                Console.WriteLine($"CLIENTE JGOANDO VOLTOU MEU DEUS É CALASEWING! {client_id}");
                await MessageClientAsync("sudoku:" + boards[1], web_socket);
            }


            try
            {
                _clients_sockets.TryAdd(client_id, web_socket);
                _clients_stats.TryAdd(client_id, stats);

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
                if (!_playing_clients_boards.ContainsKey(client_id))
                {
                    int elo_bucket = _clients_stats[client_id].elo/_elo_prestiege_size;
                    _queued_clients.TryRemove(elo_bucket, out _);
                }

                _clients_stats.TryRemove(client_id, out _);
                _clients_sockets.TryRemove(client_id, out _);
                Console.WriteLine($"saiu: {client_id}");
            }
        });

        app.Run();
    }
}




public record mm_player_info{
    public string nome { get; set; }
    public int elo { get; set; }
    public int range { get; set; }
    public DateTime entrada { get; set; }

    public mm_player_info(string nome, int elo, int range, DateTime entrada)
    {
        this.nome = nome;
        this.elo = elo;
        this.range = range;
        this.entrada = entrada;        
    }
};
