using System;
using System.IO;
using XRL;

public class SimpleFileLogger
{
	private string filePath;

	public SimpleFileLogger(string file_name)
	{
		filePath = DataManager.SavePath(file_name);
		if (File.Exists(filePath))
		{
			File.Copy(filePath, filePath + ".prev", overwrite: true);
		}
		File.WriteAllText(filePath, "--log start--\n");
	}

	public void AppendTimestamp()
	{
		File.AppendAllText(filePath, "[" + DateTime.Now.ToString("s") + "] ");
	}

	public void Info(string log)
	{
		AppendTimestamp();
		File.AppendAllText(filePath, log + "\n");
	}

	public void Error(string log)
	{
		AppendTimestamp();
		File.AppendAllText(filePath, log + "\n");
	}
}
