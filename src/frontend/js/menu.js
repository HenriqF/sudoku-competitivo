const click_sound = new Audio("sons/click.mp3");
click_sound.preload = 'auto';

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

click_sound.play();

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
    localStorage.clear();
    document.getElementById("indexhtml").click();
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
                <th scope="row"><p class="lead">#${j}</p></th>
                <td class="leader"><img class="leader_prof" src="${data[i][2]}">
                <p class="lead">${data[i][0]}</p></td>
                <td><p class="lead">${data[i][1]}</p></td>
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
        infoMelhorTempo.textContent = `Melhor tempo: ${csTOtimer(Math.floor(data.melhor_tempo/10))}`;


    } catch (error) {
        infoNome.textContent = "dois";
        return;
    }
    
};

function csTOtimer(cs){

    let min = Math.floor(cs / 6000);
    let secs = Math.floor((cs % 6000) / 100);
    let csecs = cs % 100;

    const displayS = secs < 10 ? '0' + secs : secs;
    const displayCS = csecs < 10 ? '0' + csecs : csecs;

    if (min === 0) return(`${displayS}.${displayCS}`);
        
    return (`${min}:${displayS}.${displayCS}`);
}

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

function zen(){document.getElementById("zen").click()}

leader()