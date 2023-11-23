
#requires -RunAsAdministrator

$InformationPreference = 'Continue'
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
Set-StrictMode -Version 'Latest'

if (-not (Get-Command -Name 'dotnet.exe' -ErrorAction Ignore))
{
    Write-Error "The ""dotnet.exe"" executable was not found in your PATH. Please install the .NET SDK (winget install Microsoft.DotNet.SDK.8)."
    return
}

$psExec = Join-Path -Path $PSScriptRoot -ChildPath 'PSTools/PsExec64.exe'
if (-not (Test-Path -Path $psExec))
{
    Write-Information "Downloading PsExec64.exe to ""PSTools/PsExec64.exe"""
    Invoke-WebRequest -Uri 'https://download.sysinternals.com/files/PSTools.zip' -OutFile 'PSTools.zip'
    Expand-Archive -Path (Join-Path -Path $PSScriptRoot -ChildPath 'PSTools.zip') -DestinationPath (Join-Path -Path $PSScriptRoot -ChildPath 'PSTools')
    $psExec = $psExec | Resolve-Path | Select-Object -ExpandProperty 'ProviderPath'
    Remove-Item -Path (Join-Path -Path $PSScriptRoot -ChildPath 'PSTools.zip')
}

$subject = 'dotnet-x509-certificate-bug'
$password = 'password'
$publicCertPath = Join-Path -Path $PSScriptRoot -ChildPath "${subject}.cer"
$pfxPath = Join-Path -Path $PSScriptRoot -ChildPath "${subject}.pfx"

if (-not (Test-Path -Path $pfxPath))
{
    $certReqFile = Join-Path -Path $PSScriptRoot -ChildPath "certreq.txt" -Resolve

    Write-Information "Creating certificate..."
    certreq.exe -f -new $certReqFile $publicCertPath

    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($publicCertPath)
    $thumbprint = $certificate.Thumbprint

    $store = [System.Security.Cryptography.X509Certificates.X509Store]::new('My', 'CurrentUser')
    $store.Open('ReadWrite')

    $certificate = $store.Certificates | Where-Object { $_.Thumbprint -eq $thumbprint }

    $pfxBytes = $certificate.Export('PFX', $password)
    [System.IO.File]::WriteAllBytes($pfxPath, $pfxBytes)

    $store.Remove($certificate)
    $store.Close()
    $store.Dispose()
    $certificate.Dispose()
    Write-Information "Certificate ""${subject}"" created and exported to ""$($pfxPath | Resolve-Path -Relative)"" with password ""${password}""."
    Write-Information ''
}

$csprojPath = Join-Path -Path $PSScriptRoot -ChildPath 'DotnetX509CertificateBug.csproj' -Resolve

Write-Information "Running DotnetX509CertificateBug project as SYSTEM user..."
& $psExec -nobanner -accepteula -s -w $PSScriptRoot dotnet.exe run --project $csprojPath
