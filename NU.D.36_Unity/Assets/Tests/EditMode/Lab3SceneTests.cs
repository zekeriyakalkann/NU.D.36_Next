using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NU36.Tests
{
    /// <summary>Lab 3: Rota03 configuration, and regression checks that Rota02 is untouched.</summary>
    public class Lab3SceneTests
    {
        const string Rota02Path = "Assets/Scenes/Rota02.unity";
        const string Rota03Path = "Assets/Scenes/Rota03.unity";
        const float Tolerance = 1e-4f;

        Scene scene;

        Scene Open(string path)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            return scene;
        }

        [TearDown]
        public void Close()
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        GameObject Root(string name) => scene.GetRootGameObjects().Single(g => g.name == name);

        static float Float(Object o, string field) => new SerializedObject(o).FindProperty(field).floatValue;

        // A null reference with a non-zero id is a missing (broken) reference.
        static bool IsDefault<T>(T value) => System.Collections.Generic.EqualityComparer<T>.Default.Equals(value, default);

        [Test]
        public void BuildSettings_AppendRota03AndKeepEarlierOrder()
        {
            string[] paths = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            // Later labs append scenes (Rota04); the Lab 3 order must stay a prefix.
            CollectionAssert.AreEqual(new[] { Rota02Path, "Assets/Scenes/SampleScene.unity", Rota03Path }, paths.Take(3).ToArray());
            Assert.That(EditorBuildSettings.scenes.All(s => s.enabled));
        }

        [Test]
        public void Rota03_ObjectsAndTransformsMatchLab2()
        {
            Open(Rota03Path);
            Assert.That((Root("Uçak").transform.position - new Vector3(0f, 20f, 0f)).magnitude, Is.LessThan(Tolerance));
            Assert.That((Root("Balon").transform.position - new Vector3(40f, 30f, 60f)).magnitude, Is.LessThan(Tolerance));
            Assert.That((Root("Balon").transform.localScale - Vector3.one * 6f).magnitude, Is.LessThan(Tolerance));
            Assert.That((Root("Uçaksavar").transform.forward - Vector3.forward).magnitude, Is.LessThan(Tolerance));
            Assert.AreSame(Root("Uçak").transform, Root("Uçaksavar").GetComponent<Radar>().Hedef);
        }

        [Test]
        public void Rota03_Lab2AutomationIsKeptButDisabled()
        {
            Open(Rota03Path);
            GameObject ucak = Root("Uçak");
            Assert.IsNotNull(ucak.GetComponent<HedefeGit>(), "HedefeGit must stay on the aircraft");
            Assert.IsFalse(ucak.GetComponent<HedefeGit>().enabled, "HedefeGit must not steer in Rota03");
            Assert.IsFalse(ucak.GetComponent<KanatYatisi>().enabled, "Lab 2 auto-bank must not fight player roll");
            Assert.IsTrue(ucak.GetComponent<UcusKontrol>().enabled);
        }

        [Test]
        public void Rota03_UcusKontrolHasWorksheetParameters()
        {
            Open(Rota03Path);
            var ucus = Root("Uçak").GetComponent<UcusKontrol>();
            Assert.AreEqual(8f, Float(ucus, "enAzHiz"));
            Assert.AreEqual(42f, Float(ucus, "enCokHiz"));
            Assert.AreEqual(55f, Float(ucus, "yunuslamaHizi"));
            Assert.AreEqual(90f, Float(ucus, "yatisHizi"));
            Assert.AreEqual(30f, Float(ucus, "sapmaHizi"));
            Assert.AreEqual(6f, Float(ucus, "yumusatma"));
            Assert.AreEqual(0.5f, Float(ucus, "baslangicGazi"));
        }

        [Test]
        public void Rota03_PlayerInputUsesProjectAssetUcusMapAndSendMessages()
        {
            Open(Rota03Path);
            var input = Root("Uçak").GetComponent<PlayerInput>();
            Assert.IsNotNull(input);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"), input.actions);
            Assert.AreEqual("Ucus", input.defaultActionMap);
            Assert.AreEqual(PlayerNotifications.SendMessages, input.notificationBehavior);
        }

        [Test]
        public void Rota03_SaglikAndListenerAreWired()
        {
            Open(Rota03Path);
            GameObject ucak = Root("Uçak");
            var saglik = ucak.GetComponent<Saglik>();
            Assert.IsNotNull(saglik);
            Assert.AreEqual(100, new SerializedObject(saglik).FindProperty("baslangicCani").intValue);
            Assert.AreSame(saglik, ucak.GetComponent<CanYazisi>().Saglik);
        }

        [Test]
        public void Rota03_FollowCameraIsWiredWithWorksheetParameters()
        {
            Open(Rota03Path);
            Transform ucak = Root("Uçak").transform;
            var takip = Root("Main Camera").GetComponent<TakipKamerasi>();
            Assert.IsNotNull(takip);
            Assert.AreSame(ucak, takip.Hedef);
            Assert.AreSame(ucak.GetComponent<UcusKontrol>(), new SerializedObject(takip).FindProperty("ucus").objectReferenceValue);
            Assert.That((takip.Uzaklik - new Vector3(0f, 3.2f, -11f)).magnitude, Is.LessThan(Tolerance));
            Assert.AreEqual(0.18f, takip.Yumusatma, Tolerance);
            Assert.AreEqual(55f, takip.EnAzGorusAcisi);
            Assert.AreEqual(72f, takip.EnCokGorusAcisi);
            Assert.That((takip.transform.position - ucak.TransformPoint(takip.Uzaklik)).magnitude, Is.LessThan(1e-3f),
                "Camera should start at the follow offset");
        }

        [Test]
        public void Rota03_HasNoMissingScriptsOrReferences()
        {
            Open(Rota03Path);
            GameObject[] all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            Assert.AreEqual(0, all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount), "Missing scripts");
            foreach (Component c in all.SelectMany(g => g.GetComponents<Component>()))
            {
                var so = new SerializedObject(c);
                SerializedProperty p = so.GetIterator();
                while (p.NextVisible(true))
                {
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null)
                        Assert.That(IsDefault(p.objectReferenceEntityIdValue), $"Missing reference {c.GetType().Name}.{p.propertyPath} on {c.name}");
                }
            }
        }

        [Test]
        public void Rota02_IsUnchangedByLab3()
        {
            Open(Rota02Path);
            GameObject ucak = Root("Uçak");
            Assert.IsTrue(ucak.GetComponent<HedefeGit>().enabled, "Rota02 must keep Lab 2 auto-flight");
            Assert.IsTrue(ucak.GetComponent<KanatYatisi>().enabled);
            Assert.IsNull(ucak.GetComponent<UcusKontrol>());
            Assert.IsNull(ucak.GetComponent<PlayerInput>());
            Assert.IsNull(ucak.GetComponent<Saglik>());
            Assert.IsNull(Root("Main Camera").GetComponent<TakipKamerasi>());
            Assert.That((Root("Main Camera").transform.position - new Vector3(60f, 35f, 0f)).magnitude, Is.LessThan(Tolerance),
                "Rota02 camera (D-023) must be unchanged");
        }
    }
}
