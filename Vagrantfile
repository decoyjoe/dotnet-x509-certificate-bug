# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.box = "gusztavvargadr/windows-11"

  config.vm.provider "virtualbox" do |vb|
    vb.gui = true
  end

  config.vm.provision "shell", inline: <<-SHELL
    $InformationPreference = 'Continue'
    $ProgressPreference = 'SilentlyContinue'

    $dotnetInstaller = Join-Path -Path $env:TEMP -ChildPath 'dotnet-installer.exe'
    Invoke-WebRequest -Uri 'https://download.visualstudio.microsoft.com/download/pr/cb56b18a-e2a6-4f24-be1d-fc4f023c9cc8/be3822e20b990cf180bb94ea8fbc42fe/dotnet-sdk-8.0.101-win-x64.exe' -OutFile $dotnetInstaller
    Start-Process -FilePath $dotnetInstaller -ArgumentList '/install', '/passive', '/norestart' -NoNewWindow -Wait

    $env:PATH = [Environment]::GetEnvironmentVariable('PATH', 'Machine')

    C:\\Vagrant\\Reproduce-Bug.ps1

    Write-Information ''
    Write-Information 'To reproduce again:'
    Write-Information '  1. Login with vagrant:vagrant'
    Write-Information '  2. Launch PowerShell from the Start Menu'
    Write-Information '  3. Run: C:\\Vagrant\\Reproduce-Bug.ps1'
  SHELL
end
