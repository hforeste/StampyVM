//------------------------------------------------------------------------------
// <copyright file="ExecutorBase.csx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
#r "StampyCommon.dll"
#load "StampyResult.csx"
using StampyCommon;
using StampyCommon.Loggers;
using StampyCommon.SchedulerSettings;

public abstract class ExecutorBase
{
    protected StampyCommon.StampyParameters StampyParameters {get; private set;}

    public ExecutorBase(StampyCommon.StampyParameters stampyParameters)
    {
        this.StampyParameters = stampyParameters;
    }

    public abstract StampyResult Execute();
}