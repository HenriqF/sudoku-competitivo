# [Sistema de Sudoku 6x6 Ranqueado](https://marcelomiyazaki.github.io/demo-proj-final/)

Uma plataforma que permite usuários a competirem para ver quem consegue resolver um problema de sudoku 6x6 mais rápido, com sistema de ELO.

<div align="center"><img src="https://sudoku-puzzles.net/wp-content/puzzles/sudoku-6x6/easy/1.png" width="300" height="300"></div>

<br>

## Para jogar

Com os serviços cruciais rodando (todos menos display_api), abra "src/frontent/index.html"<br><br>
Caso seja o computador rodando os serviços, digite "localhost" no campo e aperte o botão `enviar`.<br>


Caso contrário, digite o ip do computador dono dos serviços e aperte o botão `online`. Caso a nova página aberta mostre um alerta de segurança, clique em `avançado` e continue. Esse alerta acontece por que o servidor está usando um certificado autoassinado para usar HTTPS e WSS.<br>
Por fim, aperte o botão `enviar` para conectar. 



<br>

## Para desenvolver - Windows:
    

Execute o seguinte comando, ele criará um certificado para permitir conexões https e wss:

    dotnet dev-certs https --trust
<br>
Em "src/services/bd" crie um arquivo .env com as seguintes variaveis:

    HOST = "127.0.0.1"
    MYSQL_ROOT_PASSWORD = ""
    MYSQL_DATABASE = ""
<br><br>



Com o Docker ligado, execute o arquivo "startup.bat", ele iniciará um container com o banco de dados, e criará contas falsas.<br>
O arquivo "run.bat" iniciará os serviços do programa ao ser executado.

#### alternativamente,

Em "src/services/bd" execute `docker compose up -d --build --wait`<br>
Caso queira criar contas falsas, execute `dotnet run pop`<br>
Para rodar qualquer serviço, navegue até "src/services/servico" e execute dotnet run
