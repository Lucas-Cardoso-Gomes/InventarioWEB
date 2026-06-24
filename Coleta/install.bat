@echo off

set "PASTA_DESTINO=C:\Coleta"
set "NOME_EXE=coleta.exe"
set "CAMINHO_COMPLETO=%PASTA_DESTINO%\%NOME_EXE%"

if not exist "%PASTA_DESTINO%" (
    mkdir "%PASTA_DESTINO%"
)

xcopy "%~dp0*" "%PASTA_DESTINO%\" /D /Y /R /I /Q

if exist "%CAMINHO_COMPLETO%" (
    schtasks /create ^
        /tn "App_Coleta_Boot" ^
        /tr "%CAMINHO_COMPLETO%" ^
        /sc onstart ^
        /rl highest ^
        /ru SYSTEM ^
        /f
)

exit /b 0