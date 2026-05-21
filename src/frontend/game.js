

if(localStorage.getItem("tok")!==null){

    const sock = new WebSocket(`ws://${host}:5015/ws/${localStorage.getItem("tok")}`); 

    sock.onopen = () => {
        sock.send("jogar");
    }; 

    sock.onmessage = (event) => {
        let message = event.data.toString();

        if(message.startsWith("sudoku:")){
            document.getElementById("searching").style.display = "none";
            document.getElementById("posjogo").style.display = "none";
            document.getElementById("playground").style.display = "flex";
            jogando(message);
        }

        if(message.startsWith("ganhou:")){
            timer();
            document.getElementById("playground").style.display="none";
            document.getElementById("posjogo").style.display = "flex";
            ganhador(message.substring(8));
        }  
            
        if(message.startsWith("perdeu:")){
            timer();
            document.getElementById("playground").style.display="none";
            document.getElementById("posjogo").style.display = "flex";
            perdedor(message.substring(8));
        }

        if(message.startsWith("procurando por oponente...")){
            document.getElementById("posjogo").style.display = "none";
            document.getElementById("searching").style.display = "block";
        }

        resposta.textContent = message;
        console.log(message);
    };

    sock.onerror = (erro) => {
        console.error("erro");
    };

    sock.onclose = () => {
        setTimeout(ligar, 200);
    };



    const botao = document.getElementById("butao");
    const input = document.getElementById("input");
    const resposta = document.getElementById("resposta");

    botao.addEventListener("click", () => {
        sock.send(input.value);
    });

    function jogando(sudoku){
        timer();
        for(let i=1, j=8; j<sudoku.length;i++, j++){
            if(sudoku[j]!='_'){
                document.getElementById(`${i}`).value = sudoku[j]
                document.getElementById(`${i}`).disabled = true
            }
            else{
                document.getElementById(`${i}`).value = ""
                document.getElementById(`${i}`).disabled = false
            }
        }
    }

    function finalizar(){
        let out = ""
        for(let i=1; i<37;i++){
            out += `${document.getElementById(`${i}`).value}`
        }
        sock.send(out)
    }

    function jogarDnv(){sock.send("jogar")}
}

let timerInterval;
let isRunning = false;
let csecs;
let secs;
let min;

function timer() {
    
    if (isRunning) {
        clearInterval(timerInterval);
        isRunning = false;
        return;
    }

    isRunning = true;

    const startTime = Date.now();
    const timerElement = document.getElementById("timer");

    timerInterval = setInterval(() => {
        const totalCsecs = Math.floor((Date.now() - startTime) / 10);
        
        min = Math.floor(totalCsecs / 6000);
        secs = Math.floor((totalCsecs % 6000) / 100);
        csecs = totalCsecs % 100; 

        const displayS = secs < 10 ? '0' + secs : secs;
        const displayCS = csecs < 10 ? '0' + csecs : csecs;

        
        if (min === 0) {
            timerElement.textContent = `${displayS}.${displayCS}`;
        } else {
            timerElement.textContent = `${min}:${displayS}.${displayCS}`;
        }
    }, 10); // 1 centisegundo
}

const msg_posjogo = document.getElementById("vic-der");
const pontos = document.getElementById("pdls");

function ganhador(lucro){
    Carregardados();
    msg_posjogo.textContent = "VITÓRIA";
    pontos.style.color = "#7dda75";
    pontos.textContent = `+${lucro} de elo..`;
}

function perdedor(preju){
    Carregardados();
    msg_posjogo.textContent = "DERROTA";
    pontos.style.color = "#eb6a6a";
    pontos.textContent = `${preju} de elo..`;
}

const infoNome = document.getElementById("nome");
const infoEmail = document.getElementById("email");
const infoFoto = document.getElementById("foto");
const infoPos = document.getElementById("pos");
const infoElo = document.getElementById("elo");
const infoWins = document.getElementById("wins");
const infoDefeats = document.getElementById("defeats");
const infoMelhorTempo = document.getElementById("besttime");

async function Carregardados(){
    try {
        let nome = localStorage.getItem("user")

        const response = await fetch(`https://${host}:7185/stats/${nome}`);

        const data = await response.json();

        infoNome.textContent = localStorage.getItem("user");
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

perdedor(20);

function menu() {document.getElementById("menu").click();}
