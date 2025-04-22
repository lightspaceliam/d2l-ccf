# D2L Auth

## HTTPS

For this use case we need to run the Function with HTTPS locally, as the D2L app will not accept HTTP, even for local development. 

**Launch Settings:**

`\c:{path}\Solution\Project\Properties\launchSettings.json`

```json
{
  "profiles": {
    "D2L.Auth": {
      "commandName": "Project",
      "commandLineArgs": "\\\"--port 3001 --useHttps --cert C:\\Pocs\\Bed\\d2l-ccf\\D2L.Auth\\Certificates\\funcRootCa.pfx --password whatTha",
      "launchBrowser": false
    }
  }
}
```

This configuration will allow you to run both endpoints with HTTPS just by pressing the Visual Studio Play button:

```bash
https://localhost:3001/api/StartAuth
https://localhost:3001/api/callback?code={xyz}
```

## Local Settings

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ClientId": "",
    "ClientSecret": "",
    "BrightspaceBaseUrl": "",
    "AuthUri": "",
    "RefreshUri": "",
    "Scope": "",
    "D2LHosted": "",
    "RedirectUri": ""
        
  },
    ...
}
```