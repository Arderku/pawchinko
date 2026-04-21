using System;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// Plain data describing one plinko board's geometry. Consumed by the editor scene-build
    /// pass and by runtime helpers; never owns Unity references.
    /// </summary>
    [Preserve]
    [Serializable]
    public class BoardLayout
    {
        public int pegRows = 5;
        public int pegCols = 5;
        public float pegSpacingX = 0.5f;
        public float pegSpacingY = 0.6f;
        public int slotCount = 4;
        public float boardWidth = 4f;
        public float boardHeight = 6f;
        public float wallThickness = 0.2f;
    }
}
