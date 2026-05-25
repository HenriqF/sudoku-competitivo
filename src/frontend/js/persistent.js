// const host = "26.156.144.128";
// const host = "localhost";

function enviar(){
    localStorage.setItem("host", document.getElementById("ip").value);
    document.getElementById("login").click();
}

function online(){
    document.getElementById("online").href = `https://${document.getElementById("ip").value}:7185/online`;
    document.getElementById("online").click();
}

const host = localStorage.getItem("host");