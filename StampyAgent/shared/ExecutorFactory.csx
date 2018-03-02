//------------------------------------------------------------------------------
// <copyright file="ExecutorFactory.csx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
#r "StampyCommon.dll"
#load "ExecutorBase.csx"
#load "DeploymentExecutor.csx"
using StampyCommon;
using StampyCommon.Loggers;
using StampyCommon.SchedulerSettings;

public static class ExecutorFactory
{
    public static ExecutorBase GetExecutor(StampyCommon.StampyParameters stampyParameters, IStampyClientLogger logger){
        switch (stampyParameters.JobType)
        {
            case StampyJobType.Deploy:
                return new DeploymentExecutor(stampyParameters, logger);
            case StampyJobType.Build:
            case StampyJobType.Test:
            default:
                return null;
        }
    }
}