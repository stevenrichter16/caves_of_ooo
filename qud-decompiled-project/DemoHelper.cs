using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoHelper : MonoBehaviour
{
	public void EnableObject()
	{
		base.gameObject.SetActive(value: true);
	}

	public void DisableObject()
	{
		base.gameObject.SetActive(value: false);
	}

	public void LoadNextScene(int sceneIndex)
	{
		SceneManager.LoadScene(sceneIndex);
	}
}
