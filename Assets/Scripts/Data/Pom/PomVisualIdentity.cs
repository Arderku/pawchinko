using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Pawchinko
{
    /// <summary>
    /// Designer-facing visual hints for a Pom or type. String tags rather than asset refs so
    /// the data layer never depends on UnityEngine art types; art bindings live on prefabs.
    /// </summary>
    [Preserve]
    [Serializable]
    public class PomVisualIdentity
    {
        public List<string> mainColors = new();
        public List<string> effects = new();
    }
}
