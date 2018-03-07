#r "StampyCommon.dll"
#load "..\shared\ExecutorFactory.csx"
#load "..\shared\Logger.csx"
#load "..\shared\StampyResult.csx"
using System;
using System.Net;
using System.Diagnostics;
using StampyCommon;
using StampyCommon.Loggers;
using StampyCommon.SchedulerSettings;

public static StampyResult Run(StampyCommon.CloudStampyParameters jobRequest, TraceWriter log)
{
    var sw = Stopwatch.StartNew();
    var configuration = new GeneralConfiguration(null);
    StampyResult result = null;
    Exception exception = null;

    ICloudStampyLogger logger = new StampyWorkerEventsKustoLogger(configuration);
    IStampyResultsLogger resultsLogger = new StampyResultsLogger(configuration);

    logger.WriteInfo(jobRequest, "triggered from queue");
    var executor = ExecutorFactory.GetExecutor(jobRequest, logger);
    logger.WriteInfo(jobRequest, "Start execution for job request");

    try
    {
        result = executor.Execute();
    }catch(Exception ex)
    {
        result = new StampyResult();
        result.Result = JobResult.Failed;
        exception = ex;
        logger.WriteError(jobRequest, $"Error while executing job request", ex);
    }

    sw.Stop();
    logger.WriteInfo(jobRequest, $"Status: {result.Result.ToString()}");
    resultsLogger.WriteResult(jobRequest, result.Result.ToString(), (int)sw.Elapsed.TotalMinutes, exception);

    if(exception != null){
        throw exception;
    }
    return result;
}
