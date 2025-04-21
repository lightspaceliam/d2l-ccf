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
	//  cert password: pasSword1$
	private const string clientId = "337aa9da-bc87-42b4-86b6-bc99c2160394";
	private const string clientSecret = "AyLFJuL2ZfEN5dQxJtASmfyhf6yXPbXCIQQ2XIhWkFE";
	private const string d2LHosted = "https://learn-stg.anzca.edu.au/";
	private const string brightspaceBase = "https://auth.brightspace.com/";
	private readonly string d2lAuth = $"oauth2/auth";
	private readonly string d2lRefresh = $"core/connect/token";
	private const string redirectUri = $"https://localhost:3001/callback";
	private const string scope = "core:*:*";
	
	
	private readonly ILogger<Function> _logger;

	public Function(ILogger<Function> logger)
	{
		_logger = logger;
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
		var authUrl = $"{brightspaceBase}{d2lAuth}" +
		              $"?client_id={clientId}" +
		              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
		              $"&response_type=code" +
		              $"&scope={Uri.EscapeDataString(scope)}";
		
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
		client.BaseAddress = new Uri(brightspaceBase);
		
		var requestBody = new FormUrlEncodedContent(new[]
		{
			new KeyValuePair<string, string>("grant_type", "authorization_code"),
			new KeyValuePair<string, string>("code", code),
			new KeyValuePair<string, string>("client_id", clientId),
			new KeyValuePair<string, string>("client_secret", clientSecret),
			new KeyValuePair<string, string>("redirect_uri", redirectUri),
		});
		
		var response = await client.PostAsync(d2lRefresh, requestBody);
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