﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Nexus.Client.Commands;
using Nexus.Client.Util;

namespace Nexus.Client.Games.Fallout4
{
	/// <summary>
	/// Launches Fallout4.
	/// </summary>
	public class Fallout4Launcher : GameLauncherBase
	{
		#region Constructors

		/// <summary>
		/// A simple constructor that initializes the object with the given dependencies.
		/// </summary>
		/// <param name="p_gmdGameMode">>The game mode currently being managed.</param>
		/// <param name="p_eifEnvironmentInfo">The application's envrionment info.</param>
		public Fallout4Launcher(IGameMode p_gmdGameMode, IEnvironmentInfo p_eifEnvironmentInfo)
			:base(p_gmdGameMode, p_eifEnvironmentInfo)
		{
		}

		#endregion

		/// <summary>
		/// Initializes the game launch commands.
		/// </summary>
		protected override void SetupCommands()
		{
			Trace.TraceInformation("Launch Commands:");
			Trace.Indent();

			ClearLaunchCommands();

			string strCommand = GetPlainLaunchCommand();
			Trace.TraceInformation("Plain Command: {0} (IsNull={1})", strCommand, (strCommand == null));
			Image imgIcon = File.Exists(strCommand) ? Icon.ExtractAssociatedIcon(strCommand).ToBitmap() : null;
			AddLaunchCommand(new Command("PlainLaunch", "Launch Fallout4", "Launches plain Fallout4.", imgIcon, LaunchFallout4Plain, true));

			strCommand = GetSkseLaunchCommand();
			Trace.TraceInformation("SKSE Command: {0} (IsNull={1})", strCommand, (strCommand == null));
			if (File.Exists(strCommand))
			{
				imgIcon = Icon.ExtractAssociatedIcon(strCommand).ToBitmap();
				AddLaunchCommand(new Command("F4seLaunch", "Launch F4SE", "Launches Fallout4 with F4SE.", imgIcon, LaunchFallout4SKSE, true));
			}

			strCommand = GetCustomLaunchCommand();
			Trace.TraceInformation("Custom Command: {0} (IsNull={1})", strCommand, (strCommand == null));
			imgIcon = File.Exists(strCommand) ? Icon.ExtractAssociatedIcon(strCommand).ToBitmap() : null;
			AddLaunchCommand(new Command("CustomLaunch", "Launch Custom Fallout4", "Launches Fallout4 with custom command.", imgIcon, LaunchFallout4Custom, true));

			DefaultLaunchCommand = new Command("Launch Fallout4", "Launches Fallout4.", LaunchGame);

			Trace.Unindent();
		}

		#region Launch Commands

		#region Custom Command

		/// <summary>
		/// Launches the game with a custom command.
		/// </summary>
		private void LaunchFallout4Custom()
		{
			ForceReadOnlyPluginsFile();
            Trace.TraceInformation("Launching Fallout4 (Custom)...");
			Trace.Indent();

			string strCommand = GetCustomLaunchCommand();
			string strCommandArgs = EnvironmentInfo.Settings.CustomLaunchCommandArguments[GameMode.ModeId];
			if (String.IsNullOrEmpty(strCommand))
			{
				Trace.TraceError("No custom launch command has been set.");
				Trace.Unindent();
				OnGameLaunched(false, "No custom launch command has been set.");
				return;
			}
			Launch(strCommand, strCommandArgs);
		}

		/// <summary>
		/// Gets the custom launch command.
		/// </summary>
		/// <returns>The custom launch command.</returns>
		private string GetCustomLaunchCommand()
		{
			string strCommand = EnvironmentInfo.Settings.CustomLaunchCommands[GameMode.ModeId];
			if (!String.IsNullOrEmpty(strCommand))
			{
				strCommand = Environment.ExpandEnvironmentVariables(strCommand);
				strCommand = FileUtil.StripInvalidPathChars(strCommand);
				if (!Path.IsPathRooted(strCommand))
					strCommand = Path.Combine(GameMode.GameModeEnvironmentInfo.InstallationPath, strCommand);
			}
			return strCommand;
		}

		#endregion

		#region SKSE

		/// <summary>
		/// Launches the game, with SKSE.
		/// </summary>
		private void LaunchFallout4SKSE()
		{
			ForceReadOnlyPluginsFile();
			Trace.TraceInformation("Launching Fallout4 (F4SE)...");
			Trace.Indent();

			string strCommand = GetSkseLaunchCommand();
			Trace.TraceInformation("Command: " + strCommand);

			if (!File.Exists(strCommand))
			{
				Trace.TraceError("F4SE does not appear to be installed.");
				Trace.Unindent();
				OnGameLaunched(false, "F4SE does not appear to be installed.");
				return;
			}
			Launch(strCommand, null);
		}

		/// <summary>
		/// Gets the SKSE launch command.
		/// </summary>
		/// <returns>The SKSE launch command.</returns>
		private string GetSkseLaunchCommand()
		{
			return Path.Combine(GameMode.GameModeEnvironmentInfo.InstallationPath, "f4se_loader.exe");
		}

		#endregion

		#region Vanilla Launch

		/// <summary>
		/// Launches the game, without OBSE.
		/// </summary>
		private void LaunchFallout4Plain()
		{
			ForceReadOnlyPluginsFile();
            Trace.TraceInformation("Launching Fallout4 (Plain)...");
			Trace.Indent();
			string strCommand = GetPlainLaunchCommand();
			Trace.TraceInformation("Command: " + strCommand);
			Launch(strCommand, null);
		}

		/// <summary>
		/// Gets the plain launch command.
		/// </summary>
		/// <returns>The plain launch command.</returns>
		private string GetPlainLaunchCommand()
		{
			string strCommand = Path.Combine(GameMode.GameModeEnvironmentInfo.InstallationPath, "Fallout4.exe");
			return strCommand;
		}

		#endregion

		/// <summary>
		/// Launches the game, using SKSE if present.
		/// </summary>
		private void LaunchGame()
		{
			ForceReadOnlyPluginsFile();

			if (!string.IsNullOrEmpty(EnvironmentInfo.Settings.CustomLaunchCommands[GameMode.ModeId]))
				LaunchFallout4Custom();
			else if (File.Exists(Path.Combine(GameMode.GameModeEnvironmentInfo.InstallationPath, "f4se_loader.exe")))
				LaunchFallout4SKSE();
			else
				LaunchFallout4Plain();
		}

		#endregion

		private void ForceReadOnlyPluginsFile()
		{
			Trace.TraceInformation("Setting plugins.txt to read-only");
			Trace.Indent();
			string strLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string strPluginsFilePath = Path.Combine(strLocalAppData, GameMode.ModeId, "plugins.txt");
			SetFileReadAccess(strPluginsFilePath, true);
		}

		/// <summary>
		/// Sets the read-only value of a file.
		/// </summary>
		private static void SetFileReadAccess(string p_strFileName, bool p_booSetReadOnly)
		{
			try
			{
				// Create a new FileInfo object.
				FileInfo fInfo = new FileInfo(p_strFileName);

				// Set the IsReadOnly property.
				fInfo.IsReadOnly = p_booSetReadOnly;
			}
			catch { }
		}
	}
}
