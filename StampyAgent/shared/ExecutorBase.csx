//------------------------------------------------------------------------------
// <copyright file="ExecutorBase.csx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
#load "StampyParameters.csx"

public abstract class ExecutorBase
{
    protected StampyParameters StampyParameters {get; private set;}

    public ExecutorBase(StampyParameters stampyParameters)
    {
        this.StampyParameters = stampyParameters;
    }

    public abstract void Execute();
}