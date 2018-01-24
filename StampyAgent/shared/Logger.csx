//------------------------------------------------------------------------------
// <copyright file="Logger.csx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Net;

private static string HostMachineName
{
    get
    {
        string machineHostName = Dns.GetHostName();
        return machineHostName;
    }
}

public class Logger
{
    private static TraceWriter _logger;

    private Logger(TraceWriter log){
        _logger = log;
    }

    public static Logger LoadLogger(TraceWriter log){
        return new Logger(log);
    }

    public static void Info(string message, string source = null)
    {
        _logger.Info(message, HostMachineName);
    }

    public static void Error(string message, Exception ex = null){
        _logger.Error(message, ex, HostMachineName);
    }
}