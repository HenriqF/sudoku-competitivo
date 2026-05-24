const userLogin = document.getElementById("username");
const senhaLogin = document.getElementById("password");
const mensagem = document.getElementById("message");

const click_sound = new Audio("sons/click.mp3");
const algo_errado = new Audio("sons/algoerrado.mp3");

click_sound.play();

function algoERRADO(){
    click_sound.pause();
    click_sound.currentTime = 0;
    algo_errado.pause();
    algo_errado.currentTime = 0;
    algo_errado.play();
}

function mostrar_senha(){
    senhaLogin.type = "text";
};

function esconder_senha(){
    senhaLogin.type = "password";
};

function trocar(){
    document.getElementById("cadastro").click();
};

async function fazerLogin(){

    if(!(await  validarLogin())){
        return;
    }
    
    try {

        const response = await fetch(`http://${host}:5269/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ "nome": userLogin.value, "senha": senhaLogin.value })
        });

        const data = await response.json();

        if(data==="CREDINV"){
            algoERRADO()
            mensagem.innerText = "Credenciais Inválidas!";
            return;
        }
        
        localStorage.setItem("token", data);
        localStorage.setItem("user", userLogin.value);
        document.getElementById("menu").click();

    } catch (error) {
        algoERRADO()
        mensagem.innerText = "Erro ao conectar com o servidor.";
    }
    
};

async function validarLogin(){
    let regex = /^[a-zA-Z0-9]*$/;

    if(userLogin.value == "" || senhaLogin.value == ""){
        algoERRADO()
        mensagem.innerText = "Campo em Branco!";
        return false;
    }

    if(!regex.test(userLogin.value)||!regex.test(senhaLogin.value)){
        algoERRADO()
        mensagem.innerText = "Somente letras e números são permitidos!";
        return false;
    }

    if(!(await buscarNome())){
        algoERRADO()
        mensagem.innerText = "Credenciais Inválidas!";
        return false;
    }
    return true;
}

async function buscarNome() {
    try{
        const response = await fetch(`https://${host}:7185/existe/${userLogin.value}`);

        const data = await response.json();
        console.log(data)
        if(data==1) return true;
        
        return false;

    } catch(e){
        console.log("erro ao verificar..."+ e)
        msg.innerText = "Erro ao verificar disponibilidade do nome...";
        return true;
    }
    
}