@echo off
set "params=-DnsName '192.168.0.2','Servidor' -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(10) -CertStoreLocation 'cert:\CurrentUser\My'"

powershell -Command "$cert = New-SelfSignedCertificate -Subject 'CN=192.168.0.2' %params%; $pwd = ConvertTo-SecureString -String 'SenhaForte123' -Force -AsPlainText; Export-PfxCertificate -Cert $cert -FilePath '.\inventario_cert.pfx' -Password $pwd"

echo Certificado gerado na pasta atual!
pause