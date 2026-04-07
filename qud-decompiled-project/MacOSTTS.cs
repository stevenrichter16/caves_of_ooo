using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using UnityEngine;

public class MacOSTTS : MonoBehaviour
{
	public static MacOSTTS instance;

	private bool m_IsSpeaking;

	private Process m_VoiceProcess;

	private void Start()
	{
		if (instance == null)
		{
			instance = this;
			return;
		}
		UnityEngine.Debug.LogError("[Accessibility] Trying to create another MacOS TTS instance, when there already is one.");
		UnityEngine.Object.DestroyImmediate(base.gameObject);
	}

	public void Speak(string msg)
	{
		if (msg.Length != 0)
		{
			Stop();
			m_IsSpeaking = true;
			StartCoroutine("SpeakText", msg);
		}
	}

	private IEnumerator SpeakText(string textToSpeak)
	{
		textToSpeak = textToSpeak.Replace('"', '\'');
		int num = (int)((float)UAP_AccessibilityManager.GetSpeechRate() / 100f * 175f * 2f);
		string arguments = "-r " + num + " \"" + textToSpeak + "\"";
		m_VoiceProcess = new Process();
		m_VoiceProcess.StartInfo.FileName = "say";
		m_VoiceProcess.StartInfo.Arguments = arguments;
		m_VoiceProcess.StartInfo.CreateNoWindow = true;
		m_VoiceProcess.StartInfo.RedirectStandardOutput = true;
		m_VoiceProcess.StartInfo.RedirectStandardError = true;
		m_VoiceProcess.StartInfo.UseShellExecute = false;
		m_VoiceProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
		Thread worker = new Thread((ThreadStart)delegate
		{
			WaitForVoiceToFinish(m_VoiceProcess);
		})
		{
			Name = "UAP_TTS_Proc"
		};
		worker.Start();
		do
		{
			yield return null;
		}
		while (worker.IsAlive);
		m_IsSpeaking = false;
		m_VoiceProcess = null;
	}

	private void WaitForVoiceToFinish(Process process)
	{
		try
		{
			process.Start();
			process.WaitForExit();
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError("[Accessibility] TTS Error: " + ex);
		}
	}

	public void Stop()
	{
		if (m_IsSpeaking && m_VoiceProcess != null)
		{
			if (!m_VoiceProcess.HasExited)
			{
				m_VoiceProcess.Kill();
				m_VoiceProcess = null;
			}
			m_IsSpeaking = false;
			StopCoroutine("SpeakText");
		}
	}

	public bool IsSpeaking()
	{
		if (!Application.isPlaying)
		{
			return false;
		}
		return m_IsSpeaking;
	}

	private void OnDestroy()
	{
		Stop();
	}
}
