

if(localStorage.getItem("tok")!==null){

    const sock = new WebSocket(`ws://${host}:5015/ws/${localStorage.getItem("tok")}`); 

    sock.onopen = () => {
        input.value = "jogar"
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
            document.getElementById("playground").style.display="none";
            document.getElementById("posjogo").style.display = "flex";
            ganhador("19");
        }  
            
        if(message.startsWith("perdeu:")){
            document.getElementById("playground").style.display="none";
            document.getElementById("posjogo").style.display = "flex";
            perdedor("19");
        }

        if(message.startsWith("procurando por oponente...")){
            document.getElementById("posjogo").style.display = "none";
            document.getElementById("searching").style.display = "block";
        }

        resposta.textContent = message;
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
        document.getElementById("body").style.backgroundColor = "#f0f2f5";
        document.getElementById("victory").style.display = "none";
        document.getElementById("defeat").style.display = "none";
    });

    function jogando(sudoku){

        for(let i=1,  j=8; j<sudoku.length;i++, j++){
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