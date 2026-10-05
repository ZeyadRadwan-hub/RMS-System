using RMS_BACKEND.Services;
using Xunit;

namespace RMS_BACKEND.Tests;

public class ManagerDecisionTests
{
    [Trait("Category", "Authorization")]
    [Theory]
    [InlineData(4, 1, false)]
    [InlineData(4, 4, true)]
    [InlineData(null, 4, false)]
    public void Manager_may_decide_only_for_direct_reports(int? requestOwnerManagerId, int approverId, bool allowed)
    {
        var stateMachine = new RequestStateMachineService();
        Assert.Equal(allowed, stateMachine.CanApprove(TransactionStatus.Pending,
            "Manager", 8, requestOwnerManagerId, approverId));
        Assert.Equal(allowed, stateMachine.CanReject(TransactionStatus.Pending,
            "Manager", 8, requestOwnerManagerId, approverId));
    }
}
