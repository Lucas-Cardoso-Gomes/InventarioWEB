# 1. Cria o certificado com validade de 10 anos e criptografia forte
$cert = New-SelfSignedCertificate -Subject "CN=192.168.1.100" `
    -TextExtension @("2.5.29.17={text}IPAddress=192.168.1.100&DNS=ServidorInventario") `
    -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(10) `
    -CertStoreLocation "cert:\CurrentUser\My"

# 2. Define a senha
$pwd = ConvertTo-SecureString -String "password" -Force -AsPlainText

# 3. Exporta o arquivo .pfx para você colocar na pasta do servidor
Export-PfxCertificate -Cert $cert -FilePath "C:\inventario_seguro.pfx" -Password $pwd