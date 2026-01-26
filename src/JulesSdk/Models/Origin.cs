// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Models;

/// <summary>
/// The entity that an activity originates from.
/// </summary>
public enum Origin
{
    /// <summary>Activity originated from the user.</summary>
    User,
    
    /// <summary>Activity originated from the agent.</summary>
    Agent,
    
    /// <summary>Activity originated from the system.</summary>
    System
}
