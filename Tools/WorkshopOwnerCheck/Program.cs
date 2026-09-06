using System.Text;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;

return await Run(args);

static async Task<int> Run(string[] args)
{
    try
    {
        if (args.Length == 2 && args[0] == "auth")
        {
            if (!Path.IsPathFullyQualified(args[1]) || File.Exists(args[1]))
                throw new ControlledException("Use a new absolute token-file path outside the repository.");
            Console.Write("Steam account: ");
            var account = Console.ReadLine() ?? "";
            Console.Write("Password: ");
            var password = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace) { if (password.Length > 0) password.Length--; }
                else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
            }
            Console.WriteLine();
            using var session = new Session();
            await session.Connect();
            var auth = await session.Client.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
            {
                Username = account, Password = password.ToString(), IsPersistentSession = true,
                Authenticator = new UserConsoleAuthenticator()
            });
            password.Clear();
            var result = await auth.PollingWaitForResultAsync();
            await File.WriteAllTextAsync(args[1], result.RefreshToken, new UTF8Encoding(false));
            Console.WriteLine("Saved refresh token. Add it as STEAM_REFRESH_TOKEN, then remove the local file.");
            return 0;
        }
        if (args.Length != 1 || args[0] != "check")
            throw new ControlledException("Usage: check, or auth <new-absolute-token-file>.");
        var username = Secret("STEAM_USERNAME");
        var token = Secret("STEAM_REFRESH_TOKEN");
        using var live = new Session();
        await live.Connect();
        await live.Login(username, token);
        var service = live.Client.GetHandler<SteamUnifiedMessages>()!.CreateService<PublishedFile>();
        var request = new CPublishedFile_GetDetails_Request
        { appid = 294100, language = 0, includetags = true, short_description = false };
        request.publishedfileids.Add(3671535921UL);
        var response = await service.GetDetails(request);
        if (response.Result != EResult.OK || response.Body.publishedfiledetails.Count != 1)
            throw new ControlledException("Cannot read the existing Workshop item.");
        var item = response.Body.publishedfiledetails[0];
        var owner = live.Client.SteamID!.ConvertToUInt64();
        if (item.result != (uint)EResult.OK || item.publishedfileid != 3671535921UL ||
            item.consumer_appid != 294100 || item.creator != owner)
            throw new ControlledException("Workshop identity or ownership check failed.");
        Console.WriteLine("PASS: authenticated account owns Workshop item 3671535921. No Workshop writes performed.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex is ControlledException ? ex.Message :
            $"Steam ownership check failed ({ex.GetType().Name}); refresh authorization locally.");
        return 1;
    }
}

static string Secret(string key) => Environment.GetEnvironmentVariable(key) is { Length: > 0 } value
    ? value : throw new ControlledException($"Missing environment secret: {key}.");

sealed class Session : IDisposable
{
    public SteamClient Client { get; } = new();
    readonly CancellationTokenSource stop = new();
    readonly TaskCompletionSource connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource loggedIn = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CallbackManager manager;
    Task? pump;
    public Session()
    {
        manager = new(Client);
        manager.Subscribe<SteamClient.ConnectedCallback>(_ => connected.TrySetResult());
        manager.Subscribe<SteamClient.DisconnectedCallback>(_ =>
        {
            connected.TrySetException(new ControlledException("Steam disconnected."));
            loggedIn.TrySetException(new ControlledException("Steam disconnected."));
        });
        manager.Subscribe<SteamUser.LoggedOnCallback>(c =>
        {
            if (c.Result == EResult.OK) loggedIn.TrySetResult();
            else loggedIn.TrySetException(new ControlledException($"Steam login failed: {c.Result}. Refresh authorization locally."));
        });
    }
    public async Task Connect()
    {
        pump = Task.Run(() => { while (!stop.IsCancellationRequested) manager.RunWaitCallbacks(TimeSpan.FromMilliseconds(100)); });
        Client.Connect();
        await connected.Task.WaitAsync(TimeSpan.FromSeconds(45));
    }
    public async Task Login(string account, string token)
    {
        Client.GetHandler<SteamUser>()!.LogOn(new SteamUser.LogOnDetails
        { Username = account, AccessToken = token, ShouldRememberPassword = true });
        await loggedIn.Task.WaitAsync(TimeSpan.FromSeconds(45));
    }
    public void Dispose() { Client.Disconnect(); stop.Cancel(); pump?.Wait(TimeSpan.FromSeconds(2)); stop.Dispose(); }
}

sealed class ControlledException(string message) : Exception(message);
