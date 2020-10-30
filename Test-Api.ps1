[CmdletBinding()]
param(
    [string] $BaseUri = "https://localhost:5001",
    [string] $ClientId = $env:oktaclientid,
    [string] $ClientSecret = $env:oktaclientsecret,
    [string] $TokenUrl = $env:oktatokenurl
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (!(Get-Module OktaPosh)) {
    Import-Module OktaPosh -Force
}

# DRE App,
$jwt = Get-OktaJwt -ClientSecret $env:oktaclientsecret `
                    -ClientId $ClientId `
                    -OktaTokenUrl $TokenUrl `
                    -GrantType 'client_credentials' `
                    -Scopes 'access:token','get:item','save:item'

Write-Verbose ($jwt)

if ($jwt) {
    ">>> Not Authenticated"
    Invoke-RestMethod "$BaseUri/weatherforecast/weather" -SkipCertificateCheck

    "`n>>> Authenticated"
    Invoke-RestMethod "$BaseUri/weatherforecast" -Headers @{ Authorization = "Bearer $jwt"} -SkipCertificateCheck
} else {
    "Didn't get JWT"
}