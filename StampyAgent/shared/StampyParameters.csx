//------------------------------------------------------------------------------
// <copyright file="StampyParameters.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

public class StampyParameters{
    
    public StampyJobType JobType{ get; set; }
    public string BuildPath{ get; set; }
    public string DeploymentTemplate{ get; set; }
    public string CloudName {get; set; }
    internal string HostingPath
    {
        get
        {
            return BuildPath + @"\Hosting";
        }
    }

    public override string ToString(){
        var s = $"JobType: {JobType.ToString()} BuildPath: {BuildPath}";
        return s;
    }
}

public enum StampyJobType
{
    Build, Deploy, Test
}