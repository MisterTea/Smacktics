using Godot;
using System;
using System.Runtime.CompilerServices;

public class GodotGlobals : G
{
	private GodotGlobals(GameSettings settings) : base(settings)
	{
	}
	
	public override SystemException exception(string message,
	[CallerMemberName] string callingMethod = "",
		[CallerFilePath] string callingFilePath = "",
		[CallerLineNumber] int callingFileLineNumber = 0)
	{
		GD.PushError($"{message} ({callingFilePath}:{callingFileLineNumber})");
		GD.Print($"{message} ({callingFilePath}:{callingFileLineNumber})");
		return base.exception(message, callingMethod, callingFilePath, callingFileLineNumber);
	}

	public override void log(string message,
	[CallerMemberName] string callingMethod = "",
		[CallerFilePath] string callingFilePath = "",
		[CallerLineNumber] int callingFileLineNumber = 0)
	{
		GD.Print($"{message} ({callingFilePath}:{callingFileLineNumber})");
	}
	
	public static new G create(GameSettings settings)
	{
		G.i = new GodotGlobals(settings);
		return G.i;
	}
}
