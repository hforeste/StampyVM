
#load "..\shared\StampyParameters.csx"
#load "..\shared\ExecutorFactory.csx"
#load "..\shared\Logger.csx"
#load "..\shared\StampyResult.csx"
using System;
using System.Net;

public static StampyResult Run(StampyParameters jobRequest, TraceWriter log)
{
    var logger = Logger.LoadLogger(log);
    var executor = ExecutorFactory.GetExecutor(jobRequest);
    var result = executor.Execute();
    return result;
}
