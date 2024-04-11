using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Web.Blazor.Shared
{
    public sealed class IntStringKeyValuePair
    {
        [SetsRequiredMembers]
        public IntStringKeyValuePair(int key, string value)
        {
            Key = key;
            Value = value;
        }

        public required int Key { get; init; }
        public required string Value { get; init; }
    }
}
