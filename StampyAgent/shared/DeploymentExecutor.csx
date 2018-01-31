//------------------------------------------------------------------------------
// <copyright file="DeploymentExecutor.csx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
#load "ExecutorBase.csx"
#load "Logger.csx"
#load "StampyParameters.csx"
using System;
using System.Diagnostics;
using System.IO;

public class DeploymentExecutor : ExecutorBase
{
    private List<string> _availableDeploymentTemplates;
    private List<string> AvailableDeploymentTemplates
    {
        get
        {
            if (_availableDeploymentTemplates == null)
            {
                var deploymentTemplatesDirectory = Path.Combine(StampyParameters.BuildPath, @"hosting\Azure\RDTools\Deploy\Templates");
                _availableDeploymentTemplates = Directory.GetFiles(deploymentTemplatesDirectory, "*.xml")
                    .Select(s => s.Split(new char[]{ Path.DirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
                    .ToList();
            }

            return _availableDeploymentTemplates;
        }
    }

    private string DeployConsolePath 
    {
        get
        {
            return Path.Combine(StampyParameters.BuildPath, @"HostingPrivate\tools\DeployConsole.exe");
        }
    }

    public DeploymentExecutor(StampyParameters stampyParameters) : base(stampyParameters){}

    public override void Execute(){
        var jobDirectory = Path.Combine(Environment.GetEnvironmentVariable("StampyJobResultsDirectoryPath"), StampyParameters.RequestId); 
        var logFilePath = Path.Combine(jobDirectory, "devdeploy", $"{StampyParameters.CloudName}_{StampyParameters.DeploymentTemplate}.log");
        var tempDirectory = Path.Combine(jobDirectory, "devdeploy", $"{StampyParameters.CloudName}_{StampyParameters.DeploymentTemplate}");

        if (!AvailableDeploymentTemplates.Contains(StampyParameters.DeploymentTemplate))
        {
            Logger.Info($"Deployment template `{StampyParameters.DeploymentTemplate}` does not exist");
        }

        if (!File.Exists(DeployConsolePath))
        {
            Logger.Info($"Cannot find {DeployConsolePath}");
        }

        Logger.Info("Starting deployment...");
        
        var processStartInfo = new ProcessStartInfo();
        processStartInfo.FileName = DeployConsolePath;
        processStartInfo.Arguments = $"/LockBox={StampyParameters.CloudName} /Template={StampyParameters.DeploymentTemplate} /BuildPath={StampyParameters.HostingPath} /TempDir={tempDirectory} /AutoRetry=true /LogFile={logFilePath}";
        processStartInfo.UseShellExecute = false;
        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        processStartInfo.RedirectStandardError = true;

        Logger.Info($"Start {processStartInfo.FileName} {processStartInfo.Arguments}");

        try{
            using(var deployProcess = Process.Start(processStartInfo))
            {
                deployProcess.BeginErrorReadLine();
                deployProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
                deployProcess.WaitForExit();
            }            
        }catch(Exception e){
            Logger.Error(e.Message, e);
            throw;
        }

        Logger.Info("Finished deployment...");
    }

    private void ErrorReceived(object sender, DataReceivedEventArgs e){
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            Logger.Error(e.Data);
        }
    }
}