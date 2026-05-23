using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using System.Reflection.Metadata;
using System.Security.Cryptography; /* eu MARCELO botei isso */
using contracts;
using System.Text.Json;


public class Usuario
{
    public int id {get; set;}
    public string? email {get; set;}
    public byte[]? senha_hash {get; set;}

    public byte[]? senha_salt {get; set;}
    public string? nome {get; set;}

    public string? foto_link {get; set;}

    public Stats? Stats { get; set; }
    public ICollection<Partida>? JogosUserGanhador {get; set;}
    public ICollection<Partida>? JogosUserDerrotado {get; set;}
}

public class Stats
{
    [Key]
    public int user_id {get; set;}
    public int user_elo {get; set;} = 1000;
    public int qtd_jogos_jogados {get; set;}
    public int qtd_jogos_ganhos {get; set;}
    public int melhor_tempo  {get; set;}


    public Usuario? Usuario { get; set; }
}

public class Partida
{
    public int id {get; set;}
    public int user_ganhador {get; set;}
    public int user_derrotado {get; set;}

    public int user_ganhador_elo {get; set;}
    public int user_derrotado_elo {get; set;}

    public int duracao_ms {get; set;}

    public string? tabuleiros {get;set;}

    public Usuario? UserGanhador { get; set; }
    public Usuario? UserDerrotado { get; set; }
}


public class AppDbContext : DbContext
{
    public DbSet<Usuario> usuarios { get; set; }
    public DbSet<Stats> sudoku_stats { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        DotEnv.Load();

        var builder = new MySqlConnectionStringBuilder
        {
            Server = Environment.GetEnvironmentVariable("HOST"),
            Port = 3306,
            Database = Environment.GetEnvironmentVariable("MYSQL_DATABASE"),
            UserID = "root",
            Password = Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD")
        };

        var connection = builder.ConnectionString;

        options.UseMySql(connection, ServerVersion.AutoDetect(connection));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(user =>
        {   
            user.ToTable("usuarios");
            user.HasKey(u => u.id);


            user.HasOne(u => u.Stats).WithOne(s => s.Usuario).HasForeignKey<Stats>(s => s.user_id);
        });

        modelBuilder.Entity<Partida>(part =>
        {
            part.ToTable("partidas");
            part.HasKey(p => p.id);

            part.HasOne(p => p.UserGanhador).WithMany(u => u.JogosUserGanhador).HasForeignKey(p => p.user_ganhador).OnDelete(DeleteBehavior.SetNull);
            part.HasOne(p => p.UserDerrotado).WithMany(u => u.JogosUserDerrotado).HasForeignKey(p => p.user_derrotado).OnDelete(DeleteBehavior.SetNull);
        });
    }
}




public class Program
{

    static async Task<(string nome, int elo, string foto)[]?> leaderboard(AppDbContext cont)
    {
        try
        {
            (string, int, string)[]? tp = await cont.usuarios.Join(cont.sudoku_stats, u => u.id, s => s.user_id, (u, s) => new { u.nome, s.user_elo, u.foto_link })
                                        .OrderByDescending(i => i.user_elo)
                                        .Take(100)
                                        .Select(i => ValueTuple.Create(i.nome!, i.user_elo, i.foto_link!))
                                        .ToArrayAsync();

            
            return tp.Length > 0 ? tp : null;
        }
        catch
        {
            return null;
        }
    }

    static async Task<bool> novo_user(AppDbContext cont, string nome, string email, string senha, int elo = 1000, string foto_link = ".")
    {
        try
        {
            if (!foto_link.StartsWith("https://i.pinimg.com/"))
            {
                foto_link = "https://i.pinimg.com/1200x/36/bd/a2/36bda22a62ac3d53be8c6664e7f0df31.jpg";
            }

            using var hmac = new HMACSHA512();
            
            byte[] senhaSalt = hmac.Key;
            byte[] senhaHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(senha));

            Usuario novo = new Usuario
            {
                email = email,
                senha_hash = senhaHash,
                senha_salt = senhaSalt,
                nome = nome,
                foto_link = foto_link,
                Stats = new Stats{user_elo = elo},
            };

            cont.Add(novo);
            await cont.SaveChangesAsync();

        }
        catch
        {
            return false;
        }

        return true;
    }

    static async Task<bool> trocar_user_elo(AppDbContext cont, string nome, int elo)
    {
        Usuario? user = await cont.usuarios.Include(u => u.Stats).FirstOrDefaultAsync(u => u.nome == nome);
        if (user == null) return false;
        
        user.Stats!.user_elo = elo;

        cont.SaveChanges();
        return true;
    }

    static async Task<bool> updt_user_melhor_tempo(AppDbContext cont, string nome, int tempo)
    {
        Usuario? user = await cont.usuarios.Include(u => u.Stats).FirstOrDefaultAsync(u => u.nome == nome);
        if (user == null) return false;
        
        if (user.Stats!.melhor_tempo > tempo || user.Stats!.melhor_tempo <= 0)
        {
            user.Stats!.melhor_tempo = tempo;
            cont.SaveChanges();
        }

        
        return true;
    }

    static async Task<bool> trocar_user_dados(AppDbContext cont, change_user_info cui)
    {
        Usuario? user = await cont.usuarios.FirstOrDefaultAsync(u => u.email == cui.email);

        if (user == null) return false;

        if (cui.nome != null && cui.nome != "")
        {
            Usuario? procura = await find_user(cont, cui.nome);
            if (procura == null) user.nome = cui.nome;
        } 
        if (cui.foto != null && cui.foto != "") user.foto_link = cui.foto;
        if (cui.senha != null && cui.senha != "") {
            using var hmac = new HMACSHA512();
            byte[] senhaSalt = hmac.Key;
            byte[] senhaHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(cui.senha));
            user.senha_hash = senhaHash;
            user.senha_salt = senhaSalt;


        }

        cont.SaveChanges();
        return true;
    }

    static async Task<bool> deletar_user(AppDbContext cont, string nome)
    {
        Usuario? user = await cont.usuarios.FirstOrDefaultAsync(u => u.nome == nome);

        if (user != null)
        {
            cont.usuarios.Remove(user);
            cont.SaveChanges();
        }

        return true;
    }


    static async Task<Usuario?> find_user(AppDbContext cont, string nome)
    {
        Usuario? user = null;
        try
        {
            user = await cont.usuarios.Include(u => u.Stats).FirstOrDefaultAsync(u =>
                (
                    u.nome == nome
                )
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e}");
            return user;
        }

        return user;
    }

    static async Task<bool> nova_partida(AppDbContext cont, string ganhador, string derrotado, string tabuleiros, int duracao)
    {
        try
        {
            Usuario? u_ganhador = await find_user(cont, ganhador);
            Usuario? u_derrotado = await find_user(cont, derrotado);
            if (u_ganhador == null || u_derrotado == null)
            {
                return false;
            }

            u_ganhador.Stats!.qtd_jogos_ganhos += 1;
            u_ganhador.Stats!.qtd_jogos_jogados += 1;
            u_derrotado.Stats!.qtd_jogos_jogados += 1;

            Partida nova = new Partida
            {
                user_ganhador = u_ganhador.id,
                user_derrotado = u_derrotado.id,
                user_ganhador_elo = u_ganhador.Stats!.user_elo,
                user_derrotado_elo = u_derrotado.Stats!.user_elo,
                duracao_ms = duracao,
                tabuleiros = tabuleiros
            };

            cont.Add(nova);
            await cont.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e}");
            return false;
        }
        return true;
    }

    static async Task<Usuario?> userDoEmail(AppDbContext cont, string email)
    {
        Usuario? user = null;
        try
        {
            user = await cont.usuarios.FirstOrDefaultAsync(u =>
                (
                    u.email == email
                )
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e}");
            return user;
        }

        return user;
    }






    static async Task testar(AppDbContext cont)
    {
        Usuario? analise = await find_user(cont, "pedro");
        if (analise != null) goto deu_ruim;

        Console.WriteLine("pedro não exite");

        if (!await novo_user(cont, "pedro", "wow.com", "123")) goto deu_ruim;

        Console.WriteLine("pedro criado");

        analise = await find_user(cont, "pedro");
        if (analise == null) goto deu_ruim;

        Console.WriteLine($"pedro existe: {analise.id}");

        if (!await novo_user(cont, "pedro_sigma", "wow.com@hudson", "12344", 1200)) goto deu_ruim;
        Console.WriteLine("pedro_sigma criado");

        if (!await nova_partida(cont, "pedro", "pedro_sigma", "abc", 50000)) goto deu_ruim;
        Console.WriteLine($"Criada partida entre pedro e pedro_sigma");


        await novo_user(cont, "almeida", "al@hudson", "12344", 950);
        await novo_user(cont, "roberto", "bert@hudson", "12344", 5500);
        await novo_user(cont, "hudson", "maxmilneclimb@hudson", "12344", 5600);
        await novo_user(cont, "daniel", "janjagarnbret@hudson", "12344", 2200);
        await novo_user(cont, "joao", "aimori@hudson", "12344", 2256);


        Environment.Exit(0);


        deu_ruim:
            Console.WriteLine("Deu Ruim");
            Environment.Exit(1);
    }
    static async Task popular(AppDbContext cont)
    {
        await novo_user(cont, "patrick", "pátrique@mail.com", "12344", 720, "https://i.pinimg.com/736x/1e/c3/07/1ec3073a7f9433acd95f51de723ccdcc.jpg");
        await novo_user(cont, "almeida", "aalmida@mail.net", "12344", 950, "https://i.pinimg.com/736x/09/c5/bf/09c5bfc56e48d3fa98504bfc5ab9b91a.jpg");
        await novo_user(cont, "luisa", "luisa@mail.net", "12344", 950, "https://i.pinimg.com/736x/cb/92/ab/cb92ab3f88adc7b3c282e6b5805eadf5.jpg");

        await novo_user(cont, "brick", "mailmailmail@mail.com", "12344", 1000, "https://i.pinimg.com/736x/a7/2d/f5/a72df5a0a11b832c7cb119409f20571d.jpg");
        await novo_user(cont, "dewey", "godisdead@com", "12344", 1050, "https://i.pinimg.com/1200x/d7/b9/6e/d7b96ea8b0b636b1c88f45ded834a0f9.jpg");

        await novo_user(cont, "roberto", "bert@hudson.br", "12344", 1240, "https://i.pinimg.com/736x/61/a9/80/61a98089402169b84bffda769d863350.jpg");
        await novo_user(cont, "hudson", "hudson@roberto.br", "12344", 1333, "https://i.pinimg.com/736x/4a/a9/c0/4aa9c0c6b49a6a42f91ee5ae05f3f068.jpg");

        await novo_user(cont, "danielgamer", "daniel@hudson.al", "12344", 1430, "https://i.pinimg.com/736x/65/a3/97/65a3975f2a92b115aa410308621c0397.jpg");
        await novo_user(cont, "9394123aab", "wtf@hudson.al", "12344", 1455, "https://i.pinimg.com/736x/69/d4/f5/69d4f553a801270cc080e78402855353.jpg");
        await novo_user(cont, "beserk01", "bala@hudson.rs", "12344", 1499, "https://i.pinimg.com/736x/1d/73/31/1d73315d09d1091436af1f0d6d324263.jpg");

        await novo_user(cont, "jokler67", "ysosirius@yahoo.com", "12344", 1540, "https://i.pinimg.com/736x/ca/83/db/ca83db60ab88ef1bce781373fc5b7091.jpg");
        await novo_user(cont, "batman", "batman@notbruce.com", "12344", 1656, "https://i.pinimg.com/736x/fb/3d/33/fb3d33e1c6691f3a673a513c0d25359e.jpg");

        await novo_user(cont, "calasewing", "mewing@sudoku.com", "12344", 1656, "https://i.pinimg.com/1200x/fa/e3/b5/fae3b53400f5a14cd8154fab4b0fb104.jpg");

        await novo_user(cont, "magmidt", "magjuice@nr.no", "12344", 1702, "https://i.pinimg.com/736x/7b/ac/a6/7baca60d448a04cb836c11ba3a3c70e3.jpg");
        await novo_user(cont, "dilm4", "dilma@br.com", "12344", 1700, "https://i.pinimg.com/1200x/ea/7d/d0/ea7dd0981e37e5591843741ed4841e75.jpg");
        await novo_user(cont, "lulabr", "lula@br.com", "12344", 1713, "https://i.pinimg.com/736x/70/bd/f6/70bdf673699aa248a58ffc477681b74a.jpg");
        await novo_user(cont, "jairbolso", "bolsonaro@br.com", "12344", 1722, "https://i.pinimg.com/736x/8e/6a/9f/8e6a9f50db3bd00cc0afacec50c06fdf.jpg");
        await novo_user(cont, "mikejackson", "star@mj.mj", "12344", 1750, "https://i.pinimg.com/736x/80/83/f5/8083f5ec759ab9b7ad8aee1315f3050b.jpg");
        await novo_user(cont, "lizzy", "elizabeth@uk.com", "12344", 1755, "https://i.pinimg.com/736x/5c/66/78/5c66785fecf0c3bf194b51fe232efe4a.jpg");

        await novo_user(cont, "noob", "noob@sudoku", "12344", 1760, "https://i.pinimg.com/736x/6f/3d/53/6f3d53893a661c8dbc3644cce32ab4b2.jpg");
        await novo_user(cont, "pro", "pro@sudoku", "12344", 1890, "https://i.pinimg.com/736x/63/a4/cf/63a4cfaf18b0355627d153fe6448efe6.jpg");
        await novo_user(cont, "hacker", "hacker@sudoku", "12344", 1930, "https://i.pinimg.com/736x/cf/ae/5f/cfae5f1c38a5708e07cf2c811934b284.jpg");
        await novo_user(cont, "god", "god@sudoku", "12344", 2001, "https://i.pinimg.com/1200x/ce/ef/58/ceef58c2558bacf00783d1c0651990e2.jpg");
        await novo_user(cont, "creator", "creator@sudoku", "12344", 2130, "https://i.pinimg.com/736x/9b/cc/dd/9bccddd970660e8b93da9287261a72d8.jpg");

        await novo_user(cont, "light", "lightyagami@sudoku.ja", "12344", 2230, "https://i.pinimg.com/736x/f4/05/2c/f4052cf2abffba49baa7cc731bfd8f3d.jpg");
        await novo_user(cont, "L", "l@sudoku.ja", "12344", 2250, "https://i.pinimg.com/736x/bb/88/6d/bb886d9a81f0dc8e42a287e483523d15.jpg");
        await novo_user(cont, "cebolinha", "donodarua@sudoku.ja", "12344", 2250, "https://i.pinimg.com/736x/6b/36/d3/6b36d334b79b27e2e325aa3ed1aeb9f5.jpg");

        await novo_user(cont, "mariasudoku", "maria@sudoku.br", "12344", 2330, "https://i.pinimg.com/1200x/a9/18/66/a918661ede1744aad04243a1fb36922e.jpg");
        await novo_user(cont, "joaosudoku", "joao@sudoku.br", "12344", 2330, "https://i.pinimg.com/1200x/6b/09/83/6b098352d0b01f6f4d41aedc3207350e.jpg");

        Environment.Exit(0);
    }





    static async Task Main(string[] args)
    {
        Console.WriteLine("==============");
        Console.WriteLine("BANCO DE DADOS ");
        Console.WriteLine("==============");



        

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDbContext<AppDbContext>();
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var cont = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!cont.Database.CanConnect())
            {
                Console.WriteLine("sem conexao banco");
                Environment.Exit(1);
            }

            if (args.Length > 0 && args[0] == "teste") await testar(cont);
            if (args.Length > 0 && args[0] == "pop") await popular(cont);
        }

        app.UseHttpsRedirection();

        
        



        app.MapPut("/fimpartida", async (AppDbContext cont, fim_partida fp) =>
        {   
            if (!fp.abandonou) await updt_user_melhor_tempo(cont, fp.ganhador, fp.duracao_ms);
            await trocar_user_elo(cont, fp.ganhador, fp.elo_diff_ganhador);
            await trocar_user_elo(cont, fp.perdedor, fp.elo_diff_perdedor);
            await nova_partida(cont, fp.ganhador, fp.perdedor, fp.tabuleiros, fp.duracao_ms);

            return Results.Ok("ok!");
        });

        app.MapDelete("/deletar/{nome}", async (AppDbContext cont, string nome) =>
        {
            await deletar_user(cont, nome);
            return Results.Ok("deletado");
        });
            
        app.MapPut("/trocardados/", async (AppDbContext cont, change_user_info cui) => {
            bool r = await trocar_user_dados(cont, cui);

            if (r) return Results.Ok("trocado");

            return Results.Unauthorized();
        });

        app.MapGet("/stats/{nome}", async (AppDbContext cont, string nome) =>
        {
            Usuario? analise = await find_user(cont, nome);
            if (analise == null)
            {
                return Results.NotFound("Coagulo nao existe");
            }
            if (analise.Stats == null)
            {
                return Results.NotFound("Coagulo nao tem stats?");
            }

            int g_pos = cont.sudoku_stats.Count(s => s.user_elo > analise.Stats.user_elo) + 1;



            return Results.Ok( new user_stats(
                analise.id,
                analise.email!,
                analise.foto_link,

                analise.Stats.user_elo,
                analise.Stats.qtd_jogos_ganhos,
                analise.Stats.qtd_jogos_jogados,
                analise.Stats.melhor_tempo,

                g_pos
            ));
        });

        app.MapGet("/find/{nome}", async (AppDbContext cont, string nome) =>
        {
            Usuario? analise = await find_user(cont, nome);
            
            if (analise == null|| analise.senha_hash==null||analise.senha_salt ==null ||analise.nome==null||analise.email==null)
            {
                return Results.NotFound("Coagulo nao existe");
            }

            string _senhaHash = Convert.ToBase64String(analise.senha_hash);
            string _senhaSalt = Convert.ToBase64String(analise.senha_salt);


            return Results.Ok(new user_private_info(analise.nome, analise.email, _senhaHash, _senhaSalt));
        });

        app.MapGet("/existe/{nome}", async (AppDbContext cont, string nome) =>
        {
            Usuario? analise = await find_user(cont, nome);
            
            if (analise == null) return Results.Ok(0);
            return Results.Ok(1);
        });
        
        app.MapPost("/cadastrar", async (AppDbContext cont, Cadastro cadastro) =>
        {
            try
            {
                Usuario? us = await userDoEmail(cont, cadastro.email);
                if(us != null)
                {
                    return Results.Conflict("nnao criado");
                }

                await novo_user(cont, cadastro.nome, cadastro.email, cadastro.senha);
                return Results.Ok(new user_private_info(cadastro.nome, cadastro.email, "", ""));
            }
            catch
            {
                return Results.Conflict("nnao criado");
            }
        });

        app.MapGet("/leaderboard", async (AppDbContext cont) =>
        {
            (string nome, int elo, string foto)[]? res = await leaderboard(cont);
            if (res == null)
            {
                return Results.NoContent();
            }
 
            return Results.Ok(res.Select(u => new {u.nome, u.elo, u.foto}));
        });
        app.Run();
    }

}

public record Cadastro(string nome, string email, string senha);