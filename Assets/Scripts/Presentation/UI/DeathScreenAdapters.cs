using UnityEngine;
using UnityEngine.SceneManagement;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Production <see cref="ISceneRestarter"/> wrapping
    /// <see cref="SceneManager.LoadScene(int)"/> on the currently
    /// active scene's build index. Reloads the same scene from
    /// scratch — re-runs <c>GameBootstrap.Awake/Start</c>.
    /// Stateless; safe as a static singleton.
    /// </summary>
    internal sealed class SceneRestarterAdapter : ISceneRestarter
    {
        public void Restart()
        {
            int idx = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(idx);
        }
    }
}
