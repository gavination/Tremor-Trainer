using System;
using System.Threading.Tasks;
using TremorTrainer.Utilities;

namespace TremorTrainer.Services
{
    public class AuthService: IAuthService
	{
        private Supabase.Client Client { get; set; }
        public AuthService()
		{

            var url = AppSettingsManager.Settings["SupabaseUrl"];
            var key = AppSettingsManager.Settings["SupabaseKey"];

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true,
                SessionHandler = new CustomSessionHandler(), //You can use another Implementation, only change the logic in the file.
            };

            Client = new Supabase.Client(url, key, options);
            Client.InitializeAsync();
        }

        // required to appease Supabase's email constraint
        private string MassageUsername(string username)
        {
            return username + "@noreply-user.tremortrainer.com";
        }

        public async Task<bool> Login(string username, string password)
        {
            try
            {
                var formattedUsername = MassageUsername(username);
                var session = await Client.Auth.SignIn(formattedUsername, password);
                if (session != null)
                {
                    // if we get a valid session back, persist it on the local device
                    var persistedSession = new CustomSessionHandler();
                    persistedSession.SaveSession(session);
                    Client.Auth.SetPersistence(persistedSession);
                    return true;
                }
                return false;
            }
            catch(Exception e)
            {
                Console.Error.WriteLine($"User failed to login. Details: {e.Message}");
                throw e;
            }

        }


        public async Task<bool> TryLoadSession()
        {
            Client.Auth.LoadSession(); //load the session
            var session = await Client.Auth.RetrieveSessionAsync(); // try to fetch existing session
            
            if (session == null)
            {
                // return false if session isn't loaded
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task Logout()
        {
            Client.Auth.LoadSession();
            var session = await Client.Auth.RetrieveSessionAsync();
            if (session != null)
            {
                 await Client.Auth.SignOut();
            }
        }
    }

    public interface IAuthService
    {
        Task<bool> Login(string username, string password);
        Task<bool> TryLoadSession();
        Task Logout();
    }
}

