//------------------------------------------------------------------------------
// <copyright file="StampyResult.csx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
#load "JobResult.csx"

public class StampyResult {
    public string RequestId {get;set;}
    public string JobId {get;set;}
    public string BuildPath{ get; set; }
    public string DeploymentTemplate{ get; set; }
    public string CloudName {get; set; }
    public JobResult Result {get;set;}
    public string StatusMessage {get;set;}
}