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
    protected StampyCommon.CloudStampyParameters StampyParameters {get; private set;}

    public ExecutorBase(StampyCommon.CloudStampyParameters stampyParameters)
    {
        this.StampyParameters = stampyParameters;
    }

    public abstract StampyResult Execute();
}