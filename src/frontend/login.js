const userLogin = document.getElementById("username");
const senhaLogin = document.getElementById("password");
const mensagem = document.getElementById("message");

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
            mensagem.innerText = "Credenciais Inválidas!";
            return;
        }
        
        localStorage.setItem("token", data);
        localStorage.setItem("user", userLogin.value);
        document.getElementById("menu").click();

    } catch (error) {
        mensagem.innerText = "Erro ao conectar com o servidor.";
    }
    
};

async function validarLogin(){
    let regex = /^[a-zA-Z0-9]*$/;

    if(userLogin.value == "" || senhaLogin.value == ""){
        mensagem.innerText = "Campo em Branco!";
        return false;
    }

    if(!regex.test(userLogin.value)||!regex.test(senhaLogin.value)){
        mensagem.innerText = "Somente letras e números são permitidos!";
        return false;
    }

    if(!(await buscarNome())){
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