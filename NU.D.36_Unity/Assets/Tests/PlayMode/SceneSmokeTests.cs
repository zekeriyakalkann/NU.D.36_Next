using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// Smoke test: every scene in Build Settings loads and runs a few frames in Play Mode.
    /// Unity Test Framework fails the test on any logged error.
    /// </summary>
    public class SceneSmokeTests
    {
        const int FramesToRun = 10;

        // The Test Framework adds its own temporary InitTestScene to the build list while
        // Play Mode tests run; only project scenes are checked.
        const string TestRunnerScenePrefix = "Assets/InitTestScene";

        static IEnumerable<int> BuildSceneIndices()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                if (!SceneUtility.GetScenePathByBuildIndex(i).StartsWith(TestRunnerScenePrefix))
                    yield return i;
            }
        }

        [Test]
        public void BuildSettings_ContainsScenes()
        {
            Assert.That(SceneManager.sceneCountInBuildSettings, Is.GreaterThan(0), "No scenes in Build Settings");
        }

        [UnityTest]
        public IEnumerator BuildScenes_LoadAndRunWithoutErrors()
        {
            foreach (int index in BuildSceneIndices())
            {
                string path = SceneUtility.GetScenePathByBuildIndex(index);
                yield return SceneManager.LoadSceneAsync(index, LoadSceneMode.Single);
                Scene scene = SceneManager.GetActiveScene();
                Assert.IsTrue(scene.isLoaded, $"Scene {path} did not load");

                for (int i = 0; i < FramesToRun; i++)
                    yield return null;

                Assert.IsNotNull(Object.FindAnyObjectByType<Camera>(), $"Scene {path} has no camera");
                TestContext.WriteLine($"Loaded and ran {path}");
            }
        }
    }
}
