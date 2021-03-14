using UnityEditor;

namespace UnityEngine.Experimental.TerrainAPI
{
	public class TerrainGizmos : MonoBehaviour
	{
		public int GroupID = 0;
        [HideInInspector]
        public Color CubeColor, CubeWireColor; 

        void OnDrawGizmos()
		{
            transform.rotation = new Quaternion(0, 0, 0, 0);
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), Mathf.Abs(transform.localScale.z));
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.color = CubeColor;
			Gizmos.DrawCube(Vector3.zero, Vector3.one);
			Gizmos.color = CubeWireColor;
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

#if UNITY_EDITOR
			//Disable rotate tool
			if(Selection.Contains(this.gameObject))
            {
                Tools.hidden = Tools.current == Tool.Rotate ? true : false;
            }
            else
            {
                Tools.hidden = false;
            }
#endif
		}
    }
}
