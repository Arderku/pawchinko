using System;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// Per-type behavior knobs that bias ball physics + outcome distribution. Movement and
    /// bounce styles are designer-facing tags consumed by future ball behavior strategies;
    /// variance and edgeBias are scalar hints (0..1) other systems can read directly.
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomTypeBehavior
    {
        public string movementStyle = string.Empty;
        public string bounceStyle = string.Empty;
        public string variance = string.Empty;
        public float edgeBias;
    }
}
