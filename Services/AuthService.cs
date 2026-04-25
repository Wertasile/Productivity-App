 

using Blazored.LocalStorage;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

public class AuthService
{
    private readonly TokenStore _store;
    private readonly SessionStore _sessionStore;
    private readonly CustomAuthStateProvider _authStateProvider;
    private readonly HttpClient _httpClient;

    private const string COGNITO_ENDPOINT = "https://cognito-idp.us-east-1.amazonaws.com/";
    private const string CLIENT_ID = "6b61u0jrrv14ppccnrqrgk814k";

    public AuthService(
        TokenStore store,
        SessionStore sessionStore,
        CustomAuthStateProvider authStateProvider)
    {
        _httpClient = new HttpClient();
        _store = store;
        _sessionStore = sessionStore;
        _authStateProvider = authStateProvider;
    }

    // create a http request message to send to the cognito endpoint
    private HttpRequestMessage CreateRequest(string target, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, COGNITO_ENDPOINT);
        request.Headers.Add("X-Amz-Target", target);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/x-amz-json-1.1"
        );
        return request;
    }


    public async Task<bool> RefreshTokensIfNeededAsync()
    {
        var tokens = await _store.GetTokensAsync();

        if (tokens == null) return false;

        Console.WriteLine($"Stored expiry: {tokens.ExpiresAtUtc}");
        Console.WriteLine($"Now: {DateTime.UtcNow}");

        if (tokens.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(5)) return true;

        // creating a http request to refresh the tokens using the refresh token
        var request = new HttpRequestMessage(HttpMethod.Post, COGNITO_ENDPOINT);

        request.Headers.Add("X-Amz-Target","AWSCognitoIdentityProviderService.InitiateAuth");
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            AuthFlow = "REFRESH_TOKEN_AUTH",
            ClientId = CLIENT_ID,
            AuthParameters = new
            {
                REFRESH_TOKEN = tokens.RefreshToken
            }
        }), Encoding.UTF8, "application/x-amz-json-1.1");

        // send the http request
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Token refresh FAILED");
            return false;
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("AuthenticationResult");

        // after refreshing token, and receiving it via result, we access it and save it to the token store
        tokens.AccessToken = result.GetProperty("AccessToken").GetString()!;
        if (result.TryGetProperty("IdToken", out var idToken))
        {
            tokens.IdToken = idToken.GetString()!;
        }
        tokens.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(
            result.GetProperty("ExpiresIn").GetInt32());

        Console.WriteLine("New expiry:");
        Console.WriteLine(tokens.ExpiresAtUtc);

        // save the new tokens to the token store
        await _store.SaveTokensAsync(tokens);
        return true;
    }

    public async Task<AuthResult> Register(string username, string email, string password)
    {
        var request = CreateRequest("AWSCognitoIdentityProviderService.SignUp", new
        {
            ClientId = CLIENT_ID,
            Username = username,
            Password = password,
            UserAttributes = new[]
            {
                new { Name = "email", Value = email }
            }
        });

        // create and send the http request
        var response = await _httpClient.SendAsync(request);

        var result = new AuthResult();

        if (!response.IsSuccessStatusCode)
        {
            result.Success = false;
            result.ErrorMessage = "Registration failed. Please try again.";
            return result;
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        bool confirmed = doc.RootElement.GetProperty("UserConfirmed").GetBoolean();

        result.Success = true;
        result.RequiresEmailConfirmation = !confirmed;

        return result;

    }

    public async Task<AuthResult> ConfirmEmailAsync(string email, string code)
    {
        var request = CreateRequest(
            "AWSCognitoIdentityProviderService.ConfirmSignUp",
            new
            {
                ClientId = CLIENT_ID,
                Username = email,
                ConfirmationCode = code
            });

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid or expired confirmation code."
            };
        }

        return new AuthResult
        {
            Success = true
        };
    }

    public async Task ResendConfirmationAsync(string email)
    {
        var request = CreateRequest(
            "AWSCognitoIdentityProviderService.ResendConfirmationCode",
            new
            {
                ClientId = CLIENT_ID,
                Username = email
            });

        await _httpClient.SendAsync(request);
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        var request = CreateRequest(
            "AWSCognitoIdentityProviderService.InitiateAuth",
            new
            {
                AuthFlow = "USER_PASSWORD_AUTH",
                ClientId = CLIENT_ID,
                AuthParameters = new
                {
                    USERNAME = email,
                    PASSWORD = password
                }
            });

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Invalid credentials.");

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var result = doc.RootElement.GetProperty("AuthenticationResult");

        var tokens = new AuthTokens
        {
            AccessToken = result.GetProperty("AccessToken").GetString()!,
            IdToken = result.GetProperty("IdToken").GetString()!,
            RefreshToken = result.GetProperty("RefreshToken").GetString()!,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(
                result.GetProperty("ExpiresIn").GetInt32())
        };

        await _store.SaveTokensAsync(tokens);
        await _sessionStore.RemoveGuestSessionAsync();
        _authStateProvider.NotifyUserAuthentication();

        if (tokens == null)
        {
            return ("Failed to Login. Incorrect Username or password.");
        }

        return ("Login successful.");
    }

    
    public async Task ForgotPasswordAsync(string email)
    {
        var request = CreateRequest(
            "AWSCognitoIdentityProviderService.ForgotPassword",
            new
            {
                ClientId = CLIENT_ID,
                Username = email
            });

        await _httpClient.SendAsync(request);
    }

    public async Task ConfirmForgotPasswordAsync(string email,string code,string newPassword)
    {
        var request = CreateRequest(
            "AWSCognitoIdentityProviderService.ConfirmForgotPassword",
            new
            {
                ClientId = CLIENT_ID,
                Username = email,
                ConfirmationCode = code,
                Password = newPassword
            });

        await _httpClient.SendAsync(request);
    }

    public async Task LogoutAsync()
    {
        var tokens = await _store.GetTokensAsync();
        if (tokens != null)
        {
            var request = CreateRequest(
                "AWSCognitoIdentityProviderService.GlobalSignOut",
                new { AccessToken = tokens.AccessToken });

            await _httpClient.SendAsync(request);
        }

        await _store.RemoveTokensAsync();
        await _sessionStore.RemoveGuestSessionAsync();
        _authStateProvider.NotifyUserLogout();
    }
}
