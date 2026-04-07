using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ModAudioEntry : AudioEntry
{
	public async UniTask<AudioClip> SendURIRequest()
	{
		AudioType audioTypeFromFile = SoundManager.GetAudioTypeFromFile(Path);
		using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(Path, audioTypeFromFile))
		{
			await www.SendWebRequest();
			if (www.result != UnityWebRequest.Result.ConnectionError)
			{
				return DownloadHandlerAudioClip.GetContent(www);
			}
			MetricsManager.LogError(www.error);
		}
		return null;
	}

	public override async UniTask<AudioClip> GetClip()
	{
		if ((object)Clip != null)
		{
			return Clip;
		}
		if (AudioEntry.Tasks.TryGetValue(this, out var value))
		{
			return (AudioClip)(await value);
		}
		UniTaskCompletionSource<Object> source = new UniTaskCompletionSource<Object>();
		AudioEntry.Tasks.Add(this, source.Task);
		Clip = await SendURIRequest();
		source.TrySetResult(Clip);
		AudioEntry.Tasks.Remove(this);
		return Clip;
	}
}
