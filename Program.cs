using ActualBlazorStandAloneAppSPA;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

// enables @attribute [Authorize] usage in razor pages which allows users in page if user is authenticated , and <AuthorizeView>
builder.Services.AddAuthorizationCore();

// determines logged in user
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

// makes auth state available to all components, so that we can use <AuthorizeView> and [Authorize] in any component
builder.Services.AddCascadingAuthenticationState();

// we are defining a factory function within AddScoped to create an HttpClient instance that is configured with the base address of the application.
// This allows us to inject HttpClient into our components and services, and it will be properly configured to make requests to our backend API.

builder.Services.AddScoped<AuthMessageHandler>();

builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<SessionStore>();
builder.Services.AddScoped<AppSessionService>();
builder.Services.AddScoped<LocalAppStateStore>();



// OLD IMPLEMENTATION, COGNITO AUTH DOES NOT USE THE USER API
// IT ALSO DOES NOT NEED BEARER TOKENS, AS WE AINT EVEN AUTHENTICATED YET TO RECEIVE IT.
// BECAUSE COGNITO ENDPOINT IS ABSOLUTE, WE DONT NEED A FACTORY. i.e. WE DONT NEED TO SET A BASE ADDRESS.
//builder.Services.AddHttpClient<AuthService>(client =>
//    client.BaseAddress = new Uri("https://9uy2cbe5q1.execute-api.us-east-1.amazonaws.com/Prod/users/")
//)
//.AddHttpMessageHandler<AuthMessageHandler>();

/* -----------------------------
   Cognito Client
------------------------------*/
builder.Services.AddScoped<AuthService>();

/* -----------------------------
   Users API
------------------------------*/

builder.Services.AddHttpClient("UsersApi", client =>
{
    client.BaseAddress = new Uri("https://9uy2cbe5q1.execute-api.us-east-1.amazonaws.com/Prod/users/");
})
.AddHttpMessageHandler<AuthMessageHandler>();

/* -----------------------------
   APP-API for TASKBOARD, CALENDAR, DASHBOARD, NOTES, FOLDERS, and more.
------------------------------*/

builder.Services.AddHttpClient("AppApi", client =>
{
    client.BaseAddress = new Uri("https://27h4fr76cj.execute-api.us-east-1.amazonaws.com/");
})
.AddHttpMessageHandler<AuthMessageHandler>();

/* -----------------------------
   SERVICES
------------------------------*/

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<FolderService>();
builder.Services.AddScoped<TaskBoardService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<JournalService>();
builder.Services.AddScoped<ProjectService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
