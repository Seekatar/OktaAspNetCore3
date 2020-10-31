# Okta Asp .NET Core 3.1 Sample

This takes the dotnet core `webapi` scaffold app and adds support for authenticating and authorizing from Okta JWT as if called from a SPA app.

For this test, I want to be able to tell if the user is in a specific group, and that's they've requested one of those groups in the scopes in the JWT.

## Changes To Sample

### Startup.cs

In `ConfigureServices()`

1. Added `IOptions` for `OktaSettings` so it can be injected
1. `AddAuthentication` and `AddJwtBearer` to have the middleware set the user claims from the JWT. Just by adding that you can get `Request.HttpContext.User.Claims` in a controller
1. `AddAuthorization` to add a policy for my group check
1. `AddSingleton<IAuthorizationHandler, GroupPolicyHandler>` to register my handler.

In `Configure()`

1. Comment out `app.UseHttpsRedirection` since otherwise I kept getting a 401
1. Added `UseAuthentication`

### WeatherForecastController.cs

1. Copy `Get` as `GetWithoutAuth` just to have an unauthorized method.
1. To existing `Get` added `[Authorize(Policy = GroupRequirement.PolicyName)]` to enforce authorization for that method

### GroupPolicyHandler.cs (new file)

This is where the meat of the authorization takes place. In this particular case it looks for a single scope with a value that starts with `ScopePrefix` and then makes sure that there is a `groups` Claim that matches it. If that's true, it will `Succeed` the requirement. (Note the scope and group names have different naming conventions at my company.)

## Okta Setup

I used my [OktaPosh](https://www.powershellgallery.com/packages/OktaPosh) module from the PowerShell Gallery to configure Okta.  For this scenario these are the basics.

* SPA Application
* Several "Client" Test Groups
* Test User, added to all the Groups
* AuthorizationServer
  * Scope for each Group
  * Claim to get Groups start with a prefix

## Testing

For my testing I used PowerShell, and my [OktaPosh](https://www.powershellgallery.com/packages/OktaPosh) module from the PowerShell Gallery.

Setup.

```Powershell
Import-Module OktaPosh
$baseUri = "https://localhost:5001"
```

Positive test gets a scope that the users is in.

```Powershell
$jwt = Get-OktaJwt -Issuer https://dev-11111.okta.com/oauth2/aus....4x7 -ClientId ****** -RedirectUri http://localhost:8080/dc-ui/implicit/callback -Username RelianceTest -ClientSecret *** -Scopes openid,casualty.datacapture.client.nw -GrantType implicit


Invoke-RestMethod "$BaseUri/weatherforecast" -Headers @{ Authorization = "Bearer $jwt"} -SkipCertificateCheck
```

Negative test doesn't get a client's scope, so it gets 403, Unauthorized

```Powershell
$jwt = Get-OktaJwt -Issuer https://dev-11111.okta.com/oauth2/aus....4x7 -ClientId ****** -RedirectUri http://localhost:8080/dc-ui/implicit/callback -Username RelianceTest -ClientSecret *** -Scopes openid -GrantType implicit


Invoke-RestMethod "$BaseUri/weatherforecast" -Headers @{ Authorization = "Bearer $jwt"} -SkipCertificateCheck
```
