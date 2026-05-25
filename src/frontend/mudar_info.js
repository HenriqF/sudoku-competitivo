//imutaveis
var nome_user;
var foto_user;
var jwt_token;
var email;

const click_sound = new Audio("sons/click.mp3");
const algo_errado = new Audio("sons/algoerrado.mp3");

click_sound.play();

const infoNome = document.getElementById("nome");
const infoEmail = document.getElementById("user_email");
const infoFoto = document.getElementById("foto");
const msg = document.getElementById("message");
const sen1 = document.getElementById("password");
const sen2 = document.getElementById("password2");
const sen3 = document.getElementById("password3");

//mutaveis
var novo_foto;
var novo_nome;
var novo_senha;

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

async function Carregardados(){
    try {
        let nome = localStorage.getItem("user")

        const response = await fetch(`https://${host}:7185/stats/${nome}`);

        const data = await response.json();

        foto_user = data.foto_link;

        document.getElementById("username").value = nome_user;

        document.getElementById("user_foto").value = foto_user;   
        infoFoto.src = foto_user;

        infoEmail.textContent = data.email;

    } catch (error) {
        infoNome.textContent = "dois";
        return;
    }
    
};

function change(x){
    click_sound.pause();
    click_sound.currentTime = 0;
    click_sound.play();

    if(x==1){
        novo_nome = document.getElementById("username").value;
        infoNome.textContent = novo_nome;
    }
    if(x==2){
        novo_foto = document.getElementById("user_foto").value;
        infoFoto.src = novo_foto;
    }
    if(x==3){
        if(!document.getElementById("check_nome").checked){
            document.getElementById("username").style.display = "none";
            infoNome.textContent = nome_user;
            document.getElementById("username").value = nome_user;
            novo_nome = nome_user;
        }else document.getElementById("username").style.display = "block";

        if(!document.getElementById("check_foto").checked){
            document.getElementById("user_foto").style.display = "none";
            infoFoto.src = foto_user;
            document.getElementById("user_foto").value = foto_user;
            novo_foto = foto_user;
        }else document.getElementById("user_foto").style.display = "block";

        if(!document.getElementById("check_senha").checked){
            sen1.style.display = "none";
            sen2.style.display = "none";
        }else{
            sen1.style.display = "block";
            sen2.style.display = "block";
        }
    }
}
function algoERRADO(){
    click_sound.pause();
    click_sound.currentTime = 0;
    algo_errado.pause();
    algo_errado.currentTime = 0;
    algo_errado.play();
}
async function validarDados(){

    if(document.getElementById("check_nome").checked && !(await validarNome())) return;
    if(document.getElementById("check_foto").checked && !validarFoto()) return;
    if(document.getElementById("check_senha").checked && !validarSenha()) return;
    if(!document.getElementById("check_senha").checked && !document.getElementById("check_foto").checked && !document.getElementById("check_nome").checked){
        algoERRADO();
        msg.innerText = "Palhaço...";
        return;
        }
    if(sen3.value===""){
        algoERRADO();
        msg.innerText = "Confirme com sua senha!";
        return;
    }
    if(!(await senhaConfirmacao())) return;

    mudarDados();
}

async function validarNome(){
    let regex = /^[a-zA-Z0-9]*$/;

    novo_nome = document.getElementById("username").value;

    if(!regex.test(novo_nome)){
        algoERRADO();
        msg.innerText = "Somente letras e números são permitidos!";
        return false;
    }

    if(novo_nome.length > 13){
        algoERRADO();
        msg.innerText = "Nome muito longo! (limite de 13 caracteres)";
        return false;
    }

    if(novo_nome!=nome_user && await buscarNome(novo_nome)){
        algoERRADO();
        msg.innerText = "Nome já em uso! =C";
        return false;
    }
    return true;
}

async function buscarNome(nome_buscado) {
    try{
        const response = await fetch(`https://${host}:7185/existe/${nome_buscado}`);

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

function validarFoto(){
    novo_foto = document.getElementById("user_foto").value;

    if(!novo_foto.startsWith("https://i.pinimg.com/")){
        algoERRADO();
        msg.innerText = "URL deve começar com https://i.pinimg.com/";
        return false;
    }
    return true;
}

function validarSenha(){
    let regex = /^[a-zA-Z0-9_*#]*$/;

    if(sen1.value !=sen2.value){
        algoERRADO();
        msg.innerText = "Senhas Diferentes!";
        return false;
    }

    if(sen1.value.length>32){
        algoERRADO();
        msg.innerText = "Senha muito longa! (limite 32 caracteres)";
        return false;
    }
    
    if(!regex.test(sen1.value)){
        algoERRADO();
        msg.innerText = "A senha permite apenas letras, números, { _ * #}";
        return false;
    }
    return true;
}

async function mudarDados() {
    if(document.getElementById("check_nome").checked){
        novo_nome = document.getElementById("username").value;
    }else novo_nome = "";

    if(document.getElementById("check_foto").checked){
        novo_foto = document.getElementById("user_foto").value;
    }else novo_foto = "";

    if(document.getElementById("check_senha").checked){
        novo_senha = sen1.value;
    }else novo_senha = "";
    
    try{

        const response = await fetch(`https://${host}:7185/mudarinfo`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${jwt_token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                "email" : infoEmail.textContent,
                "nome" : novo_nome,
                "foto" : novo_foto,
                "senha" : novo_senha
                })
            });

        if (!response.ok) {
            algoERRADO();
            msg.innerText = `Erro ${response.status}`;
            return;
        }

        const data = await response.json();

        msg.style.color = "green";
        msg.innerText = `Dados atualizados com sucesso! ${data}` ;

        fazerLogin();

    } catch{
        algoERRADO();
        msg.innerText = "Erro ao Enviar Dados";
    }
}

async function fazerLogin(){
    if(novo_senha==="")novo_senha = sen3.value;
    if(novo_nome==="")novo_nome = nome_user;
    
    try {

        const response = await fetch(`https://${host}:7185/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ "nome": novo_nome, "senha": novo_senha })
        });

        const data = await response.json();

        if(data==="CREDINV"){
            algoERRADO();
            msg.innerText = "Credenciais Inválidas!";
            return;
        }
        
        localStorage.setItem("token", data);
        localStorage.setItem("user", novo_nome);
        document.getElementById("menu").click();

    } catch {
        algoERRADO();
        msg.innerText = "Erro ao conectar com o servidor.";
    }
    
};

async function senhaConfirmacao(){
    
    try {

        const response = await fetch(`https://${host}:7185/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ "nome": nome_user, "senha": sen3.value })
        });

        const data = await response.json();

        if(data==="CREDINV"){
            algoERRADO();
            msg.innerText = "Senha ERRADA!";
            return false;
        }
        
        return true;

    } catch(e) {
        algoERRADO();
        msg.innerText = "Erro ao conectar com o servidor." + e;
        return false;
    }
    
};

const sen4 = document.getElementById("password4");

async function deletar(){

    try {
        const response = await fetch(`https://${host}:7185/deletarconta`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ "nome": nome_user, "senha": sen4.value })
        });

        const data = await response.json();

        console.log(data);

        if(data==="CREDINV"){
            algoERRADO();
            document.getElementById("message2").innerText = "Senha ERRADA!";
            return;
        }

        if (!response.ok) {
            algoERRADO();
            document.getElementById("message2").innerText = `Erro ${response.status}`;
            return;
        }

        localStorage.clear();
        document.getElementById("login").click();


    } catch(e) {
        algoERRADO();
        document.getElementById("message2").innerText = "Erro ao conectar com o servidor." + e;
    }
}

function vira(){
    click_sound.pause();
    click_sound.currentTime = 0;
    click_sound.play();

    if(document.getElementById("dados").style.display === "block"){
        document.getElementById("trocar").textContent = "mudar dados";
        document.getElementById("deletarconta").style.display = "block";
        document.getElementById("dados").style.display = "none";
        document.getElementById("trocar").classList.remove("delet");
        document.getElementById("trocar").classList.add("muddados");
        return;
    }
    document.getElementById("trocar").textContent = "deletar conta";
    document.getElementById("deletarconta").style.display = "none";
    document.getElementById("dados").style.display = "block";
    document.getElementById("trocar").classList.remove("muddados");
    document.getElementById("trocar").classList.add("delet");

}
function mostrar_senha(){
    document.getElementById("password").type = "text";
    document.getElementById("password2").type = "text";
    document.getElementById("password3").type = "text";
    document.getElementById("password4").type = "text";
};

function esconder_senha(){
    document.getElementById("password").type = "password";
    document.getElementById("password2").type = "password";
    document.getElementById("password3").type = "password";
    document.getElementById("password4").type = "password";
};

function cancelar(){
    document.getElementById("menu").click();
};

Carregardados();