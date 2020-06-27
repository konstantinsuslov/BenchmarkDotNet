using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

public class BenchmarkRunnerTests
{
    private ITestOutputHelper Output { get; }

    public BenchmarkRunnerTests(ITestOutputHelper output) => Output = output;

    [Fact]
    public void WhenUserAsksForInfoAnInfoIsDisplayedAndNoBenchmarksAreExecuted()
    {
        var logger = new OutputLogger(Output);
        var config = ManualConfig.CreateEmpty().AddLogger(logger);

        var summary = BenchmarkRunner.Run(typeof(ClassWithTwoBenchmarks), config, new[] { "--info" });

        Assert.Null(summary);
        Assert.Contains(HostEnvironmentInfo.GetInformation(), logger.GetLog());
    }
}