using System;
using System.Collections;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 3 terrain in Rota03 at runtime. The aircraft is flown through UcusKontrol.Adim (the
    /// same code Update runs) in large steps, so kilometres are covered in a few seconds; keyboard
    /// input is covered by Lab3GirdiTests.
    /// </summary>
    public class Lab3TerrainPlayTests
    {
        SonsuzArazi arazi;
        UcusKontrol ucus;
        Camera cam;
        Transform ucak;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Rota03", LoadSceneMode.Single);
            yield return null;
            arazi = UnityEngine.Object.FindFirstObjectByType<SonsuzArazi>();
            ucus = UnityEngine.Object.FindFirstObjectByType<UcusKontrol>();
            cam = Camera.main;
            ucak = ucus.transform;
        }

        int MeshSayisi() => Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "ChunkMesh");

        [UnityTest]
        public IEnumerator InitialWindow_IsFullyBuilt_AndTerrainIsUnderTheAircraft()
        {
            Assert.IsNotNull(arazi);
            int beklenen = (2 * arazi.Yaricap + 1) * (2 * arazi.Yaricap + 1);
            Assert.AreEqual(beklenen, arazi.HavuzBoyutu);
            Assert.AreEqual(beklenen, arazi.HazirChunkSayisi, "Every chunk of the window is built before the first frame");
            Assert.AreEqual(0, arazi.BekleyenChunkSayisi);
            Assert.AreEqual(beklenen, arazi.transform.childCount);
            Assert.AreEqual(0, arazi.GetComponentsInChildren<Collider>().Length, "Terrain must not collide");
            Assert.IsTrue(arazi.KapsiyorMu(ucak.position.x, ucak.position.z));
            Assert.That(AraziUretici.Yukseklik(ucak.position.x, ucak.position.z), Is.LessThan(ucak.position.y - 10f));
            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual("NU36/GradientSky", RenderSettings.skybox.shader.name);
            Assert.AreEqual(2200f, cam.farClipPlane);

            // Every renderer visible, with the terrain shader that compiled for this platform.
            Renderer[] renderers = arazi.GetComponentsInChildren<Renderer>();
            Assert.That(renderers.All(r => r.enabled));
            Assert.That(renderers.All(r => r.sharedMaterial.shader.isSupported));
            yield return null;
            Lab3Kayit.Goruntu(cam, "arazi_01_baslangic.png");
        }

        [UnityTest]
        public IEnumerator Flying_StraightTurningAndClimbing_KeepsWindowAroundAircraftWithoutGrowth()
        {
            int beklenen = arazi.HavuzBoyutu;
            int mesh0 = MeshSayisi();
            int cocuk0 = arazi.transform.childCount;
            int gc0 = GC.CollectionCount(0);
            ucus.GazAyarla(1f);
            var log = new StringBuilder("frame,x,y,z,heading_deg,center_chunk_x,center_chunk_z,ready,pending\n");
            int bosluk = 0, enCokBekleyen = 0, frame = 0;
            float enUzakAdim = 0f;

            IEnumerator Ucus(int kare, float yunuslama, float sapma, float yatis, string resim)
            {
                ucus.Yunuslama = yunuslama; ucus.Sapma = sapma; ucus.Yatis = yatis;
                for (int i = 0; i < kare; i++)
                {
                    Vector3 once = ucak.position;
                    ucus.Adim(0.4f); // 16.8 m per frame at full throttle
                    enUzakAdim = Mathf.Max(enUzakAdim, Vector3.Distance(once, ucak.position));
                    yield return null;
                    frame++;
                    enCokBekleyen = Mathf.Max(enCokBekleyen, arazi.BekleyenChunkSayisi);

                    // Window must stay centred on the aircraft and the 5 chunks around it must be built.
                    Vector2Int c = SonsuzArazi.ChunkKoordu(ucak.position, arazi.ChunkBoyutu);
                    Assert.AreEqual(c, arazi.MerkezChunk, $"frame {frame}");
                    for (int dz = -2; dz <= 2; dz++)
                        for (int dx = -2; dx <= 2; dx++)
                            if (!arazi.KapsiyorMu(ucak.position.x + dx * arazi.ChunkBoyutu, ucak.position.z + dz * arazi.ChunkBoyutu))
                                bosluk++;
                    if (frame % 10 == 0)
                        log.AppendLine($"{frame},{ucak.position.x:F0},{ucak.position.y:F0},{ucak.position.z:F0}," +
                                       $"{ucak.eulerAngles.y:F0},{c.x},{c.y},{arazi.HazirChunkSayisi},{arazi.BekleyenChunkSayisi}");
                }
                if (resim != null)
                {
                    yield return new WaitForSeconds(0.6f); // camera catches up
                    Lab3Kayit.Goruntu(cam, resim);
                }
            }

            // Start high enough to stay above the mountain ranges ahead; the terrain has no collider.
            ucak.SetPositionAndRotation(new Vector3(0f, 120f, 0f), Quaternion.Euler(0f, 70f, 0f));
            yield return new WaitForSeconds(1f);

            yield return Ucus(100, 0f, 0f, 0f, "arazi_02_gerade.png");   // straight, ~1.7 km
            yield return Ucus(5, 0f, 1f, 0f, null);                      // right turn (yaw), 60 degrees
            yield return Ucus(60, 0f, 0f, 0f, "arazi_03_donus_sonrasi.png");
            yield return Ucus(2, -1f, 0f, 0f, null);                     // pull the nose up, about 40 degrees
            yield return Ucus(40, 0f, 0f, 0f, "arazi_04_tirmanis.png");
            yield return Ucus(2, 1f, 0f, 0f, null);                      // nose back down
            yield return Ucus(2, 0f, 0f, -1f, null);                     // roll left
            yield return Ucus(60, 0f, 0f, 0f, "arazi_05_yatis.png");

            Lab3Kayit.Yaz("arazi_ucus_izi.csv", log.ToString());
            Lab3Kayit.Yaz("arazi_ucus_ozet.txt",
                $"frames={frame}\nfinal_position={ucak.position}\nlargest_step_m={enUzakAdim:F1}\nmax_pending_chunks={enCokBekleyen}\n" +
                $"pool={arazi.HavuzBoyutu} children={arazi.transform.childCount} meshes={MeshSayisi()}\n" +
                $"holes_in_5x5_around_aircraft={bosluk}\ngc_gen0_collections={GC.CollectionCount(0) - gc0}\n");

            Assert.AreEqual(0, bosluk, "No unbuilt chunk within 2 chunks of the aircraft at any frame");
            Assert.AreEqual(beklenen, arazi.HavuzBoyutu);
            Assert.AreEqual(cocuk0, arazi.transform.childCount, "No GameObjects created or destroyed while flying");
            Assert.AreEqual(mesh0, MeshSayisi(), "No meshes created while flying");
            Assert.Greater(Vector3.Distance(Vector3.zero, new Vector3(ucak.position.x, 0, ucak.position.z)), 2500f, "Flew more than 2.5 km");
            Assert.AreEqual(0, arazi.GetComponentsInChildren<Collider>().Length);
            Assert.That(arazi.transform.GetComponentsInChildren<MeshFilter>().All(f => f.sharedMesh.vertexCount == AraziUretici.KoseSayisi(arazi.Hucre)));
        }

        [UnityTest]
        public IEnumerator LongTeleport_RebuildsWholeWindow_WithinAFewSeconds()
        {
            ucak.position = new Vector3(52_000f, 25f, -37_000f);
            float t0 = Time.realtimeSinceStartup;
            yield return null; // LateUpdate re-centres the window and queues the missing chunks
            int frames = 1;
            while (arazi.BekleyenChunkSayisi > 0 && frames < 600)
            {
                yield return null;
                frames++;
            }
            Assert.AreEqual(0, arazi.BekleyenChunkSayisi);
            Assert.AreEqual(arazi.HavuzBoyutu, arazi.HazirChunkSayisi);
            Assert.IsTrue(arazi.KapsiyorMu(ucak.position.x, ucak.position.z));
            Lab3Kayit.Yaz("arazi_teleport.txt", $"frames_to_rebuild_full_window={frames}\nseconds={Time.realtimeSinceStartup - t0:F2}\n");
        }

        [UnityTest]
        public IEnumerator ExistingFlightAndCamera_StillWork_WithTerrainPresent()
        {
            Vector3 p0 = ucak.position;
            float hiz = ucus.Hiz;
            yield return new WaitForSeconds(1f);
            Assert.That((ucak.position - p0).z, Is.GreaterThan(hiz * 0.5f), "Aircraft still flies forward");
            Assert.That(Vector3.Distance(cam.transform.position, ucak.position), Is.LessThan(20f), "Follow camera stays with the aircraft");
            Assert.IsNotNull(ucak.GetComponent<Saglik>());
            Assert.IsNotNull(ucak.GetComponent<UnityEngine.InputSystem.PlayerInput>());
        }
    }
}
