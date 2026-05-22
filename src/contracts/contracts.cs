
using System.Text.Json.Serialization;
namespace contracts;

public record new_sudokus(string[] boards);

public record api_message(string message);

public record user_stats(
    int id,
    string email,
    string? foto_link,

    int elo,
    int vitorias,
    int partidas,
    int melhor_tempo,

    int pos_global
);


public record change_user_info(
    string email,
    string nome,
    string senha,
    string foto
);


public record user_private_info ( 
    string nome,
    string email,
    string hash,
    string salt
);

public record player_elo_rel (
    string nome,
    int elo,
    string foto
);


public record fim_partida (
    string ganhador,
    string perdedor,
    string tabuleiros,

    int elo_diff_ganhador,
    int elo_diff_perdedor,

    int duracao_ms,
    bool abandonou
);