using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour {

	public GameObject loadingScreen;
	public Slider loadingProgress;

	private bool isLoading = false;

	public void quitApplication() {
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#else
		Application.Quit();
		#endif
	}

	public void loadScene(string sceneName) {
		if (!isLoading) {
			isLoading = true;
			SceneManager.LoadScene(sceneName);
		}
	}

	public void loadSceneAsync(string sceneName) {
		if (!isLoading) {
			isLoading = true;
			if (loadingScreen != null) {
				loadingScreen.SetActive(true);
			}
			StartCoroutine(LoadYourAsyncScene(sceneName));
		}
	}

	IEnumerator LoadYourAsyncScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        //Wait until the last operation fully loads to return anything
        while (!asyncLoad.isDone)
        {
			if (loadingProgress != null) {
				loadingProgress.value = Mathf.Clamp01(asyncLoad.progress / 0.9f);
			}
            yield return null;
        }
    }
}
