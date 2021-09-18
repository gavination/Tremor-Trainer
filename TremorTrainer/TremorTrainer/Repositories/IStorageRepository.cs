using System;
using System.Collections.Generic;
using TremorTrainer.Models;

namespace TremorTrainer.Repositories
{
    public interface IStorageRepository
    {
        bool ExportSessions(List<Session> sessions);
        string GetDownloadPath();

    }
}
