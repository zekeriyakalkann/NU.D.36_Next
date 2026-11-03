using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 3 integration in Rota03 with simulated keyboard input. A virtual keyboard and mouse are
    /// added to the Input System, PlayerInput is paired with them, and key states are queued as
    /// input events (processed at the start of the next frame, like real key presses). The
    /// virtual devices are removed afterwards. Covers PlayerInput wiring, key directions,
    /// rotation, throttle callbacks, HedefeGit isolation, follow camera, health, and screenshots.
    /// (InputTestFixture is not used: its reset breaks the enabled project-wide actions asset.)
    /// </summary>
    public class Lab3GirdiTests
    {
        Keyboard keyboard;
        Mouse mouse;
        readonly HashSet<Key> pressed = new HashSet<Key>();
        Scene rota03;
        GameObject ucak;
        UcusKontrol ucus;
        PlayerInput playerInput;
        TakipKamerasi takip;
        Camera cam;

        InputSettings.EditorInputBehaviorInPlayMode savedEditorBehavior;
        InputSettings.BackgroundBehavior savedBackgroundBehavior;

        [UnitySetUp]
        public IEnumerator LoadRota03()
        {
            // By default the Editor only forwards keyboard input to Play Mode while the Game view
            // has focus (and batch mode has no Game view). Let the virtual devices through for the
            // duration of the test; restored in teardown.
            savedEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            savedBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;

            pressed.Clear();
            keyboard = InputSystem.AddDevice<Keyboard>("Lab3TestKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("Lab3TestMouse");
            yield return SceneManager.LoadSceneAsync("Rota03", LoadSceneMode.Single);
            rota03 = SceneManager.GetActiveScene();
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            ucak = ucus.gameObject;
            playerInput = ucak.GetComponent<PlayerInput>();
            takip = Object.FindAnyObjectByType<TakipKamerasi>();
            cam = takip.GetComponent<Camera>();
            // Keyboard&Mouse scheme requires both devices; pair only the virtual ones.
            playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnloadRota03()
        {
            Scene empty = SceneManager.CreateScene("Lab3TestBos" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (rota03.IsValid() && rota03.isLoaded)
                yield return SceneManager.UnloadSceneAsync(rota03);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorBehavior;
            InputSystem.settings.backgroundBehavior = savedBackgroundBehavior;
        }

        void SendKeys() => InputSystem.QueueStateEvent(keyboard, new KeyboardState(pressed.ToArray()));

        void Press(KeyControl key)
        {
            pressed.Add(key.keyCode);
            SendKeys();
        }

        void Release(KeyControl key)
        {
            pressed.Remove(key.keyCode);
            SendKeys();
        }

        IEnumerator Hold(KeyControl key, float seconds)
        {
            Press(key);
            yield return null; // event processed at the start of this frame
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
            Release(key);
            yield return null;
        }

        [Test]
        public void PlayerInput_IsOnUcusMapWithSendMessagesAndKeyboard()
        {
            Assert.AreEqual("InputSystem_Actions", playerInput.actions.name);
            Assert.AreEqual("Ucus", playerInput.currentActionMap.name);
            Assert.AreEqual(PlayerNotifications.SendMessages, playerInput.notificationBehavior);
            Assert.That(playerInput.devices.Contains(keyboard), "Simulated keyboard must be paired");
        }

        [UnityTest]
        public IEnumerator Keys_DeliverExpectedAxisValuesThroughCallbacks()
        {
            var cases = new (KeyControl key, string axis, float expected)[]
            {
                (keyboard.wKey, "Yunuslama", 1f), (keyboard.sKey, "Yunuslama", -1f),
                (keyboard.dKey, "Yatis", 1f), (keyboard.aKey, "Yatis", -1f),
                (keyboard.eKey, "Sapma", 1f), (keyboard.qKey, "Sapma", -1f),
                (keyboard.upArrowKey, "Gaz", 1f), (keyboard.downArrowKey, "Gaz", -1f),
            };
            var csv = new StringBuilder("key,action,pressed_value,released_value\n");
            foreach (var (key, axis, expected) in cases)
            {
                Press(key);
                yield return null;
                float pressed = Read(axis);
                Release(key);
                yield return null;
                float released = Read(axis);
                csv.AppendLine($"{key.name},{axis},{Lab3Kayit.F(pressed, "F1")},{Lab3Kayit.F(released, "F1")}");
                Assert.AreEqual(expected, pressed, 1e-4f, $"{key.name} → {axis}");
                Assert.AreEqual(0f, released, 1e-4f, $"{key.name} released");
            }
            Lab3Kayit.Yaz("girdi_yonleri.csv", csv.ToString());
        }

        float Read(string axis) => axis switch
        {
            "Yunuslama" => ucus.Yunuslama,
            "Yatis" => ucus.Yatis,
            "Sapma" => ucus.Sapma,
            _ => ucus.GazGirdisi,
        };

        [UnityTest]
        public IEnumerator Aircraft_RotatesWithHeldKeys()
        {
            ucak.transform.rotation = Quaternion.identity;
            yield return Hold(keyboard.dKey, 0.5f);
            Assert.That(ucak.transform.right.y, Is.LessThan(-0.1f), $"D: right wing down, right={ucak.transform.right}");
            Lab3Kayit.Goruntu(cam, "rota03_yatis_D.png");

            ucak.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(1f); // let smoothed roll decay
            ucak.transform.rotation = Quaternion.identity;
            yield return Hold(keyboard.eKey, 0.5f);
            Assert.That(ucak.transform.forward.x, Is.GreaterThan(0.02f), $"E: nose right, forward={ucak.transform.forward}");

            ucak.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(1f);
            ucak.transform.rotation = Quaternion.identity;
            yield return Hold(keyboard.wKey, 0.5f);
            Assert.That(ucak.transform.forward.y, Is.LessThan(-0.05f), $"W: nose down, forward={ucak.transform.forward}");
        }

        [UnityTest]
        public IEnumerator Gaz_CallbacksOnlyOnChange_HoldRampsReleaseHolds()
        {
            var sayac = ucak.AddComponent<GazCagriSayaci>();
            var csv = new StringBuilder("phase,seconds,callbacks_total,gaz,hiz\n");
            float g0 = ucus.GazOrani;
            csv.AppendLine($"start,0,0,{Lab3Kayit.F(g0)},{Lab3Kayit.F(ucus.Hiz)}");

            float t0 = Time.time;
            Press(keyboard.upArrowKey);
            for (int i = 0; i < 20; i++) yield return null;
            int callbacksWhileHeld = sayac.Cagrilar.Count;
            float heldSeconds = Time.time - t0;
            Release(keyboard.upArrowKey);
            yield return null;
            float g1 = ucus.GazOrani;
            csv.AppendLine($"hold_up_20_frames,{Lab3Kayit.F(heldSeconds)},{sayac.Cagrilar.Count},{Lab3Kayit.F(g1)},{Lab3Kayit.F(ucus.Hiz)}");

            for (int i = 0; i < 30; i++) yield return null;
            float g2 = ucus.GazOrani;
            csv.AppendLine($"released_30_frames,,{sayac.Cagrilar.Count},{Lab3Kayit.F(g2)},{Lab3Kayit.F(ucus.Hiz)}");

            for (int tap = 0; tap < 3; tap++)
            {
                Press(keyboard.upArrowKey);
                yield return null;
                yield return null;
                Release(keyboard.upArrowKey);
                yield return null;
            }
            csv.AppendLine($"three_taps,,{sayac.Cagrilar.Count},{Lab3Kayit.F(ucus.GazOrani)},{Lab3Kayit.F(ucus.Hiz)}");

            yield return Hold(keyboard.downArrowKey, 1.5f);
            csv.AppendLine($"hold_down_1.5s,,{sayac.Cagrilar.Count},{Lab3Kayit.F(ucus.GazOrani)},{Lab3Kayit.F(ucus.Hiz)}");
            float minHiz = ucus.Hiz, minGaz = ucus.GazOrani;
            yield return Hold(keyboard.upArrowKey, 1.5f);
            csv.AppendLine($"hold_up_1.5s,,{sayac.Cagrilar.Count},{Lab3Kayit.F(ucus.GazOrani)},{Lab3Kayit.F(ucus.Hiz)}");
            csv.AppendLine("callbacks(frame:value): " + string.Join(" ", sayac.Cagrilar.Select(c => $"{c.frame}:{Lab3Kayit.F(c.value, "F0")}")));
            Lab3Kayit.Yaz("gaz_callback_deneyi.csv", csv.ToString());

            Assert.AreEqual(1, callbacksWhileHeld, "Send Messages: one OnGaz on press, none while held");
            Assert.AreEqual(1f, sayac.Cagrilar[0].value);
            Assert.AreEqual(0f, sayac.Cagrilar[1].value, "Release sends OnGaz(0)");
            float expectedG1 = Mathf.Min(1f, g0 + 1.2f * heldSeconds);
            Assert.That(g1, Is.EqualTo(expectedG1).Within(0.05f), $"Held {heldSeconds:F3} s: ramps at 1.2/s (clamped at 1)");
            Assert.AreEqual(g1, g2, 1e-5f, "Released: throttle unchanged");
            Assert.AreEqual(0f, minGaz, 1e-4f);
            Assert.AreEqual(8f, minHiz, 1e-3f);
            Assert.AreEqual(1f, ucus.GazOrani, 1e-4f);
            Assert.AreEqual(42f, ucus.Hiz, 1e-3f);
        }

        [UnityTest]
        public IEnumerator HedefeGit_IsDisabled_AircraftFliesStraightUsingDeltaTime()
        {
            var hedefeGit = ucak.GetComponent<HedefeGit>();
            Assert.IsFalse(hedefeGit.enabled);
            Vector3 p0 = ucak.transform.position;
            float t0 = Time.time;
            yield return new WaitForSeconds(1.5f);
            float elapsed = Time.time - t0;
            Vector3 moved = ucak.transform.position - p0;

            Assert.That(Vector3.Angle(Vector3.forward, ucak.transform.forward), Is.LessThan(0.01f), "No input, HedefeGit off: heading stays +Z");
            Assert.AreEqual(ucus.Hiz * elapsed, moved.z, ucus.Hiz * 0.05f, "Distance = Hiz × elapsed time");
            Assert.That(Mathf.Abs(moved.x) + Mathf.Abs(moved.y), Is.LessThan(1e-3f));
            Assert.IsFalse(hedefeGit.HedefeVardi);
        }

        [UnityTest]
        public IEnumerator Camera_FollowsAircraft_FovTracksThrottle()
        {
            Lab3Kayit.Goruntu(cam, "rota03_baslangic_gaz_0.5.png");
            Assert.AreEqual(63.5f, cam.fieldOfView, 0.01f, "Start throttle 0.5");

            yield return Hold(keyboard.upArrowKey, 0.6f);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(1f, ucus.GazOrani, 1e-4f);
            Assert.AreEqual(72f, cam.fieldOfView, 0.01f);
            AssertFollowing(42f);
            Lab3Kayit.Goruntu(cam, "rota03_gaz_1.png");

            yield return Hold(keyboard.downArrowKey, 1.0f);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(0f, ucus.GazOrani, 1e-4f);
            Assert.AreEqual(55f, cam.fieldOfView, 0.01f);
            AssertFollowing(8f);
            Lab3Kayit.Goruntu(cam, "rota03_gaz_0.png");
        }

        void AssertFollowing(float speed)
        {
            float lag = Vector3.Distance(cam.transform.position, takip.IstenenKonum);
            Vector3 toTarget = (ucak.transform.position - cam.transform.position).normalized;
            TestContext.WriteLine($"speed {speed}: lag {lag:F2} m, fov {cam.fieldOfView:F1}");
            Assert.That(lag, Is.LessThan(speed * takip.Yumusatma * 1.5f + 0.5f), "Camera must stay near its follow point");
            Assert.That(Vector3.Angle(cam.transform.forward, toTarget), Is.LessThan(0.5f), "Camera must look at the aircraft");
        }

        [UnityTest]
        public IEnumerator Saglik_InRota03_ListenerReceivesDamage()
        {
            var saglik = ucak.GetComponent<Saglik>();
            LogAssert.Expect(LogType.Log, new Regex(@"^\[CanYazisi\] .*: can = 90$"));
            saglik.Vur(10);
            yield return null;
            Assert.AreEqual(90, saglik.Can);
        }

        [UnityTest]
        public IEnumerator Radar_StillWorksInRota03()
        {
            var radar = Object.FindAnyObjectByType<Radar>();
            Assert.IsFalse(radar.HedefGorunur, "Aircraft starts at 90° above the radar");
            yield return new WaitForSeconds(2f); // flies +Z at 25 m/s, under 45° after about 0.8 s
            Assert.IsTrue(radar.HedefGorunur);
        }
    }
}
