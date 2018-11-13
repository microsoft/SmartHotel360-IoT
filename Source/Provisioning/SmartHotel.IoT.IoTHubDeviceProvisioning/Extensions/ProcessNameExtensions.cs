using System;
using System.Diagnostics;

namespace SmartHotel.IoT.IoTHubDeviceProvisioning.Extensions
{
	public static class ProcessNameExtensions
	{
		public static ProcessExecutionResult ExecuteProcess( this string processFilename, string processArguments )
		{
			var startInfo = new ProcessStartInfo();
			startInfo.FileName = processFilename;
			startInfo.Arguments = processArguments;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			var process = Process.Start( startInfo );
			if ( process == null )
			{
				throw new InvalidOperationException( $"Unable to start process: {processFilename}" );
			}

			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			process.WaitForExit();

			return new ProcessExecutionResult
			{
				Output = output,
				Error = error,
				ExitCode = process.ExitCode
			};
		}
	}
}
