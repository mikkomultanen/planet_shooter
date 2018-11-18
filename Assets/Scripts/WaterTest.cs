using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WaterTest : MonoBehaviour {
	public WaterSystem waterSystem;
	public int count = 5;
	public float radius = 1f;
	private Camera _camera;
	void Start () {
		_camera = GetComponent<Camera>();
	}
	private void Update() {
		if(Input.GetMouseButton(0)) {
			Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
			Vector3 viewPos = _camera.ScreenToViewportPoint(mousePos);
			if (viewPos.x > 0 && viewPos.x < 1 && viewPos.y > 0 && viewPos.y < 1) {
				Vector3 wordPos = _camera.ScreenToWorldPoint(mousePos);
				for (int i = 0; i < count; i++) {
					Vector3 position = wordPos + UnityEngine.Random.insideUnitSphere * radius;
					position.z = 0f;
					waterSystem.Emit(position);
				}
			}
		}
	}
}
