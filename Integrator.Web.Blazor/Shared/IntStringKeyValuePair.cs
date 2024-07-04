using System.Diagnostics.CodeAnalysis;

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
