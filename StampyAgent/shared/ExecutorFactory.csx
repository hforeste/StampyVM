//------------------------------------------------------------------------------
// <copyright file="ExecutorFactory.csx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
#load "ExecutorBase.csx"
#load "DeploymentExecutor.csx"
#load "StampyParameters.csx"

public static class ExecutorFactory
{
    public static ExecutorBase GetExecutor(StampyParameters stampyParameters){
        switch (stampyParameters.JobType)
        {
            case StampyJobType.Deploy:
                return new DeploymentExecutor(stampyParameters);
            case StampyJobType.Build:
            case StampyJobType.Test:
            default:
                return null;
        }
    }
}