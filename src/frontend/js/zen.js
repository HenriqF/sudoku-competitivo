let sudoku_incompleto;
let sudoku_completo;
let sudokus_feitos = 0;

let erros;
let strikes= 0;

const strike1 = new Audio("sons/strike1.mp3");
strike1.preload = 'auto';
const strike2 = new Audio("sons/strike2.mp3");
strike2.preload = 'auto';

const ganhou = new Audio("sons/victory.mp3");
ganhou.preload = 'auto';
const click_sound = new Audio("sons/click.mp3");
click_sound.preload = 'auto';


async function jogar(){
    click_sound.play();

    document.getElementById("sudoku").style.display = "block";
    document.getElementById("posgame").style.display = "none";

    try {
        const response = await fetch(`https://${host}:7185/zen`);

        const data = await response.json();

        sudoku_incompleto = data.substring(0, 36);
        sudoku_completo = data.substring(36);

        

        for(let i=1, j=0; j<36;i++, j++){
            if(sudoku_incompleto[j]!='_'){
                document.getElementById(`${i}`).value = sudoku_incompleto[j]
                document.getElementById(`${i}`).disabled = true
            }
            else{
                document.getElementById(`${i}`).value = ""
                document.getElementById(`${i}`).disabled = false
            }
        }

    } catch{
        console.log("erro ao conectar")
    }
}

function finalizar(){
    let out = "";

    for(let i=1; i<37;i++){
        out += `${document.getElementById(`${i}`).value}`;
    }

    if(out===sudoku_completo){
        sudokus_feitos += 1;
        strikes = 0;
        ganhou.pause();
        ganhou.currentTime = 0;
        ganhou.play();
        document.getElementById("sudoku").style.display = "none";
        document.getElementById("posgame").style.display = "flex";
        document.getElementById("feitos").textContent = `Sudokus completos: ${sudokus_feitos}`;
        return;
    }

    document.getElementById("sus").classList.add("shake");

    setTimeout(() => {
        document.getElementById("sus").classList.remove("shake");
    }, 100);

    strikes += 1;

    if(strikes%2===0){
        strike2.pause();
        strike2.currentTime = 0;
        strike2.play();
    }else{
        strike1.pause();
        strike1.currentTime = 0;
        strike1.play();
    }

    document.getElementById("strikes").textContent = `${strikes} erros`;
    document.getElementById("strikes").style.color = "red";

    clearTimeout(erros);

    erros = setTimeout(()=>{
        document.getElementById("strikes").style.color = "white";
    },5000);
}

function menu(){document.getElementById("menu").click()}

jogar()