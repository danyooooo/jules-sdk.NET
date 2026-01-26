// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;
using Xunit;

namespace JulesSdk.Tests;

public class SessionStateTests
{
    [Theory]
    [InlineData(SessionState.Unspecified, "Unspecified")]
    [InlineData(SessionState.Queued, "Queued")]
    [InlineData(SessionState.Planning, "Planning")]
    [InlineData(SessionState.AwaitingPlanApproval, "AwaitingPlanApproval")]
    [InlineData(SessionState.InProgress, "InProgress")]
    [InlineData(SessionState.Completed, "Completed")]
    [InlineData(SessionState.Failed, "Failed")]
    public void SessionState_HasCorrectStringRepresentation(SessionState state, string expected)
    {
        Assert.Equal(expected, state.ToString());
    }
    
    [Fact]
    public void SessionState_AllValuesAreDefined()
    {
        var states = Enum.GetValues<SessionState>();
        Assert.Equal(9, states.Length);
    }
}

public class OriginTests
{
    [Theory]
    [InlineData(Origin.User, "User")]
    [InlineData(Origin.Agent, "Agent")]
    [InlineData(Origin.System, "System")]
    public void Origin_HasCorrectStringRepresentation(Origin origin, string expected)
    {
        Assert.Equal(expected, origin.ToString());
    }
}
