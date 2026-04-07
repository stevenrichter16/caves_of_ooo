public static class Interface
{
	public static string GetPrintableKeyForCommand(string command)
	{
		if (command == "Accept")
		{
			return "space";
		}
		if (command == "Cancel")
		{
			return "escape";
		}
		return command;
	}
}
