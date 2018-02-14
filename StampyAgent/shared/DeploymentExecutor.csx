//------------------------------------------------------------------------------
// <copyright file="DeploymentExecutor.csx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
#load "ExecutorBase.csx"
#load "Logger.csx"
#load "StampyParameters.csx"
#load "StampyResult.csx"
#load "JobResult.csx"
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

public class DeploymentExecutor : ExecutorBase
{
    private StampyResult _stampyResult;
    private StringBuilder _statusMessageBuilder;
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

    public DeploymentExecutor(StampyParameters stampyParameters) : base(stampyParameters){
        _stampyResult = new StampyResult();
        _stampyResult.BuildPath = StampyParameters.BuildPath;
        _stampyResult.CloudName = StampyParameters.CloudName;
        _stampyResult.DeploymentTemplate = StampyParameters.DeploymentTemplate;
        _stampyResult.RequestId = StampyParameters.RequestId;
        _statusMessageBuilder = new StringBuilder();
    }

    public override StampyResult Execute(){
        var jobDirectory = Path.Combine(Environment.GetEnvironmentVariable("StampyJobResultsDirectoryPath"), StampyParameters.RequestId); 
        var logFilePath = Path.Combine(jobDirectory, "devdeploy", $"{StampyParameters.CloudName}.{StampyParameters.DeploymentTemplate.Replace(".xml", string.Empty)}.log");
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DeployConsole", StampyParameters.CloudName);
        bool throwException = false;
        string exceptionMessage = "";

        if(!Directory.Exists(jobDirectory)){
            Directory.CreateDirectory(Directory.GetParent(logFilePath).FullName);
        }

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
        processStartInfo.RedirectStandardOutput = true;

        Logger.Info($"Start {processStartInfo.FileName} {processStartInfo.Arguments}");

        Process deployProcess;
        try{
            deployProcess = Process.Start(processStartInfo);
            deployProcess.BeginErrorReadLine();
            deployProcess.BeginOutputReadLine();
            deployProcess.OutputDataReceived += new DataReceivedEventHandler(OutputReceived);
            deployProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
            deployProcess.WaitForExit();           
        }catch(Exception e){
            Logger.Error(e.Message, e);
            throw;
        }

        if(deployProcess.ExitCode != 0){
            _stampyResult.Result = JobResult.Failed;
            _stampyResult.StatusMessage = _statusMessageBuilder.ToString();
            Logger.Error("Error while executing deployconsole.exe");
            Logger.Error(_stampyResult.StatusMessage);
            throwException = true;
            exceptionMessage = _stampyResult.StatusMessage;           
        }

        deployProcess.Dispose();

        if(throwException){
            throw new Exception(exceptionMessage);
        }

        Logger.Info("Finished deployment...");

        return _stampyResult;
    }

    private void OutputReceived(object sender, DataReceivedEventArgs e){
        if(!string.IsNullOrWhiteSpace(e.Data)){
            if(e.Data.Equals($"Deploy to {StampyParameters.CloudName} failed", StringComparison.CurrentCultureIgnoreCase)){
                _stampyResult.Result = JobResult.Failed;
            }
            
            if(e.Data.Contains("Total Time for Template")){
                _stampyResult.Result = JobResult.Passed;
            }
        }
    }

    private void ErrorReceived(object sender, DataReceivedEventArgs e){
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            _statusMessageBuilder.AppendLine(e.Data);
        }
    }
}