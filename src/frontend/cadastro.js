const userCadastro = document.getElementById("username");
const emailCadastro = document.getElementById("email");
const senhaCadastro = document.getElementById("password");
const senhaConfirm = document.getElementById("password2");
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
    senhaCadastro.type = "text";
    senhaConfirm.type = "text";
};

function esconder_senha(){
    senhaCadastro.type = "password";
    senhaConfirm.type = "password";
};


function trocar(){
    document.getElementById("login").click();
};


async function fazerSignup(){

    if(!validarCadastro()){
        return;
    }

    try {
        var email = emailCadastro.value.toLowerCase();

        const response = await fetch(`http://${host}:5269/cadastro`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ "nome": userCadastro.value, "email": email, "senha": senhaCadastro.value })
        });

        if (!response.ok){
            algoERRADO();
            mensagem.innerText = (await response.text()).slice(1, -1);
            return;
        }
        
        const data  = await response.json();
        
        mensagem.style.color = "green";
        mensagem.innerText = `seja bem vindo, ${data["nome"]}`;
        fazerLogin();

    } catch (error) {
        algoERRADO();
        mensagem.innerText = "Erro";
    }
}

function validarCadastro(){
    let regex = /^[a-zA-Z0-9]*$/;
    let regex2 = /^[a-zA-Z0-9@._/-/+]*$/;
    let regex3 = /^[a-zA-Z0-9_*#]*$/;

    if(userCadastro.value == "" || emailCadastro.value == "" || senhaCadastro.value == "" || senhaConfirm.value == ""){
        algoERRADO();
        mensagem.innerText = "Campo em Branco!";
        return false;
    }

    if(!regex.test(userCadastro.value)||!regex2.test(emailCadastro.value)||!regex3.test(senhaCadastro.value)){
        algoERRADO();
        mensagem.innerText = "Caractere inválido!";
        return false;
    }

    if(senhaCadastro.value.length > 32){
        algoERRADO();
        mensagem.innerText = "Senha muito longa! (limite 32 caracteres)";
        return false;
    }

    if(emailCadastro.value.length >254){
        algoERRADO();
        mensagem.innerText = "Email muito longa! (limite 254 caracteres)";
        return false;
    }

    if((userCadastro.value).length > 13){
        algoERRADO();
        mensagem.innerText = "Usuário muito longo! (limite de 13 caracteres)";
        return false;
    }

    if(senhaCadastro.value != senhaConfirm.value){
        algoERRADO();
        mensagem.innerText = "Senhas Diferentes!";
        return false;
    }
    return true;
}

async function fazerLogin(){
    try {

        const response = await fetch(`http://${host}:5269/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ "nome": userCadastro.value, "senha": senhaCadastro.value })
        });

        const data = await response.json();

        if(data==="CREDINV"){
            algoERRADO();
            mensagem.innerText = "Credenciais Inválidas!";
            return;
        }

        localStorage.setItem("token", data);
        localStorage.setItem("user", userCadastro.value);
        document.getElementById("menu").click();

    } catch (error) {
        algoERRADO();
        mensagem.innerText = "Erro ao conectar com o servidor.";
    }
    
};