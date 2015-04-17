using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PlaceLoader))]
public class PlaceManager : MonoBehaviour {

	AsyncOperation async;

	public Place currentPlace { get; private set; }
	public bool placeIsLoading { get; private set; }

	void LoadPlace(int levelIndex) {
		if (placeIsLoading)
			return;

		placeIsLoading = true;

		// Initiate async loading of a new scene.
		async = Application.LoadLevelAsync(levelIndex);
		async.allowSceneActivation = false;
		Application.backgroundLoadingPriority = ThreadPriority.Low;
	}

	IEnumerator ActivateLoadedPlaceCo() {
		if (async == null) {
			Debug.LogError("There is no inactive scene to activate!");
			yield break;
		}

		// Continue when the progress reaches 90%. It reaches max 90% before an 'allowSceneActivation' sets to TRUE.
		while (!async.isDone && async.progress < 0.9f)
			yield return null;
		
		OnLoadCompleteBeforeSceneActivation();
		
		// Activate the loaded scene.
		async.allowSceneActivation = true;
		yield return async;
		
		// Wait until a new place that comes with the new scene register itself in PlaceManager.
		while(currentPlace == null)
			yield return null;
		
		OnLoadCompleteAfterSceneActivation();
		
		placeIsLoading = false;
		async = null;
		yield break;
	}

	void OnLoadCompleteBeforeSceneActivation() {
		var player = King.visitor;
		
		// Get a local position in the exit lobby.
		player.localPositionAtPrevLobby = currentPlace.exitLobby.InverseTransformPoint(player.transform.position);
		
		LeanTween.cancel(player.gameObject);
	}

	void OnLoadCompleteAfterSceneActivation() {
		var player = King.visitor;

		// Put the player on the same local position in the new lobby as it was in the previous lobby.
		// It makes a transition from place to place seamless.
		player.transform.position = currentPlace.enterLobby.TransformPoint(player.localPositionAtPrevLobby);
		
		player.MoveTo(currentPlace.vantagePoint.transform.position);
	}

	public void NextPlace(bool manualActivation = false) {
		if (placeIsLoading)
			return;

		if (Application.loadedLevel + 1 < Application.levelCount) {
			LoadPlace(Application.loadedLevel + 1);
		} else {
			LoadPlace(1);
		}
		
		if (!manualActivation) {
			StartCoroutine(ActivateLoadedPlaceCo());
		}
	}

	public void ActivateLoadedPlace() {
		StartCoroutine(ActivateLoadedPlaceCo());
	}

	public void AddPlace(Place place) {
		currentPlace = place;
	}
}