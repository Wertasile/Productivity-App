
public class UserService
{
    private readonly HttpClient _httpClient;
    public UserService(IHttpClientFactory factory) {
        _httpClient = factory.CreateClient("UsersApi");
    }

    public async Task GetUser()
    {

    }

    public async Task DeleteUser()
    {

    }

    public async Task UpdateUser()
    {

    }
}

