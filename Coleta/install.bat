@echo off
title Instalacao do Coleta
echo Iniciando a copia e configuracao do aplicativo...
echo.

:: 1. Define as variaveis de diretorio e arquivo
set "PASTA_ORIGEM=%~dp0"
set "PASTA_DESTINO=C:\Coleta"
set "NOME_EXE=coleta.exe"
set "CAMINHO_COMPLETO=%PASTA_DESTINO%\%NOME_EXE%"

:: 2. Cria a pasta destino no disco C (se ainda nao existir)
if not exist "%PASTA_DESTINO%" (
    echo Criando diretorio %PASTA_DESTINO%...
    mkdir "%PASTA_DESTINO%"
)

:: 3. Copia todos os arquivos da pasta atual para C:\Coleta
:: /E = Copia diretorios e subdiretorios, incluindo os vazios
:: /Y = Suprime a confirmacao para sobrescrever arquivos (otimo para atualizar o app depois)
:: /I = Se o destino nao existir e houver mais de um arquivo, assume que o destino deve ser uma pasta
:: /Q = Modo silencioso, nao exibe os nomes dos arquivos copiados
echo Copiando arquivos para o disco local...
xcopy "%PASTA_ORIGEM%*" "%PASTA_DESTINO%\" /E /I /Y /Q

:: Verifica se a copia do executavel funcionou
if not exist "%CAMINHO_COMPLETO%" (
    echo.
    echo ERRO: O executavel "%NOME_EXE%" nao foi encontrado na pasta de origem para ser copiado!
    echo Instalacao abortada.
    pause
    exit /b
)

:: 4. Cria a tarefa no Agendador do Windows apontando para o Disco C
echo.
echo Criando a tarefa agendada para iniciar com o Windows...
schtasks /create /tn "App_Coleta_Startup" /tr "\"%CAMINHO_COMPLETO%\"" /sc onlogon /rl highest /f

:: 5. Validacao final
if %errorlevel% equ 0 (
    echo.
    echo Instalacao concluida com sucesso!
    echo O aplicativo foi instalado em: %CAMINHO_COMPLETO%
    echo E iniciara em segundo plano no proximo login do usuario.
) else (
    echo.
    echo FALHA ao criar a tarefa no agendador!
    echo IMPORTANTE: O prompt de comando foi aberto como Administrador?
    echo Acesso negado e comum ao tentar gravar na raiz do C: sem privilegios.
)

echo.
pause