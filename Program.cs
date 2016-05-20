using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace innoculus
{
	/// <summary>
	/// When started:
	/// * Look for Oculus Home
	///     * If exists, attach and wait
	///     * Else exit, unless /start when we should start service then Home
	/// * If Oculus Home ends, stop service
	/// </summary>
	public static class Program
	{
		private static bool _hasAdminRights = false;
		private static ProcessMonitor _monitor = null;
		private static OculusManager _mgr = null;
	    private static ILogger _logger= new Logger();

		public static void Main(params string[] args)
		{
			// Stop and disable services - assumes has admin rights
			if (args.Any(a => a.ToLower() == "/stop_service"))
			{
				DisableService(false);
				return;
			}

			try
			{
				CheckAdminRights();
				_logger.Log("Running with Admin privileges");
			}
			catch (System.Security.SecurityException)
			{
				_logger.Log("Running without Admin privileges");
			}

			_mgr = new OculusManager();

			// Look for Oculus Home running
			_monitor = new ProcessMonitor(OculusManager.OCULUS_PROCESS_NAME);

			if (_monitor.HasProcess)
			{
				_logger.Log("Found process: " + OculusManager.OCULUS_PROCESS_NAME);
			}
			else
			{
				_logger.Log("No process. Starting: " + _mgr.InstallationPath);
				DisableService(!_hasAdminRights);
				_mgr.StartOculusHome(_monitor);
			}

			if (_monitor.HasProcess)
			{
				_logger.Log("Waiting for process to exit");
				_monitor.ProcessExited += Monitor_ProcessExited;
				Application.Run();
			}
			else
			{
				_logger.Log("Exiting; No process: " + OculusManager.OCULUS_PROCESS_NAME);
			}
			_mgr.StopOculusHome();
		}

		[PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
		public static void CheckAdminRights()
		{
			_hasAdminRights = true;
		}

		public static void RestartWithAdmin(string args)
		{
			_logger.Log("Restarting process with permissions for: " + args);
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Application.ExecutablePath,
					Arguments = args,
					Verb = "runas"
				}
			};
			process.Start();
			System.Threading.Thread.Sleep(1000);
			process.WaitForExit();
			_logger.Log("Admin process complete");
		}

		private static void Monitor_ProcessExited(object sender, EventArgs e)
		{
			_logger.Log("Process exited");
			_mgr.StopOculusHome();
			Application.Exit();
		}

		private static void DisableService(bool needsAdmin)
		{
			_logger.Log("Stopping service: " + OculusManager.OCULUS_SERVICE_NAME);
			try
			{
				var svc = new ServiceController(OculusManager.OCULUS_SERVICE_NAME);
				if (svc.Status != ServiceControllerStatus.Stopped)
				{
					if (needsAdmin)
					{
						RestartWithAdmin("/stop_service");
					}
					else
					{
						svc.Stop();
						svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
					}
					if (svc.Status != ServiceControllerStatus.Running)
					{
						_logger.Log("Service stopped");
					}
					else
					{
						_logger.Log("Service not stopped");
					}
				}
			}
			catch (Exception ex)
			{
				_logger.Log("[ERROR] Failed to stop service: " + ex.Message);
			}
		}
	}
}
