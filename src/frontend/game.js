const click_sound = new Audio("sons/click.mp3");
const strike1 = new Audio("sons/strike1.mp3");
const strike2 = new Audio("sons/strike2.mp3");
const perdeu = new Audio("sons/morte.mp3");
const ganhou = new Audio("sons/victory.mp3");
const comecou = new Audio("sons/countdown.mp3")

if(localStorage.getItem("tok")!==null){

    const sock = new WebSocket(`ws://${host}:5015/ws/${localStorage.getItem("tok")}`); 

    sock.onopen = () => {
        sock.send("jogar");
    }; 

    sock.onmessage = (event) => {
        let message = event.data.toString();

        if(message.startsWith("timer")){
            document.getElementById("searching").style.display = "none";
            document.getElementById("posjogo").style.display = "none";
            document.getElementById("playground").style.display = "flex";
            document.getElementById("sus").style.display = "none";
            countdown();
            comecou.pause();
            comecou.currentTime = 0;
            comecou.play();

        }

        if(message.startsWith("sudoku:")){
            document.getElementById("sus").style.display = "block";
            let match_info = message.split(' ');
            jogando(match_info[1], match_info[2]);

        }

        if(message.startsWith("opp: ")){
            let op_info = message.split(' ');
            document.getElementById("nome2").textContent = op_info[1];
            document.getElementById("email2").textContent = `Elo: ${op_info[2]}`;
            document.getElementById("foto2").src = op_info[3];
        }

        if(message.startsWith("strike: ")){
            if(message.substring(8)==1){strike1.play()}
            if(message.substring(8)==2){
                strike1.pause();
                strike1.currentTime = 0;
                strike2.play();
            }
            shake(message.substring(8));
        }

        if(message.startsWith("ganhou:")){
            timer();
            ganhou.pause();
            ganhou.currentTime = 0;
            ganhou.play();
            document.getElementById("playground").style.display="none";
            document.getElementById("posjogo").style.display = "flex";
            let pos_info = message.split(' ');
            nome = pos_info[2];
            ganhador(pos_info[1]);
            document.getElementById("tempo").textContent = csTOtimer(pos_info[3].substring(0,pos_info[3].length-1));
        }  

        if(message.startsWith("perdeu:")){
            timer();
            strike2.pause();
            strike2.currentTime = 0;
            perdeu.pause();
            perdeu.currentTime = 0;
            perdeu.play();
            document.getElementById("playground").style.display="none";
            document.getElementById("posjogo").style.display = "flex";
            let pos_info = message.split(' ');
            nome = pos_info[2];
            perdedor(pos_info[1]);
            document.getElementById("tempo").textContent = csTOtimer(pos_info[3].substring(0,pos_info[3].length-1));
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

    function jogando(sudoku ,tempo_passado){
        timer(tempo_passado);
        for(let i=1, j=0; j<sudoku.length;i++, j++){
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
        let out = "";

        for(let i=1; i<37;i++){
            out += `${document.getElementById(`${i}`).value}`;
        }

        sock.send(out);
    }

    function jogarDnv(){
        sock.send("jogar");
        click_sound.pause();
        click_sound.currentTime = 0;
        click_sound.play();
    }

    function desistir(){
        sock.send("abandonar")
        click_sound.pause();
        click_sound.currentTime = 0;
        click_sound.play();
    }
}

let timerInterval;
let isRunning = false;
let totalCsecs;

function timer(tempo_passado) {
    
    if (isRunning) {
        clearInterval(timerInterval);
        isRunning = false;
        return;
    }

    isRunning = true;

    const startTime = Date.now() - tempo_passado;
    const timerElement = document.getElementById("timer");

    timerInterval = setInterval(() => {
        totalCsecs = Math.floor((Date.now() - startTime) / 10);
        
        let min = Math.floor(totalCsecs / 6000);
        let secs = Math.floor((totalCsecs % 6000) / 100);
        let csecs = totalCsecs % 100; 

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
    pontos.textContent = `+${lucro} de elo`;
}

function perdedor(preju){
    Carregardados();
    msg_posjogo.textContent = "DERROTA";
    pontos.style.color = "#eb6a6a";
    pontos.textContent = `${preju} de elo`;
}

function csTOtimer(cs){

    let min = Math.floor(cs / 6000);
    let secs = Math.floor((cs % 6000) / 100);
    let csecs = cs % 100;

    const displayS = secs < 10 ? '0' + secs : secs;
    const displayCS = csecs < 10 ? '0' + csecs : csecs;

    if (min === 0) return(`${displayS}.${displayCS}`);
        
    return (`${min}:${displayS}.${displayCS}`);
}


const infoNome = document.getElementById("nome");
const infoEmail = document.getElementById("email");
const infoFoto = document.getElementById("foto");
const infoPos = document.getElementById("pos");
const infoElo = document.getElementById("elo");
const infoWins = document.getElementById("wins");
const infoDefeats = document.getElementById("defeats");
const infoMelhorTempo = document.getElementById("besttime");

let nome;

async function Carregardados(){
    try {
        const response = await fetch(`https://${host}:7185/stats/${nome}`);

        const data = await response.json();

        infoNome.textContent = nome;
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

perdedor(20);

function menu() {
    document.getElementById("menu").click();
    click_sound.pause();
    click_sound.currentTime = 0;
    click_sound.play();
}

let txtStrikes;

function shake(erros){
    document.getElementById("sus").classList.add("shake");
    document.getElementById("strikes").style.color = "red";

    document.getElementById("strikes").textContent = `${erros}/3 erros`

    setTimeout(() => {
        document.getElementById("sus").classList.remove("shake");
    }, 100);
    
    clearTimeout(txtStrikes);

    txtStrikes = setTimeout(() => {
        document.getElementById("strikes").style.color = "white";
    }, 5000);
}

function countdown(){
    let num = document.getElementById("countdown");

    num.textContent = 3;
    num.style.display = "block";
    num.classList.add("remix3");

    setTimeout(() => {
       num.classList.remove("remix3");
       num.textContent = 2;
       num.classList.add("remix2");
       setTimeout(() => {
            num.classList.remove("remix2");
            num.textContent = 1;
            num.classList.add("remix1");
            setTimeout(() => {
                num.style.display = "none";
                num.classList.remove("remix1");
            }, 1000);
        }, 1000);
    }, 1000);

    
}