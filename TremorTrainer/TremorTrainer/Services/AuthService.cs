using System;
using System.Threading.Tasks;
using TremorTrainer.Utilities;

namespace TremorTrainer.Services
{
	public class AuthService: IAuthService
	{
        private Supabase.Client AuthClient { get; set; }

        public AuthService()
		{

            var url = AppSettingsManager.Settings["SupabaseUrl"];
            var key = AppSettingsManager.Settings["SupabaseKey"];

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true
            };

            AuthClient = new Supabase.Client(url, key, options);
            AuthClient.InitializeAsync();
        }

        public async Task<bool> Login(string username, string password)
        {
            await AuthClient.Auth.SignIn("gavin@noreply-user.tremortrainer.com", "password");
            if (AuthClient.Auth.CurrentSession != null)
            {
                return true;
            }
            return false;
        }
    }

    public interface IAuthService
    {
        Task<bool> Login(string username, string password);
    }
}

