using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Shared
{
    public interface ILoggerDecorator
    {
        void Add(ILogger logger);
    }
}
