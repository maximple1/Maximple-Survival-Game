using System;
using System.Collections.Generic;
using System.Linq;
using SETUtil;
using SETUtil.Common.Extend;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace EnvSpawn
{
	[AddComponentMenu("Kris Development/Enviro Spawn CS")]
	public class EnviroSpawn_CS : MonoBehaviour
	{
		public enum ScatterMode
		{
			Random = 0,
			FixedGrid = 1,
			EqualSpread = 2,
		}

		[Serializable]
		public class PositioningParams
		{
			public Vector2 point;
			public float rotationAngle;
			public float scale;
			public GameObject gameObjectBinding;
		}

		public Color gizmoColor = new Color(0, 0, 1);
		
		[Header("Surface Interaction")]
		public LayerMask ignoreMask = 0;
		public LayerMask avoidMask = 0;
		public float offset = 0;
		public bool followNormalsOrientation = true;
		
		[Header("Generation Settings")]
		[FormerlySerializedAs("population")] public int density = 1;
		public Vector2 dimensions = new Vector2(2, 2);
		public Vector2 scaleVariation = new Vector2(0.5f, 1.5f);
		public Vector2 rotationVariation = new Vector2(0, 360);

		[HideInInspector] public ScatterMode scatterMode = ScatterMode.Random; //random, fixed, equal
		[HideInInspector] public float fixedGridScale = 1;

		[HideInInspector] public List<PositioningParams> positioningParams = new List<PositioningParams>();
		[HideInInspector] public List<GameObject> prefabs = new List<GameObject>();

		void OnDrawGizmos()
		{
			Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
			Gizmos.matrix = rotationMatrix;

			Gizmos.color = gizmoColor;
			Gizmos.DrawWireCube(Vector3.zero, new Vector3(dimensions.x * transform.localScale.x, 0.1f, dimensions.y * transform.localScale.z));
			Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.1f);
			Gizmos.DrawCube(Vector3.zero, new Vector3(dimensions.x * transform.localScale.x, 0.1f, dimensions.y * transform.localScale.z));

#if UNITY_EDITOR
			if (Selection.Contains(gameObject)) {
				if (Application.isEditor) {
					UpdateData();
				}
			}
#endif
		}

		public void Generate()
		{
			GeneratePositionings();
			UpdateData();
		}

		public void MassInstantiateNew()
		{
			foreach (var _obj in FindObjectsOfType<EnviroSpawn_CS>()) {
				_obj.Generate();
			}
		}

		private void GeneratePositionings()
		{
			Clear();
			GenerateRaycastPositionings();

			foreach (var p in positioningParams) {
				if (!CreateAndBindInstance(p)) {
					break;
				}
			}

			Dirtify();
		}

		private bool CreateAndBindInstance(PositioningParams pstParams)
		{
			var _valid = prefabs.Where(a => a != null).ToList();
			if (_valid.Count == 0) {
				return false;
			}

			var _prefab = _valid.SelectRandom();
			if (_prefab == null) {
				return false;
			}

			var _instance = SceneUtil.Instantiate(_prefab);
			_instance.hideFlags = HideFlags.NotEditable;

			_instance.transform.SetParent(transform);
			pstParams.gameObjectBinding = _instance;

			pstParams.scale = Random.Range(scaleVariation.x, scaleVariation.y);
			pstParams.rotationAngle = Random.Range(rotationVariation.x, rotationVariation.y);
			return true;
		}

		private void UpdateData()
		{
			if (positioningParams == null) {
				return;
			}

			ValidatePositioningInstances();

			// this prevents bubbling of instances with colliders
			positioningParams.ForEach(a => {
				if (a.gameObjectBinding != null) {
					a.gameObjectBinding.SetActive(false);
				}

				Dirtify(a.gameObjectBinding);
			});
			
			var _toReEnable = new List<GameObject>();
			

			foreach (var _positioning in positioningParams) {
				RaycastHit _hit;

				if (_positioning.gameObjectBinding == null) {
					continue;
				}

				if (Physics.Raycast(
					transform.position + transform.right * _positioning.point.x +
					transform.forward * _positioning.point.y, Vector3.down, out _hit, Mathf.Infinity, ~(ignoreMask))) {
					// don't spawn if surface's layer is to be avoided
					if (!((int) avoidMask).ContainsFlag(1 << _hit.transform.gameObject.layer)) {
						var _tr = _positioning.gameObjectBinding.transform;
						_tr.position = _hit.point + (offset * (followNormalsOrientation ? _hit.normal : Vector3.up));
						_tr.rotation = followNormalsOrientation ? Quaternion.FromToRotation(Vector3.up, _hit.normal) : Quaternion.identity;
						_tr.localScale = Vector3.one * _positioning.scale;

						_tr.RotateAround(_tr.position, _tr.up, _positioning.rotationAngle);
						_toReEnable.Add(_positioning.gameObjectBinding);
					}
				}
			}

			_toReEnable.ForEach(a => {
				if (a != null) {
					a.SetActive(true);
				}
				
				Dirtify(a);
			});
		}

		private void GenerateRaycastPositionings()
		{
			float _x = dimensions.x;
			float _y = dimensions.y;
			int _lc = 0; //loop count

			positioningParams.Clear();

			if (scatterMode == ScatterMode.Random) {
				// generate random scatter
				for (uint r = 0; r < density; r++) {
					positioningParams.Add(new PositioningParams() {
						point = GenerateRandomRaycastPositioning()
					});
				}
			} else if (scatterMode == ScatterMode.FixedGrid) {
				float tp = (float) density; //r
				float c = tp / ((float) _x * _y); //expected cycles

				_lc = 0;

				for (uint cn = 0; cn < c; cn++) //na - cycle number
				{
					// float localCellOffset = fixedGridScale / c * cn; //p
					for (uint _ay = 0; _ay < _y; _ay++) {
						for (uint _ax = 0; _ax < _x; _ax++) {
							if (_lc < density) {
								positioningParams.Add(new PositioningParams() {
									point = new Vector2(_ax * fixedGridScale - _x / 2 + fixedGridScale / 2, _ay * fixedGridScale - _y / 2 + fixedGridScale / 2),
								});
							}

							_lc++;
						}
					}
				}
			} else if (scatterMode == ScatterMode.EqualSpread) {
				int _a = (int) Mathf.Sqrt(((float) density) * dimensions.x / dimensions.y); //horizontal cell count
				int _b = density / _a; //vertical cell count
				_lc = 0;

				for (uint a1 = 0; a1 < _a; a1++) {
					for (uint b1 = 0; b1 < _b; b1++) {
						positioningParams.Add(new PositioningParams() {
							point = new Vector2(dimensions.x / _a * a1 - (dimensions.x / 2 - dimensions.x / _a / 2), dimensions.y / _b * b1 - (dimensions.y / 2 - dimensions.y / _b / 2)),
						});
						_lc++;
					}
				}
			}

			Dirtify();
		}

		private Vector2 GenerateRandomRaycastPositioning()
		{
			//get flat plane positions for each population id
			var _localScale = transform.localScale;
			Vector2 _rayPos = new Vector2(Random.Range(-dimensions.x / 2 * _localScale.x, dimensions.x / 2 * _localScale.z)
				, Random.Range(-dimensions.y / 2 * _localScale.x, dimensions.y / 2 * _localScale.z));
			return _rayPos;
		}

		/// <summary>
		/// Validate for missing and unlisted instances
		/// </summary>
		private void ValidatePositioningInstances()
		{
			// check if action needs be taken
			int _childCount = transform.childCount;
			int _positioningsCount = positioningParams.Count;
			bool _missingInstancesDetected = false;

			foreach (var p in positioningParams) {
				if (p.gameObjectBinding == null) {
					_missingInstancesDetected = true;
					break;
				}
			}

			if (_childCount == _positioningsCount && !_missingInstancesDetected) {
				return;
			}

			// collect children
			var _childrenPool = new List<GameObject>();
			foreach (Transform c in transform) {
				_childrenPool.Add(c.gameObject);
			}

			// fix bindings in positionings
			foreach (var p in positioningParams) {
				if (_childrenPool.Count > 0) {
					// binding existing
					if (p.gameObjectBinding != null) {
						_childrenPool.Remove(p.gameObjectBinding);
					} else {
						var _ch = _childrenPool.FirstOrDefault();
						p.gameObjectBinding = _ch;
						_childrenPool.Remove(_ch);
					}
				} else {
					// instantiate new
					if (!CreateAndBindInstance(p)) {
						break;
					}
				}
			}

			// clear excess children
			foreach (var c in _childrenPool) {
				SceneUtil.SmartDestroy(c);
			}

			Dirtify();
		}

		public void Clear()
		{
			foreach (Transform _child in transform) {
				SceneUtil.SmartDestroy(_child.gameObject);
			}

			positioningParams.Clear();
			Dirtify();
		}

		private void Dirtify(Object other = null)
		{
#if UNITY_EDITOR
			EditorUtility.SetDirty(other != null ? other : this);
#endif
		}
	}
}