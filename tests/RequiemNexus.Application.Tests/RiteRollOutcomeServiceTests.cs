using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Services;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="RiteRollOutcomeService"/> (Phase 19.5 P1-3).
/// </summary>
public class RiteRollOutcomeServiceTests
{
    [Fact]
    public async Task ApplyRiteRollOutcomeAsync_CallsConditionService_WhenMapped()
    {
        var condition = new Mock<IConditionService>();
        var sut = new RiteRollOutcomeService(condition.Object, Mock.Of<ILogger<RiteRollOutcomeService>>());

        await sut.ApplyRiteRollOutcomeAsync(9, "user-a", SorceryType.Cruac, RiteRollOutcomeTrigger.DramaticFailure);

        condition.Verify(
            c => c.ApplyConditionAsync(9, ConditionType.Tempted, null, null, "user-a"),
            Times.Once);
    }

    [Fact]
    public async Task ApplyRiteRollOutcomeAsync_SkipsConditionService_WhenNoTraditionMapping()
    {
        var condition = new Mock<IConditionService>();
        var sut = new RiteRollOutcomeService(condition.Object, Mock.Of<ILogger<RiteRollOutcomeService>>());

        await sut.ApplyRiteRollOutcomeAsync(9, "user-a", SorceryType.Necromancy, RiteRollOutcomeTrigger.DramaticFailure);

        condition.Verify(
            c => c.ApplyConditionAsync(It.IsAny<int>(), It.IsAny<ConditionType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyRiteRollOutcomeAsync_AppliesStumbled_OnContinueTrigger_ForNecromancy()
    {
        var condition = new Mock<IConditionService>();
        var sut = new RiteRollOutcomeService(condition.Object, Mock.Of<ILogger<RiteRollOutcomeService>>());

        await sut.ApplyRiteRollOutcomeAsync(3, "u", SorceryType.Necromancy, RiteRollOutcomeTrigger.ContinueAfterZeroSuccesses);

        condition.Verify(c => c.ApplyConditionAsync(3, ConditionType.Stumbled, null, null, "u"), Times.Once);
    }
}
