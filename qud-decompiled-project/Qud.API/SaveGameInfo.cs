using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XRL;

namespace Qud.API;

[Serializable]
public class SaveGameInfo
{
	public string ID;

	public string Name;

	public string Description;

	public string SaveTime;

	public string Info;

	public string Version;

	public SaveGameJSON json;

	public string Directory;

	public string Size;

	public List<string> ModsEnabled;

	public bool DifferentMods()
	{
		if (ModsEnabled == null)
		{
			ModsEnabled = new List<string>();
		}
		if ((ModManager.ActiveMods?.Count ?? 0) == ModsEnabled.Count)
		{
			return ModsEnabled.Union(ModManager.GetRunningMods()).Count() != ModsEnabled.Count;
		}
		return true;
	}

	public void Delete()
	{
		DataManager.DeleteSaveDirectory(Directory);
	}

	public async Task<bool> TryRestoreModsAndLoadAsync()
	{
		await The.UiContext;
		if (await The.Core.RestoreModsLoadedAsync(ModsEnabled))
		{
			await Task.Run(delegate
			{
				XRLGame.LoadGame(Path.Combine(Directory, "Primary"), Session: true, ShowPopup: true);
			});
			return true;
		}
		return false;
	}
}
