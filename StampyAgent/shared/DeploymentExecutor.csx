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

    private string _jobDirectory
    {
        get
        {
            return Path.Combine(Environment.GetEnvironmentVariable("StampyJobResultsDirectoryPath"), StampyParameters.RequestId);
        }
    }

    private string _azureLogFilePath
    {
        get
        {
            return Path.Combine(_jobDirectory, "devdeploy", $"{StampyParameters.CloudName}.{StampyParameters.DeploymentTemplate.Replace(".xml", string.Empty)}.log");
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
            return Path.Combine(_deploymentArtificatsDirectory, $"{StampyParameters.CloudName}.{StampyParameters.DeploymentTemplate.Replace(".xml", string.Empty)}.log");
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
        bool throwException = false;
        string exceptionMessage = "";

        if(!Directory.Exists(_jobDirectory)){
            Directory.CreateDirectory(Directory.GetParent(_logFilePath).FullName);
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
        processStartInfo.Arguments = $"/LockBox={StampyParameters.CloudName} /Template={StampyParameters.DeploymentTemplate} /BuildPath={StampyParameters.HostingPath} /TempDir={_deploymentArtificatsDirectory} /AutoRetry=true /LogFile={_logFilePath}";
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
            else if(e.Data.Contains("Total Time for Template")){
                _stampyResult.Result = JobResult.Passed;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_azureLogFilePath));
            File.AppendAllText(_azureLogFilePath, e.Data, Encoding.UTF8);
        }
    }

    private void ErrorReceived(object sender, DataReceivedEventArgs e){
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            _statusMessageBuilder.AppendLine(e.Data);
        }
    }
}