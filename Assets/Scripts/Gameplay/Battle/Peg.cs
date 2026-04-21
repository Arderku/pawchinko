using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Static peg marker. Holds row/col so future modifier systems can index pegs cheaply.
    /// All physics behaviour comes from the SphereCollider + PhysicsMaterial on the same GO.
    /// </summary>
    public class Peg : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private int row;
        [SerializeField] private int col;

        public int Row => row;
        public int Col => col;

        /// <summary>
        /// Sets the peg's grid coordinates. Called once during board construction.
        /// </summary>
        public void SetCoords(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
    }
}
