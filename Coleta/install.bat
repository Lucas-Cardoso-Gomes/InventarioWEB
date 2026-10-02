@echo off

set "PASTA_DESTINO=C:\Coleta"
set "NOME_EXE=coleta.exe"
set "CAMINHO_COMPLETO=%PASTA_DESTINO%\%NOME_EXE%"

:: 1. Encerrar o processo do agente se ele já estiver em execução
:: (/F = Força finalização, /T = Finaliza processos filho se houver)
taskkill /F /IM "%NOME_EXE%" /T >nul 2>&1

:: 2. Criar a pasta de destino caso ainda não exista
if not exist "%PASTA_DESTINO%" (
    mkdir "%PASTA_DESTINO%"
)

:: 3. Copiar/Substituir todos os arquivos
:: Usar /Y (sobrescrever sem perguntar), /R (sobrescrever somente leitura) e /K (manter atributos)
xcopy "%~dp0*" "%PASTA_DESTINO%\" /Y /R /I /Q /K >nul 2>&1

:: 4. Recriar/Atualizar a Tarefa Agendada mantendo o NOME EXATO atual
if exist "%CAMINHO_COMPLETO%" (
    schtasks /create ^
        /tn "App_Coleta_Boot" ^
        /tr "\"%CAMINHO_COMPLETO%\"" ^
        /sc onstart ^
        /rl highest ^
        /ru SYSTEM ^
        /f >nul 2>&1

    :: 5. Reexecutar a tarefa agendada IMEDIATAMENTE após atualizar
    :: Desta forma a máquina já envia o inventário sem precisar reiniciar
    schtasks /run /tn "App_Coleta_Boot" >nul 2>&1
)

exit /b 0