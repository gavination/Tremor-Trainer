using System;
using System.Collections.Generic;
using System.Text;

namespace TremorTrainer.Utilities
{
    public class InternalLoginFailedException : Exception
    {
        public InternalLoginFailedException()
        {
        }

        public InternalLoginFailedException(string message): base(message)
        {
        }
        public InternalLoginFailedException(string message, Exception inner)
        : base(message, inner)
        {
        }

    }

}
