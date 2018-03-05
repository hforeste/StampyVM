#r "StampyCommon.dll"
#load "..\shared\ExecutorFactory.csx"
#load "..\shared\Logger.csx"
#load "..\shared\StampyResult.csx"
using System;
using System.Net;
using StampyCommon;
using StampyCommon.Loggers;
using StampyCommon.SchedulerSettings;

public static StampyResult Run(StampyCommon.StampyParameters jobRequest, TraceWriter log)
{
    var configuration = new GeneralConfiguration(null);
    IStampyClientLogger logger = new StampyWorkerEventsKustoLogger(configuration);
    logger.WriteInfo(jobRequest, "triggered from queue");
    var executor = ExecutorFactory.GetExecutor(jobRequest, logger);
    logger.WriteInfo(jobRequest, $"Execute {jobRequest.JobType.ToString()} job");
    var result = executor.Execute();
    logger.WriteInfo(jobRequest, $"Status: {result.Result.ToString()}");
    return result;
}
