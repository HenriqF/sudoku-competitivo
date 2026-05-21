const infoNome = document.getElementById("nome");
const infoEmail = document.getElementById("email");
const infoFoto = document.getElementById("foto");
const infoPos = document.getElementById("pos");
const infoElo = document.getElementById("elo");
const infoWins = document.getElementById("wins");
const infoDefeats = document.getElementById("defeats");
const infoMelhorTempo = document.getElementById("besttime");

var nome_user;
var jwt_token;

if(localStorage.getItem("token")!==null && tokenValido()){
    nome_user = localStorage.getItem("user");
    infoNome.textContent = localStorage.getItem("user");
    jwt_token = localStorage.getItem("token");
    
}else document.getElementById("login").click();

async function tokenValido() {
    try{
        const response = await fetch(`https://${host}:7185/jwtvalido/${localStorage.getItem("token")}`);

        const data = await response.json();

        if (data==1)return true;

        return false;
    } catch(error){
        return false;
    }
}

function sair(){
    localStorage.clear()
    document.getElementById("login").click();
}

async function leader() {
    try {
        const table = document.getElementById("table");
        const newRow = table.insertRow();

        const response = await fetch(`https://${host}:7185/leaderboard`);

        const data = await response.json();

        for(let i=0, j=1;i<data.length;i++){
            if(i!=0 && data[i][1]!=data[i-1][1]){
                j += 1;

                if(i+1!=j) j = i+1;
            }

            table.innerHTML += `
                <tr>
                <th scope="row">#${j}</th>
                <td>${data[i][0]}</td>
                <td>${data[i][1]}</td>
                </tr>
                `
        }
        Carregardados()

    } catch (error) {
        return
        console.log("ERROOO")
        Carregardados()
    }
}

async function Carregardados(){
    try {
        let nome = localStorage.getItem("user")

        const response = await fetch(`https://${host}:7185/stats/${nome}`);

        const data = await response.json();

        infoEmail.textContent = data.email;
        infoFoto.src = data.foto_link;
        infoPos.textContent = `Rank: ${data.pos_global}`;
        infoElo.textContent = `Elo: ${data.elo}`;
        infoWins.textContent = `Vitórias: ${data.vitorias}`;
        infoDefeats.textContent = `Derrotas: ${data.partidas - data.vitorias}`;
        infoMelhorTempo.textContent = `Melhor tempo: ${data.melhor_tempo}`;


    } catch (error) {
        infoNome.textContent = "dois";
        return;
    }
    
};


async function jogar(){
    const response = await fetch(`https://${host}:7185/jogartoken/${nome_user}`, {
        method: 'GET',
        headers: { 'Authorization': `Bearer ${jwt_token}`}
    });

    if (response.status == 200){
        localStorage.setItem("tok", await response.json());
        document.getElementById("jogo").click();
    }
}

function mudarDados(){
    document.getElementById("mudar_info").click();
};

leader()