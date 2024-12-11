namespace Services;

using System.Diagnostics;
using System.Management;
using Models.CommandInterface;
using Helpers.ResponseHelper;
#pragma warning disable CA1416 


public class ProcessUtilities
{
    private readonly string _localPath;

    public ProcessUtilities()
    {
        _localPath = LocalPathFinder.MicrosipPath();
    }
    public int isThread = 1;

    public IResult MakeCall(CommandInterface callData)
    {
        if (string.IsNullOrEmpty(callData.Phone) || callData.Phone == "0")
        {
            return ResponseHelper.ResponseStatus("Phone is required", 400);
        }

        if (callData.DealId == 0 && callData.ContactId == 0)
        {
            return ResponseHelper.ResponseStatus("CallId or ContactId is required", 400);
        }

        try
        {
            var processCalled = Process.Start("cmd.exe", $"/c {_localPath} {callData.Phone}");
            if (!string.IsNullOrEmpty(_localPath))
            {
                MonitorProcess(callData);
                return ResponseHelper.ResponseStatus("Call made",200);
            }

            return ResponseHelper.ResponseStatus("The LocalPath is Null",400);
        }
        catch (Exception ex)
        {
            return ResponseHelper.ResponseStatus(ex.Message,400);
        }
    }

    private void MonitorProcess(CommandInterface callData)
    {
        int pid = GetMicrosipPid();
        if (pid == -1){
             ResponseHelper.ResponseStatus("Pid Is Null",400);
             return;
        }

        string pidString = pid.ToString();

        ManagementEventWatcher watcher = new ManagementEventWatcher(
            new WqlEventQuery($"SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Thread' AND TargetInstance.ProcessHandle = '{pidString}'")
        );

        watcher.EventArrived += new EventArrivedEventHandler(async (sender, e) => await HandleEvent(sender, e, callData));
        watcher.Start();
    }

    private int GetMicrosipPid()
    {
        Process[] processes = Process.GetProcessesByName("microsip");
        bool processExist = processes.Length > 0;
        return processExist ? processes[0].Id : -1;
    }

    public async Task HandleEvent(object sender, EventArrivedEventArgs e, CommandInterface CallData)
    {
        isThread++;
        if (isThread == 2)
        {
             await RecordingService.SendTheArchive(CallData);
             ResponseHelper.ResponseStatus("end of call",200);
             return;
        }
}

}
