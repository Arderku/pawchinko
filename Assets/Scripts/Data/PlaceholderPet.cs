using System;

namespace Pawchinko
{
    /// <summary>
    /// Placeholder team-roster entry used until real Paw / PawDefinition creature data exists.
    /// Carries only what the temp HUD needs to render the active-pet row + card.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    [Serializable]
    public class PlaceholderPet
    {
        public string petName;
        public int level;

        public PlaceholderPet() { }

        public PlaceholderPet(string petName, int level)
        {
            this.petName = petName;
            this.level = level;
        }
    }
}
