@echo off
:: =====================================================================
:: Script de Instalação Silenciosa - Coleta de Inventário
:: Recomendado: Executar via GPO de Inicialização (Configurações do Computador)
:: =====================================================================

set "PASTA_DESTINO=C:\Coleta"
set "NOME_EXE=coleta.exe"
set "CAMINHO_COMPLETO=%PASTA_DESTINO%\%NOME_EXE%"

:: 1. Cria o diretório de destino se não existir
if not exist "%PASTA_DESTINO%" (
    mkdir "%PASTA_DESTINO%"
)

:: 2. Copia os arquivos da origem (onde o .bat está) para o C:\Coleta
:: /D = Copia apenas se o arquivo de origem for mais novo que o destino (Update)
:: /Y = Suprime confirmação para sobrescrever
:: /R = Sobrescreve arquivos somente-leitura
:: /I = Se o destino não existir, presume que é uma pasta
:: /Q = Modo silencioso
xcopy "%~dp0*" "%PASTA_DESTINO%\" /D /Y /R /I /Q

:: 3. Verifica se o executável foi copiado com sucesso
if exist "%CAMINHO_COMPLETO%" (
    :: 4. Cria/Atualiza a tarefa agendada
    :: /sc onlogon = Executa sempre que um usuário logar
    :: /rl highest = Executa com privilégios máximos (Admin)
    :: /ru "SYSTEM" = Garante que a tarefa tenha permissão de sistema
    :: /f = Força a criação mesmo que a tarefa já exista
    schtasks /create /tn "App_Coleta_Boot" /tr "\"%CAMINHO_COMPLETO%\"" /sc onstart /rl highest /f /ru "SYSTEM"
)

:: Finaliza o script sem interações
exit /b