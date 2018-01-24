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
        processStartInfo.Arguments = $"/LockBox={StampyParameters.CloudName} /Template={StampyParameters.DeploymentTemplate} /BuildPath={StampyParameters.HostingPath} /TempDir={Path.Combine(Path.GetTempPath(), StampyParameters.CloudName)} /AutoRetry=true";
        processStartInfo.UseShellExecute = false;
        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;

        try{
            using(var deployProcess = Process.Start(processStartInfo))
            {
                deployProcess.BeginOutputReadLine();
                deployProcess.BeginErrorReadLine();
                deployProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
                deployProcess.OutputDataReceived += new DataReceivedEventHandler(OutputReceived);
                deployProcess.WaitForExit();
            }            
        }catch(Exception e){
            Logger.Error(e.Message, e);
            throw;
        }
    }

    private void ErrorReceived(object sender, DataReceivedEventArgs e){
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            Logger.Error(e.Data);
        }
    }

    private void OutputReceived(object sender, DataReceivedEventArgs e){
        if(!string.IsNullOrWhiteSpace(e.Data)){
            Logger.Info(e.Data);
        }
    }
}