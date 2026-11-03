using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 2 integration: Rota02 runs in Play Mode with Unity driving Update/LateUpdate.
    /// The scene is loaded by name, so the result does not depend on Build Settings order.
    /// </summary>
    public class Lab2PlayModeTests
    {
        const string SceneName = "Rota02";
        const float TimeoutSeconds = 20f;

        HedefeGit git;
        KanatYatisi yatis;
        Radar radar;

        [UnitySetUp]
        public IEnumerator LoadRota02()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            git = Object.FindAnyObjectByType<HedefeGit>();
            yatis = Object.FindAnyObjectByType<KanatYatisi>();
            radar = Object.FindAnyObjectByType<Radar>();
            Assert.IsNotNull(git, "HedefeGit missing");
            Assert.IsNotNull(yatis, "KanatYatisi missing");
            Assert.IsNotNull(radar, "Radar missing");
        }

        [UnityTest]
        public IEnumerator Aircraft_ReachesBalonAndStops()
        {
            float elapsed = 0f;
            while (!git.HedefeVardi && elapsed < TimeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(git.HedefeVardi, $"Did not reach Balon within {TimeoutSeconds} s");
            TestContext.WriteLine($"Arrived after {elapsed:F2} s at {git.transform.position}");

            Vector3 stopped = git.transform.position;
            for (int i = 0; i < 10; i++)
                yield return null;
            Assert.AreEqual(stopped, git.transform.position, "Aircraft must stay still within the arrival distance");
        }

        [UnityTest]
        public IEnumerator Aircraft_BanksRightDuringInitialTurn()
        {
            // Balon (40, 30, 60) is to the right of the initial heading (+Z).
            for (int i = 0; i < 30; i++)
                yield return null;

            Transform gorsel = yatis.Gorsel;
            float rightY = gorsel.TransformPoint(new Vector3(4.87f, 0f, 0f)).y;
            float leftY = gorsel.TransformPoint(new Vector3(-4.87f, 0f, 0f)).y;
            Assert.That(yatis.YatisAcisi, Is.GreaterThan(0f));
            Assert.That(rightY, Is.LessThan(leftY), "Right wing must be lower while turning right");
        }

        [UnityTest]
        public IEnumerator Radar_SeesAircraftOnlyAfterItEntersTheFieldOfView()
        {
            yield return null;
            // Start: aircraft directly above the radar (90° from forward).
            Assert.IsFalse(radar.HedefGorunur, "Aircraft at (0, 20, 0) is 90° off the radar's forward axis");

            float elapsed = 0f;
            while (!git.HedefeVardi && elapsed < TimeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return null;
            Assert.IsTrue(radar.HedefGorunur, $"Aircraft near Balon should be visible; position {git.transform.position}");
        }
    }
}
