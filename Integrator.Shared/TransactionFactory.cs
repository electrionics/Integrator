using System.Transactions;

namespace Integrator.Shared
{
    public static class TransactionFactory
    {
        public static TransactionScope CreateConfiguredWithDefaults()
        {
            var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

            return scope;
        }
    }
}
