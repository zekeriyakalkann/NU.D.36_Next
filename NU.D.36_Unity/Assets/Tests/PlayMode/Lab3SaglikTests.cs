using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 3, Saglik + CanYazisi (Observer): health rules, event counts, and listener
    /// subscription across the component lifecycle. Play Mode so Awake/OnEnable/OnDisable run.
    /// </summary>
    public class Lab3SaglikTests
    {
        readonly List<GameObject> created = new List<GameObject>();
        int listenerLogs;

        GameObject New(string name, bool active = true)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            created.Add(go);
            return go;
        }

        void CountListenerLogs(string message, string stack, LogType type)
        {
            if (message.StartsWith("[CanYazisi]") && type == LogType.Log)
                listenerLogs++;
        }

        [SetUp]
        public void Subscribe()
        {
            listenerLogs = 0;
            Application.logMessageReceived += CountListenerLogs;
        }

        [TearDown]
        public void Cleanup()
        {
            Application.logMessageReceived -= CountListenerLogs;
            foreach (GameObject go in created)
                if (go != null)
                    Object.Destroy(go);
            created.Clear();
        }

        [Test]
        public void StartsAt100_Vur10Gives90_AndRaisesChangeOnce()
        {
            var saglik = New("S").AddComponent<Saglik>();
            var values = new List<int>();
            saglik.CanDegisti += values.Add;

            Assert.AreEqual(100, saglik.Can);
            saglik.Vur(10);
            Assert.AreEqual(90, saglik.Can);
            CollectionAssert.AreEqual(new[] { 90 }, values);
        }

        [Test]
        public void NeverBelowZero_DeathRaisedExactlyOnce_NoChangesAfterDeath()
        {
            var saglik = New("S").AddComponent<Saglik>();
            var values = new List<int>();
            int deaths = 0;
            saglik.CanDegisti += values.Add;
            saglik.Oldu += () => deaths++;

            for (int i = 0; i < 5; i++)
                saglik.Vur(30);

            Assert.AreEqual(0, saglik.Can);
            Assert.IsTrue(saglik.OlduMu);
            CollectionAssert.AreEqual(new[] { 70, 40, 10, 0 }, values, "No events after death");
            Assert.AreEqual(1, deaths);

            saglik.Vur(10);
            Assert.AreEqual(0, saglik.Can);
            Assert.AreEqual(1, deaths);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void NonPositiveDamage_IsIgnored(int miktar)
        {
            var saglik = New("S").AddComponent<Saglik>();
            int events = 0;
            saglik.CanDegisti += _ => events++;
            saglik.Vur(miktar);
            Assert.AreEqual(100, saglik.Can);
            Assert.AreEqual(0, events);
        }

        [Test]
        public void Listener_LogsHealthWhenDamaged()
        {
            GameObject go = New("Ucak");
            var saglik = go.AddComponent<Saglik>();
            go.AddComponent<CanYazisi>();
            LogAssert.Expect(LogType.Log, new Regex(@"^\[CanYazisi\] Ucak: can = 90$"));
            saglik.Vur(10);
            Assert.AreEqual(1, listenerLogs);
        }

        [Test]
        public void Listener_UnsubscribesOnDisable_AndDoesNotDoubleSubscribe()
        {
            GameObject go = New("Ucak");
            var saglik = go.AddComponent<Saglik>();
            var yazici = go.AddComponent<CanYazisi>();

            saglik.Vur(10);
            Assert.AreEqual(1, listenerLogs, "Enabled: one log");

            yazici.enabled = false;
            saglik.Vur(10);
            Assert.AreEqual(1, listenerLogs, "Disabled: no log");

            yazici.enabled = true;
            saglik.Vur(10);
            Assert.AreEqual(2, listenerLogs, "Re-enabled: exactly one more log");

            Object.DestroyImmediate(yazici);
            saglik.Vur(10);
            Assert.AreEqual(2, listenerLogs, "Destroyed listener must not be called");
            Assert.AreEqual(60, saglik.Can);
        }

        [Test]
        public void Listener_CanObserveSaglikOnAnotherObject()
        {
            var saglik = New("Hedef").AddComponent<Saglik>();
            GameObject listenerGo = New("Gosterge", active: false);
            listenerGo.AddComponent<CanYazisi>().Saglik = saglik;
            listenerGo.SetActive(true);

            LogAssert.Expect(LogType.Log, new Regex(@"^\[CanYazisi\] Gosterge: can = 75$"));
            saglik.Vur(25);
        }

        [Test]
        public void Listener_WithoutSaglik_WarnsAndDoesNotThrow()
        {
            GameObject go = New("Bos", active: false);
            var yazici = go.AddComponent<CanYazisi>();
            LogAssert.Expect(LogType.Warning, new Regex(@"\[CanYazisi\] Bos: no Saglik"));
            Assert.DoesNotThrow(() => go.SetActive(true));
            Assert.DoesNotThrow(() => yazici.enabled = false);
        }
    }
}
