//------------------------------------------------------------------------------
// <copyright file="DeploymentExecutor.csx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
#r "StampyCommon.dll"
#load "ExecutorBase.csx"
#load "Logger.csx"
#load "StampyResult.csx"
#load "JobResult.csx"
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using StampyCommon;
using StampyCommon.Loggers;
using StampyCommon.SchedulerSettings;

public class DeploymentExecutor : ExecutorBase
{
    private StampyResult _stampyResult;
    private StringBuilder _statusMessageBuilder;
    private List<string> _availableDeploymentTemplates;
    private IStampyClientLogger _logger;
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

    private string _jobDirectory
    {
        get
        {
            var jobDirectory = Path.Combine(Environment.GetEnvironmentVariable("StampyJobResultsDirectoryPath"), StampyParameters.RequestId);
            if(!Directory.Exists(jobDirectory)){
                Directory.CreateDirectory(Path.GetDirectoryName(jobDirectory));
            }
            return jobDirectory;
        }
    }

    private string _azureLogFilePath
    {
        get
        {
            var logFilePath = Path.Combine(_jobDirectory, "devdeploy", $"{StampyParameters.CloudName}.{StampyParameters.DeploymentTemplate.Replace(".xml", string.Empty)}.log");
            var logDirectory = Path.GetDirectoryName(logFilePath);
            if(!Directory.Exists(logDirectory)){
                Directory.CreateDirectory(logDirectory);
            }
            return logFilePath;
        }
    }

    private string _deploymentArtificatsDirectory
    {
        get
        {
            return Path.Combine(Path.GetTempPath(), "DeployConsole", StampyParameters.CloudName);
        }
    }

    private string _logFilePath
    {
        get
        {
            return Path.Combine(Path.GetTempPath(), "DeployConsoleLogs", $"{StampyParameters.CloudName}.{StampyParameters.DeploymentTemplate.Replace(".xml", string.Empty)}.log");
        }
    }

    public DeploymentExecutor(StampyCommon.StampyParameters stampyParameters, IStampyClientLogger logger) : base(stampyParameters){
        _stampyResult = new StampyResult();
        _stampyResult.BuildPath = StampyParameters.BuildPath;
        _stampyResult.CloudName = StampyParameters.CloudName;
        _stampyResult.DeploymentTemplate = StampyParameters.DeploymentTemplate;
        _stampyResult.RequestId = StampyParameters.RequestId;
        _statusMessageBuilder = new StringBuilder();
        _logger = logger;
    }

    public override StampyResult Execute(){
        bool throwException = false;
        string exceptionMessage = "";

        if (!AvailableDeploymentTemplates.Contains(StampyParameters.DeploymentTemplate))
        {
            _logger.WriteInfo(StampyParameters, $"Deployment template `{StampyParameters.DeploymentTemplate}` does not exist");
        }

        if (!File.Exists(DeployConsolePath))
        {
            _logger.WriteInfo(StampyParameters, $"Cannot find {DeployConsolePath}");
        }

        _logger.WriteInfo(StampyParameters, "Starting deployment...");
        
        var processStartInfo = new ProcessStartInfo();
        processStartInfo.FileName = DeployConsolePath;
        processStartInfo.Arguments = $"/LockBox={StampyParameters.CloudName} /Template={StampyParameters.DeploymentTemplate} /BuildPath={StampyParameters.BuildPath + @"\Hosting"} /TempDir={_deploymentArtificatsDirectory} /AutoRetry=true /LogFile={_azureLogFilePath}";
        processStartInfo.UseShellExecute = false;
        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        processStartInfo.RedirectStandardError = true;
        processStartInfo.RedirectStandardOutput = true;

        _logger.WriteInfo(StampyParameters, $"Start {processStartInfo.FileName} {processStartInfo.Arguments}");

        Process deployProcess;
        try{
            deployProcess = Process.Start(processStartInfo);
            deployProcess.BeginErrorReadLine();
            deployProcess.BeginOutputReadLine();
            deployProcess.OutputDataReceived += new DataReceivedEventHandler(OutputReceived);
            deployProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
            deployProcess.WaitForExit();           
        }catch(Exception e){
            _logger.WriteError(StampyParameters, "Failed while running deployment process", e);
            throw;
        }

        if(deployProcess.ExitCode != 0){
            _stampyResult.Result = JobResult.Failed;
            _stampyResult.StatusMessage = _statusMessageBuilder.ToString();
            _logger.WriteInfo(StampyParameters, "Error while executing deployconsole.exe " + _stampyResult.StatusMessage);
            throwException = true;
            exceptionMessage = _stampyResult.StatusMessage;           
        }

        deployProcess.Dispose();

        if(throwException){
            throw new Exception(exceptionMessage);
        }

        _logger.WriteInfo(StampyParameters, "Finished deployment...");

        return _stampyResult;
    }

    private void OutputReceived(object sender, DataReceivedEventArgs e){
        if(!string.IsNullOrWhiteSpace(e.Data)){
            if(e.Data.Equals($"Deploy to {StampyParameters.CloudName} failed", StringComparison.CurrentCultureIgnoreCase)){
                _stampyResult.Result = JobResult.Failed;
            }
            else if(e.Data.Contains("Total Time for Template")){
                _stampyResult.Result = JobResult.Passed;
            }
        }
    }

    private void ErrorReceived(object sender, DataReceivedEventArgs e){
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            Logger.Info(e.Data);
            _statusMessageBuilder.AppendLine(e.Data);
        }
    }
}