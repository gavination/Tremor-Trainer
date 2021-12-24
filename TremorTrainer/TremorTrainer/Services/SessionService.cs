using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TremorTrainer.Models;
using TremorTrainer.Repositories;

namespace TremorTrainer.Services
{
    public class SessionService : ISessionService
    {
        private readonly List<Session> _sessions;
        private readonly ISessionRepository _sessionRepository;
        private readonly IMessageService _messageService;

        public SessionService(ISessionRepository sessionRepository, IMessageService messageService)
        {
            _sessionRepository = sessionRepository;
            _messageService = messageService;
            _sessions = _sessionRepository.GetSessions();
        }

        public async Task<bool> AddSessionAsync(Session newSession)
        {
            _sessions.Add(newSession);
            var result = _sessionRepository.AddSession(newSession);

            if (result > 0)
            {
                return await Task.FromResult(true);
            }
            else
            {
                string errorMessage = $"Error: {Constants.UnknownErrorMessage} Details: Unable to save session to database.";
                await _messageService.ShowAsync(errorMessage);
                return await Task.FromResult(false);
            }
        }

        public async Task<bool> UpdateSessionAsync(Session Session)
        {
            var oldSession = _sessions.FirstOrDefault((Session arg) => arg.Id == Session.Id);
            _sessions.Remove(oldSession);
            _sessions.Add(Session);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteSessionAsync(Guid id)
        {
            Session oldSession = _sessions.FirstOrDefault((Session arg) => arg.Id == id);
            _sessions.Remove(oldSession);

            return await Task.FromResult(true);
        }

        public async Task<Session> GetSessionAsync(Guid SessionId)
        {
            return await Task.FromResult(_sessions.FirstOrDefault(s => s.Id == SessionId));
        }

        public async Task<IEnumerable<Session>> GetSessionsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(_sessions);
        }

        public async Task<bool> ExportUserSessions()
        {
            var userResponse = await _messageService.ShowCancelAlertAsync("Export Session Data?",
                "Are you sure you want to export?");
            if (userResponse)
            {
                var result = _sessionRepository.ExportSessions(_sessions);

                if (result != null)
                {
                    await _messageService.ShowAsync($"Exported sessions to csv file at this location: {result}");
                    return true;
                }
                else
                {
                    string errorMessage = $"Error: {Constants.UnknownErrorMessage} Details: Unable to export sessions to file";
                    await _messageService.ShowAsync(errorMessage);
                    return false;
                }
            }
            return false;
           
        }


        public void DeleteSessions()
        {
            _sessionRepository.DeleteSessions();
        }

        public bool DetermineFirstSession()
        {
            var inductionSessions = _sessionRepository.GetSessions().Where(x => x.Type == SessionType.Induction);

            if (inductionSessions.Any())
            {
                return false;
            }
            return true;
        }
        public  SessionType GetSessionType(bool isPrescribed)
        {
            if (!isPrescribed)
            {
                return SessionType.AsNeeded;
            }
            bool isFirstSession = DetermineFirstSession();
            return isFirstSession ? SessionType.Induction : SessionType.Maintenance;
        }

        public int GetSessionLength(bool isPrescribed)
        {
            if (!isPrescribed)
            {
                return Constants.AsNeededSessionTimeLimit;
            }

            //determine if this is the first session for the User
            var isFirstSession = DetermineFirstSession();
            if (isFirstSession)
            {
                return Constants.FirstPrescribedSessionTimeLimit;
            }
            else
            {
                return Constants.PrescribedSessionTimeLimit;
            }
        }
    }

    public interface ISessionService
    {
        Task<bool> AddSessionAsync(Session newSession);
        Task<Session> GetSessionAsync(Guid SessionId);
        Task<IEnumerable<Session>> GetSessionsAsync(bool forceRefresh = false);
        SessionType GetSessionType(bool isPrescribed);
        int GetSessionLength(bool isPrescribed);
        Task<bool> ExportUserSessions();
        void DeleteSessions();
        bool DetermineFirstSession();
    }
}