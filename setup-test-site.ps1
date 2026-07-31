#Requires -RunAsAdministrator
# Creates a test IIS website for IISBlitz testing

Import-Module WebAdministration

$siteName = "IISBlitzTest"
$sitePath = "C:\inetpub\$siteName"
$port = 8099

# Create folder structure
New-Item -Path $sitePath -ItemType Directory -Force | Out-Null
New-Item -Path "$sitePath\logs" -ItemType Directory -Force | Out-Null

# index.html
@"
<!DOCTYPE html>
<html>
<head><title>IISBlitz Test</title></head>
<body><h1>IISBlitz Test Site</h1><p>It works!</p></body>
</html>
"@ | Set-Content "$sitePath\index.html"

# appsettings.json
@"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=TestDb;Trusted_Connection=true"
  }
}
"@ | Set-Content "$sitePath\appsettings.json"

# web.config
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <defaultDocument>
      <files>
        <add value="index.html" />
      </files>
    </defaultDocument>
  </system.webServer>
</configuration>
"@ | Set-Content "$sitePath\web.config"

# Dummy log
"$(Get-Date) - IISBlitz test log entry" | Set-Content "$sitePath\logs\test.log"

# Remove site if it already exists
if (Get-Website -Name $siteName -ErrorAction SilentlyContinue) {
    Remove-Website -Name $siteName
}

# Remove app pool if it already exists
if (Test-Path "IIS:\AppPools\$siteName") {
    Remove-WebAppPool -Name $siteName
}

# Create app pool and site
New-WebAppPool -Name $siteName | Out-Null
New-Website -Name $siteName -PhysicalPath $sitePath -Port $port -ApplicationPool $siteName | Out-Null

Write-Host ""
Write-Host "Done! Created IIS site '$siteName' on http://localhost:$port" -ForegroundColor Green
Write-Host "Physical path: $sitePath" -ForegroundColor Cyan
