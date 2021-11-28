using MathNet.Numerics;
using System;
using System.Collections.Generic;
using TremorTrainer.Models;

namespace TremorTrainer.Repositories
{
    public interface IStorageRepository
    {
        string GetDownloadPath();
    }
}
