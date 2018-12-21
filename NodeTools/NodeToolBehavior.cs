using UnityEngine;

namespace NodeTools
{
    public class NodeToolBehavior : MonoBehaviour
    {
        public void Update(){
            if (Input.GetKeyUp(KeyCode.P))
            {
                NodeSelectionTool.instance.enabled = true;
                NodeSelectionTool.instance.Reset();
                Debug.Log("[NodeTools] Tool enabled: " + NodeSelectionTool.instance.enabled);
            }
        }
    }
}
