using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using H = NU36.KursunDaglariHaritasi;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8 (Bölüm 4): the built structures of the map — our own simple geometry (not from the asset list): the
    /// departure station, the safe zone with its gate, the airbase (runway, hangars, tower), the stone viaduct over the
    /// gorge, the river and the rail chunks. No people, flags or emblems (worksheet, GDD §9.3 and §17).
    /// </summary>
    public static class Lab8Yapilar
    {
        public class Mat
        {
            public Material Duvar, Cati, Ahsap, Beton, Asfalt, PistCizgisi, Tas, Metal, Yesil, Kum, Su, Cam;
            public Mat()
            {
                Duvar = Lab8Varliklar.Lit("Yapi_Duvar", new Color(0.78f, 0.72f, 0.6f), 0.1f);
                Cati = Lab8Varliklar.Lit("Yapi_Cati", new Color(0.42f, 0.16f, 0.12f), 0.15f);
                Ahsap = Lab8Varliklar.Lit("Yapi_Ahsap", new Color(0.36f, 0.25f, 0.16f), 0.1f);
                Beton = Lab8Varliklar.Lit("Yapi_Beton", new Color(0.55f, 0.55f, 0.53f), 0.1f);
                Asfalt = Lab8Varliklar.Lit("Yapi_Asfalt", new Color(0.17f, 0.17f, 0.18f), 0.15f);
                PistCizgisi = Lab8Varliklar.Lit("Yapi_PistCizgisi", new Color(0.92f, 0.92f, 0.88f), 0.2f);
                Tas = Lab8Varliklar.Lit("Yapi_Tas", new Color(0.5f, 0.46f, 0.41f), 0.08f);
                Metal = Lab8Varliklar.Lit("Yapi_Metal", new Color(0.38f, 0.4f, 0.38f), 0.45f, 0.6f);
                Yesil = Lab8Varliklar.Lit("Yapi_Yesil", new Color(0.2f, 0.42f, 0.22f), 0.2f);
                Kum = Lab8Varliklar.Lit("Yapi_KumTorbasi", new Color(0.62f, 0.55f, 0.4f), 0.05f);
                Su = Lab8Varliklar.Lit("Su", new Color(0.12f, 0.27f, 0.3f), 0.9f);
                Cam = Lab8Varliklar.Lit("Yapi_Cam", new Color(0.35f, 0.5f, 0.55f), 0.9f, 0.2f);
            }
        }

        // ---------------------------------------------------------------- primitives

        public static GameObject Kutu(string ad, Transform ust, Vector3 merkez, Vector3 boyut, Quaternion donus, Material m, int katman, bool carpisici = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = ad;
            go.transform.SetParent(ust, false);
            go.transform.SetPositionAndRotation(merkez, donus);
            go.transform.localScale = boyut;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            if (!carpisici) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.layer = katman;
            return go;
        }

        public static GameObject Silindir(string ad, Transform ust, Vector3 alt, float yaricap, float boy, Material m, int katman, Quaternion? donus = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = ad;
            go.transform.SetParent(ust, false);
            Quaternion q = donus ?? Quaternion.identity;
            go.transform.SetPositionAndRotation(alt + q * Vector3.up * (boy * 0.5f), q);
            go.transform.localScale = new Vector3(yaricap * 2f, boy * 0.5f, yaricap * 2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.layer = katman;
            return go;
        }

        public static GameObject Yazi(string ad, Transform ust, string metin, Vector3 konum, Quaternion donus, float boy, Color renk)
        {
            var go = new GameObject(ad, typeof(TextMesh));
            go.transform.SetParent(ust, false);
            go.transform.SetPositionAndRotation(konum, donus);
            var t = go.GetComponent<TextMesh>();
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.font = f;
            t.text = metin;
            t.fontSize = 64;
            t.characterSize = boy / 6.4f * 0.1f;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = renk;
            go.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            return go;
        }

        static Transform Grup(string ad, Transform ust = null)
        {
            var g = new GameObject(ad).transform;
            if (ust != null) g.SetParent(ust, false);
            return g;
        }

        /// <summary>Frame on the line at s: position, horizontal forward, right.</summary>
        static (Vector3 p, Vector3 ileri, Vector3 sag, Quaternion q) Cerceve(RayEgrisi e, float s)
        {
            Vector3 ileri = e.YatayYon(s);
            return (e.Nokta(s), ileri, e.Sag(s), Quaternion.LookRotation(ileri, Vector3.up));
        }

        // ---------------------------------------------------------------- rails

        public static Transform RayKur(RayEgrisi e, Material[] malzemeler)
        {
            Transform kok = Grup("Ray");
            List<Mesh> meshler = Lab8Varliklar.RayMeshleri(e, 0f, e.Uzunluk);
            for (int i = 0; i < meshler.Count; i++)
            {
                var go = new GameObject(meshler[i].name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(kok, false);
                go.GetComponent<MeshFilter>().sharedMesh = meshler[i];
                go.GetComponent<MeshRenderer>().sharedMaterials = malzemeler;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            }
            return kok;
        }

        static void Tampon(string ad, Transform ust, RayEgrisi e, float s, bool ileriBakar, Mat m, int katman)
        {
            var (p, ileri, sag, q) = Cerceve(e, s);
            Quaternion yon = ileriBakar ? q : q * Quaternion.Euler(0f, 180f, 0f);
            Kutu(ad, ust, p + Vector3.up * 0.7f, new Vector3(3.2f, 1.2f, 0.8f), yon, m.Ahsap, katman);
            Kutu(ad + " Ayak", ust, p + Vector3.up * 0.4f - (yon * Vector3.forward) * 1.2f, new Vector3(2.4f, 0.8f, 2f), yon, m.Metal, katman);
        }

        // ---------------------------------------------------------------- stations

        public static Transform IstasyonKur(RayEgrisi e, Mat m, int katman)
        {
            Transform kok = Grup("Kalkis Istasyonu");
            float s = H.IstasyonBoyu * 0.5f + 10f;
            var (p, ileri, sag, q) = Cerceve(e, s);
            Vector3 sol = -sag;
            Kutu("Peron", kok, p + sol * 6.2f + Vector3.up * 0.45f, new Vector3(6f, 0.9f, H.IstasyonBoyu - 20f), q, m.Beton, katman);
            Vector3 bina = p + sol * 17f;
            Kutu("Istasyon Binasi", kok, bina + Vector3.up * 3.5f, new Vector3(9f, 7f, 26f), q, m.Duvar, katman);
            Kutu("Cati", kok, bina + Vector3.up * 7.6f, new Vector3(11f, 1.2f, 29f), q, m.Cati, katman);
            Kutu("Saçak", kok, p + sol * 8.2f + Vector3.up * 4.4f, new Vector3(4f, 0.3f, 60f), q, m.Cati, katman);
            for (int i = -2; i <= 2; i++)
                Silindir("Direk", kok, p + sol * 9.6f + ileri * (i * 14f), 0.18f, 4.3f, m.Ahsap, katman);
            Yazi("Tabela", kok, "KALKIŞ", bina + sag * 4.6f + Vector3.up * 5.5f, q * Quaternion.Euler(0f, -90f, 0f), 2.2f, new Color(0.15f, 0.1f, 0.08f));
            // Water tower and stacked supplies.
            Vector3 su = e.Nokta(s + 70f) + (-e.Sag(s + 70f)) * 15f;
            for (int i = 0; i < 4; i++)
                Silindir("Su Kulesi Ayak", kok, su + new Vector3(i < 2 ? -2.2f : 2.2f, 0f, i % 2 == 0 ? -2.2f : 2.2f), 0.25f, 8f, m.Ahsap, katman);
            Silindir("Su Kulesi", kok, su + Vector3.up * 8f, 3.2f, 5.5f, m.Ahsap, katman);
            for (int i = 0; i < 6; i++)
                Kutu("Sandik", kok, p + sol * 6f + ileri * (-40f + i * 3.1f) + Vector3.up * (1.6f), Vector3.one * 1.4f, q, m.Ahsap, katman);
            Tampon("Tampon Bas", kok, e, 3f, false, m, katman);
            return kok;
        }

        public static Transform GuvenliBolgeKur(RayEgrisi e, Mat m, int katman)
        {
            Transform kok = Grup("Guvenli Bolge");
            float L = e.Uzunluk, kapi = H.GuvenliBolgeBasi(L);
            var (p, ileri, sag, q) = Cerceve(e, kapi);
            // Gate over the line.
            Kutu("Kapi Sol", kok, p - sag * 5.5f + Vector3.up * 4f, new Vector3(1.2f, 8f, 1.2f), q, m.Yesil, katman);
            Kutu("Kapi Sag", kok, p + sag * 5.5f + Vector3.up * 4f, new Vector3(1.2f, 8f, 1.2f), q, m.Yesil, katman);
            Kutu("Kapi Ust", kok, p + Vector3.up * 8.4f, new Vector3(13f, 1.6f, 0.8f), q, m.Yesil, katman);
            Yazi("Kapi Yazisi", kok, "GÜVENLİ BÖLGE", p + Vector3.up * 8.4f - ileri * 0.45f, q, 1.1f, Color.white);
            // Depot: two long sheds, a platform, sandbag walls.
            float sm = L - 160f;
            var (d, di, ds, dq) = Cerceve(e, sm);
            Kutu("Peron", kok, d - ds * 6.2f + Vector3.up * 0.45f, new Vector3(6f, 0.9f, 180f), dq, m.Beton, katman);
            foreach (int yan in new[] { -1, 1 })
            {
                Vector3 c = d + ds * (yan * 26f);
                Kutu("Depo", kok, c + Vector3.up * 4f, new Vector3(14f, 8f, 70f), dq, m.Duvar, katman);
                Kutu("Depo Cati", kok, c + Vector3.up * 8.4f, new Vector3(15.5f, 0.8f, 72f), dq, m.Yesil, katman);
            }
            for (int i = 0; i < 14; i++)
            {
                float ss = kapi + 30f + i * 18f;
                if (ss > L - 10f) break;
                var (bp, bi, bs, bq) = Cerceve(e, ss);
                foreach (int yan in new[] { -1, 1 })
                    Kutu("Kum Torbasi", kok, bp + bs * (yan * 44f) + Vector3.up * 0.7f, new Vector3(1.6f, 1.4f, 14f), bq, m.Kum, katman);
            }
            Tampon("Tampon Son", kok, e, L - 3f, true, m, katman);
            return kok;
        }

        // ---------------------------------------------------------------- airbase

        public class Us { public Transform Kok; }

        public static Us UsKur(Mat m, int katman)
        {
            Transform kok = Grup("Us");
            Quaternion q = Quaternion.Euler(0f, H.PistYonu, 0f);
            Vector3 c = H.UsMerkezi;
            Vector3 ileri = H.PistEkseni, yan = Vector3.Cross(Vector3.up, ileri);
            Kutu("Pist", kok, c + Vector3.up * 0.05f, new Vector3(H.PistEni, 0.2f, H.PistBoyu), q, m.Asfalt, katman);
            for (float u = -H.PistBoyu / 2f + 60f; u < H.PistBoyu / 2f - 50f; u += 40f)
                Kutu("Orta Cizgi", kok, c + ileri * u + Vector3.up * 0.17f, new Vector3(0.9f, 0.05f, 18f), q, m.PistCizgisi, katman, false);
            foreach (int uc in new[] { -1, 1 })
                for (int i = -4; i <= 4; i++)
                    if (i != 0)
                        Kutu("Esik", kok, c + ileri * (uc * (H.PistBoyu / 2f - 18f)) + yan * (i * 3.8f) + Vector3.up * 0.17f, new Vector3(1.6f, 0.05f, 22f), q, m.PistCizgisi, katman, false);
            Vector3 apron = c - yan * 95f;
            Kutu("Apron", kok, apron + Vector3.up * 0.04f, new Vector3(150f, 0.15f, 300f), q, m.Beton, katman);
            Kutu("Taksi Yolu", kok, c - yan * 45f + Vector3.up * 0.03f, new Vector3(50f, 0.12f, 18f), q, m.Beton, katman);
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 h = apron - yan * 50f + ileri * (i * 70f);
                var hangar = Silindir("Hangar", kok, h, 15f, 46f, m.Metal, katman, q * Quaternion.Euler(90f, 0f, 0f));
                hangar.transform.position = h + Vector3.up * 2f;   // half buried: an arched roof
            }
            Vector3 kule = apron - yan * 55f;
            Kutu("Kule", kok, kule + Vector3.up * 7f, new Vector3(6f, 14f, 6f), q, m.Duvar, katman);
            Kutu("Kule Cam", kok, kule + Vector3.up * 15.5f, new Vector3(7.5f, 3f, 7.5f), q, m.Cam, katman);
            Kutu("Kule Cati", kok, kule + Vector3.up * 17.4f, new Vector3(8.5f, 0.8f, 8.5f), q, m.Cati, katman);
            for (int i = 0; i < 3; i++)
                Silindir("Yakit Tanki", kok, apron - yan * 40f + ileri * (110f + i * 12f), 4f, 6f, m.Metal, katman);
            Yazi("Us Yazisi", kok, "ÜS", kule + Vector3.up * 20.5f, q * Quaternion.Euler(0f, -90f, 0f), 4f, Color.white);
            return new Us { Kok = kok };
        }

        // ---------------------------------------------------------------- viaduct, river

        /// <summary>Stretch of the line that runs over ground lower than the rail bed by more than 2.5 m (the gorge).</summary>
        public static (float bas, float son) KopruAraligi(RayEgrisi e, System.Func<float, float, float> zemin)
        {
            float bas = -1f, son = -1f;
            for (float s = 0f; s <= e.Uzunluk; s += 2f)
            {
                Vector3 p = e.Nokta(s);
                if (zemin(p.x, p.z) < p.y - 2.5f)
                {
                    if (bas < 0f) bas = s;
                    son = s;
                }
            }
            return (bas, son);
        }

        public static Transform ViyadukKur(RayEgrisi e, float bas, float son, System.Func<float, float, float> zemin, Mat m, int katman)
        {
            Transform kok = Grup("Viyaduk");
            if (bas < 0f) return kok;
            bas -= 12f;
            son += 12f;
            // Deck with parapets, swept along the curve.
            const float en = 8f, kalinlik = 2.6f, korkuluk = 1.1f;
            var v = new List<Vector3>(); var tri = new List<int>();
            Vector2[] kesit =
            {
                new Vector2(-en / 2, -kalinlik), new Vector2(en / 2, -kalinlik), new Vector2(en / 2, korkuluk), new Vector2(en / 2 - 0.5f, korkuluk),
                new Vector2(en / 2 - 0.5f, -0.3f), new Vector2(-en / 2 + 0.5f, -0.3f), new Vector2(-en / 2 + 0.5f, korkuluk), new Vector2(-en / 2, korkuluk),
            };
            int k = kesit.Length, adim = 0;
            for (float s = bas; ; s += 3f, adim++)
            {
                float ss = Mathf.Min(s, son);
                Vector3 p = e.Nokta(ss), sag = e.Sag(ss);
                foreach (Vector2 c in kesit)
                    v.Add(p + sag * c.x + Vector3.up * c.y);
                if (ss >= son) break;
            }
            int halka = adim + 1;
            for (int i = 0; i < halka - 1; i++)
                for (int j = 0; j < k; j++)
                {
                    int a = i * k + j, b = i * k + (j + 1) % k, c = (i + 1) * k + j, d = (i + 1) * k + (j + 1) % k;
                    tri.AddRange(new[] { a, c, b, b, c, d });
                }
            var mesh = new Mesh { name = "Viyaduk_Guverte", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Lab8Varliklar.Klasor(Lab8Varliklar.Meshler);
            string yol = $"{Lab8Varliklar.Meshler}/Viyaduk_Guverte.asset";
            AssetDatabase.DeleteAsset(yol);
            AssetDatabase.CreateAsset(mesh, yol);
            var guverte = new GameObject("Guverte", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            guverte.transform.SetParent(kok, false);
            guverte.GetComponent<MeshFilter>().sharedMesh = mesh;
            guverte.GetComponent<MeshRenderer>().sharedMaterial = m.Tas;
            guverte.GetComponent<MeshCollider>().sharedMesh = mesh;
            guverte.layer = katman;

            // Piers every 24 m, from the gorge floor up to the deck, tapering.
            for (float s = bas + 12f; s <= son - 6f; s += 24f)
            {
                var (p, ileri, sag, q) = Cerceve(e, s);
                float alt = zemin(p.x, p.z) - 4f, ust = p.y - kalinlik;
                if (ust - alt < 3f) continue;
                float boy = ust - alt;
                Kutu("Ayak", kok, new Vector3(p.x, alt + boy * 0.5f, p.z), new Vector3(en - 1f + boy * 0.04f, boy, 4.5f + boy * 0.03f), q, m.Tas, katman);
                Kutu("Ayak Basligi", kok, new Vector3(p.x, ust - 0.6f, p.z), new Vector3(en + 0.6f, 1.2f, 6f), q, m.Tas, katman);
            }
            return kok;
        }

        public static GameObject SuKur(Mat m)
        {
            var su = GameObject.CreatePrimitive(PrimitiveType.Plane);
            su.name = "Nehir Suyu";
            Object.DestroyImmediate(su.GetComponent<Collider>());
            su.transform.position = new Vector3(0f, H.SuSeviyesi, 0f);
            su.transform.localScale = new Vector3(H.AraziBoyutu / 10f, 1f, H.AraziBoyutu / 10f);
            su.GetComponent<MeshRenderer>().sharedMaterial = m.Su;
            su.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return su;
        }
    }
}
