using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This component is used to create a patrol path, two points which enemies will move between.
    /// </summary>
    public partial class PatrolPath : MonoBehaviour
    {
        /// <summary>
        /// One end of the patrol path.
        /// </summary>
        public Vector2 startPosition, endPosition;

        /// <summary>
        /// Create a Mover instance which is used to move an entity along the path at a certain speed.
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public Mover CreateMover(float speed)
        { 
            return new Mover(this, speed); 
        }

        // CUSTOM ADDITION SO OPTIONAL PARAMETER CAN BE REMOVED FROM METHOD ABOVE
        // AND CREAM PARSE WONT FAIL
        /// <summary>
        /// Create a Mover instance which is used to move an entity along the path at a certain speed.
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public Mover CreateMover()
        {
            return new Mover(this, 1);
        }

        void Reset()
        {
            startPosition = Vector3.left;
            endPosition = Vector3.right;
        }
    }
}