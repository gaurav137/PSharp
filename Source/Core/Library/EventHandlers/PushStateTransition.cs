﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the repo root for full license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Defines a push state transition.
    /// </summary>
    internal sealed class PushStateTransition
    {
        /// <summary>
        /// Target state.
        /// </summary>
        public Type TargetState;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PushStateTransition(Type targetState)
        {
            this.TargetState = targetState;
        }
    }
}
