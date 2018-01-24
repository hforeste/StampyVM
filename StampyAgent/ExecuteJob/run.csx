
#load "..\shared\StampyParameters.csx"
#load "..\shared\ExecutorFactory.csx"
#load "..\shared\Logger.csx"
using System;
using System.Net;

public static void Run(StampyParameters jobRequest, TraceWriter log)
{
    var logger = Logger.LoadLogger(log);
    var executor = ExecutorFactory.GetExecutor(jobRequest);
    executor.Execute();
}
