@echo off
start "Banco de dados" cmd /K dotnet run --project "%~dp0\src\services\bd"
start "Interface" cmd /K dotnet run --project "%~dp0\src\services\interface"
start "Websocket" cmd /K dotnet run --project "%~dp0\src\services\websocket"
start "Sudoku Gen" cmd /K dotnet run --project "%~dp0\src\services\sudoku_gen"
start "Display" cmd /K dotnet run --project "%~dp0\src\services\display_api"