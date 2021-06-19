using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TremorTrainer.Models;

namespace TremorTrainer.Services
{
    public class SessionService: ISessionService
    {
        public SessionService()
        {
        }

        public async Task<List<Session>> GetSessions()
        {
            List<Session> sessions = new List<Session>
            {
                new Session {
                    SessionId = "1",
                    SessionLength = "10 minutes",
                    Type = SessionType.Introductory,
                    Status = SessionStatus.Passed,
                    Timestamp = DateTime.Now,
                    UserId = "2",
                    Description = "Sample first session"
            },
                new Session {
                    SessionId = "2",
                    SessionLength = "20 minutes",
                    Type = SessionType.Therapeutic,
                    Status = SessionStatus.Failed,
                    Timestamp = DateTime.Now,
                    UserId = "2",
                    Description = "Sample second session"
            }
            };

            return await Task.FromResult(sessions);
        }
    }

    public interface ISessionService
    {
        Task<List<Session>> GetSessions();
    }
}
