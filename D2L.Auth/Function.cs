using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace D2L.Auth;

public class Function
{
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _d2LHosted;
	private readonly string _brightspaceBaseUrl;
	private readonly string _d2lAuthUri;
	private readonly string _d2lRefreshUri;
	private readonly string _redirectUri;
	private readonly string _scope;
	
	
	private readonly ILogger<Function> _logger;

	public Function(ILogger<Function> logger)
	{
		_logger = logger;

	    _clientId = Environment.GetEnvironmentVariable("ClientId");
	    _clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
	    _brightspaceBaseUrl = Environment.GetEnvironmentVariable("BrightspaceBaseUrl");
	    _d2lAuthUri = Environment.GetEnvironmentVariable("AuthUri");
	    _d2lRefreshUri = Environment.GetEnvironmentVariable("RefreshUri");
	    _scope = Environment.GetEnvironmentVariable("Scope");
	    _d2LHosted = Environment.GetEnvironmentVariable("D2LHosted");
	    _redirectUri = Environment.GetEnvironmentVariable("RedirectUri");
	}

	/// <summary>
	/// Step 1 Redirect UserFor Authorization.
	/// </summary>
	/// <param name="req"></param>
	/// <returns></returns>
	[Function("StartAuth")]
	public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
	{
		// https://learn-stg.anzca.edu.au/d2l/lp/extensibility/oauth2
		//var authUrl = $"{d2LHosted}d2l/auth/api/token?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code";
		var authUrl = $"{_brightspaceBaseUrl}{_d2lAuthUri}" +
		              $"?client_id={_clientId}" +
		              $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
		              $"&response_type=code" +
		              $"&scope={Uri.EscapeDataString(_scope)}";
		
		_logger.LogInformation($"Auth URL: {authUrl}");
		
		return new RedirectResult(authUrl);
	}
	
	/// <summary>
	/// Step 2 Handle the callback and exchange Code for Tokens
	/// </summary>
	/// <param name="req"></param>
	/// <returns></returns>
	[Function("Callback")]
	public async Task<IActionResult> RunCallback([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "callback")] HttpRequestData req)
	{
		_logger.LogInformation($"Made it to the callback");
		var code = req.Query["code"];
		if (string.IsNullOrWhiteSpace(code))
		{
			return new BadRequestObjectResult("Missing code");
		}
		
		_logger.LogInformation($"Code: {code}");
		
		using var client = new HttpClient();
		client.BaseAddress = new Uri(_brightspaceBaseUrl);
		
		var requestBody = new FormUrlEncodedContent(new[]
		{
			new KeyValuePair<string, string>("grant_type", "authorization_code"),
			new KeyValuePair<string, string>("code", code),
			new KeyValuePair<string, string>("client_id", _clientId),
			new KeyValuePair<string, string>("client_secret", _clientSecret),
			new KeyValuePair<string, string>("redirect_uri", _redirectUri),
		});
		
		var response = await client.PostAsync(_d2lRefreshUri, requestBody);
		if (!response.IsSuccessStatusCode)
		{
			var error = await response.Content.ReadAsStringAsync();
			return new BadRequestObjectResult($"Error exchanging token: {error}");
		}
		var tokenResult = await response.Content.ReadFromJsonAsync<TokenResponse>();
		return new OkObjectResult(tokenResult);
	}
}

public class TokenResponse
{
	[JsonPropertyName("access_token")]
	public string AccessToken { get; set; }

	[JsonPropertyName("refresh_token")]
	public string RefreshToken { get; set; }

	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }

	[JsonPropertyName("token_type")]
	public string TokenType { get; set; }
}