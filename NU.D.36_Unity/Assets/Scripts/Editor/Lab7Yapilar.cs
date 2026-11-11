using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 7 structures built from primitives by code: the airfield (runway with markings and lights, apron, Nissen hangars,
    /// control hut, landing zone), the Bölüm 3 bridge (H5–J5), the three searchlight towers, the AA guns, the sound locator,
    /// the clouds of the single rule and the villages; positions come from the paper map (<see cref="Bolum3Harita"/>).
    /// Every falling part is grouped so it can become one rigid body when its target is destroyed (<see cref="Yikilabilir"/>).
    /// </summary>
    public static class Lab7Yapilar
    {
        public sealed class Mat
        {
            public readonly Material Asfalt = Lab7Varliklar.Lit("Asfalt", new Color(0.55f, 0.55f, 0.56f), 0.25f, null, Lab7Varliklar.AsfaltDokusu(), new Vector2(4f, 140f));
            public readonly Material Isaret = Lab7Varliklar.Lit("PistIsareti", new Color(0.78f, 0.78f, 0.74f), 0.3f);
            public readonly Material Beton = Lab7Varliklar.Lit("Beton", new Color(0.36f, 0.36f, 0.35f), 0.15f, null, Lab7Varliklar.AsfaltDokusu(), new Vector2(6f, 6f));
            public readonly Material Hangar = Lab7Varliklar.Lit("Hangar", new Color(0.17f, 0.19f, 0.16f), 0.35f, null, null, null, 0.45f);
            public readonly Material Karanlik = Lab7Varliklar.Lit("Karanlik", new Color(0.03f, 0.03f, 0.03f), 0.1f);
            public readonly Material Celik = Lab7Varliklar.Lit("Celik", new Color(0.22f, 0.23f, 0.24f), 0.45f, null, null, null, 0.6f);
            public readonly Material Tas = Lab7Varliklar.Lit("Tas", new Color(0.40f, 0.39f, 0.36f), 0.1f);
            public readonly Material Guverte = Lab7Varliklar.Lit("KopruGuvertesi", new Color(0.30f, 0.29f, 0.27f), 0.15f);
            public readonly Material Ahsap = Lab7Varliklar.Lit("Ahsap", new Color(0.30f, 0.21f, 0.13f), 0.1f);
            public readonly Material Kum = Lab7Varliklar.Lit("KumTorbasi", new Color(0.52f, 0.46f, 0.33f), 0.05f);
            public readonly Material Badana = Lab7Varliklar.Lit("Badana", new Color(0.72f, 0.69f, 0.62f), 0.1f);
            public readonly Material Toprak = Lab7Varliklar.Lit("Toprak", new Color(0.58f, 0.46f, 0.31f), 0.1f);
            public readonly Material TasDuvar = Lab7Varliklar.Lit("TasDuvar", new Color(0.47f, 0.46f, 0.43f), 0.1f);
            public readonly Material Kiremit = Lab7Varliklar.Lit("Kiremit", new Color(0.42f, 0.19f, 0.12f), 0.2f);
            public readonly Material Arduaz = Lab7Varliklar.Lit("Arduaz", new Color(0.17f, 0.17f, 0.19f), 0.3f);
            public readonly Material Cam = Lab7Varliklar.Lit("KaranlikCam", new Color(0.05f, 0.06f, 0.08f), 0.8f);
            public readonly Material Pencere = Lab7Varliklar.Ampul("YanikPencere", new Color(1f, 0.72f, 0.38f), 2.2f);
            public readonly Material Turuncu = Lab7Varliklar.Lit("Ruzgarlik", new Color(0.85f, 0.35f, 0.08f), 0.2f);
            public readonly Material Tank = Lab7Varliklar.Lit("YakitTanki", new Color(0.30f, 0.32f, 0.27f), 0.4f, null, null, null, 0.5f);
            public readonly Material PistLambasi = Lab7Varliklar.Ampul("PistLambasi", new Color(1f, 0.86f, 0.6f), 6f);
            public readonly Material YesilLamba = Lab7Varliklar.Ampul("YesilLamba", new Color(0.3f, 1f, 0.4f), 7f);
            public readonly Material KirmiziLamba = Lab7Varliklar.Ampul("KirmiziLamba", new Color(1f, 0.15f, 0.1f), 7f);
            public readonly Material SokakLambasi = Lab7Varliklar.Ampul("SokakLambasi", new Color(1f, 0.75f, 0.45f), 8f);
            public readonly Material Mercek = Lab7Varliklar.Ampul("IsildakMercegi", new Color(1f, 0.96f, 0.85f), 14f);
            public readonly Material InisCizgisi = Lab7Varliklar.Ampul("InisBolgesiCizgisi", new Color(0.25f, 1f, 0.35f), 3f);
            public readonly Material IsildakHuzmesi = Lab7Varliklar.Huzme("IsildakHuzmesi", new Color(1f, 0.95f, 0.8f), 0.32f, 1.4f);
            public readonly Material FarHuzmesi = Lab7Varliklar.Huzme("FenerHuzmesi", new Color(1f, 0.95f, 0.85f), 0.05f, 2.2f);
            public readonly Material IsaretHuzmesi = Lab7Varliklar.Huzme("InisIsareti", new Color(0.3f, 1f, 0.4f), 0.6f, 1.2f);
        }

        // ================================================================== helpers

        public static Transform Grup(string ad, Transform ust, Vector3 yerel = default, Quaternion? donus = null)
        {
            var g = new GameObject(ad).transform;
            g.SetParent(ust, false);
            g.localPosition = yerel;
            g.localRotation = donus ?? Quaternion.identity;
            return g;
        }

        public static GameObject Parca(PrimitiveType tur, string ad, Transform ust, Material mat, Vector3 konum, Vector3 olcek, Quaternion? donus = null, bool carpisici = false)
        {
            GameObject g = GameObject.CreatePrimitive(tur);
            g.name = ad;
            if (!carpisici)
                Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(ust, false);
            g.transform.localPosition = konum;
            g.transform.localRotation = donus ?? Quaternion.identity;
            g.transform.localScale = olcek;
            var r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            return g;
        }

        /// <summary>A square beam of the given thickness from local point a to b.</summary>
        public static GameObject Kiris(string ad, Transform ust, Material mat, Vector3 a, Vector3 b, float kalinlik)
        {
            Vector3 d = b - a;
            return Parca(PrimitiveType.Cube, ad, ust, mat, (a + b) / 2f, new Vector3(kalinlik, d.magnitude, kalinlik), Quaternion.FromToRotation(Vector3.up, d));
        }

        public static GameObject Ampul(string ad, Transform ust, Material mat, Vector3 konum, float cap = 1.1f)
        {
            GameObject g = Parca(PrimitiveType.Sphere, ad, ust, mat, konum, Vector3.one * cap);
            g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }

        public static Light Isik(string ad, Transform ust, LightType tur, Color renk, float parlaklik, float menzil, Vector3 konum, Quaternion? donus = null, float aci = 60f, float icAci = 40f)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(ust, false);
            go.transform.localPosition = konum;
            go.transform.localRotation = donus ?? Quaternion.identity;
            var l = go.AddComponent<Light>();
            l.type = tur;
            l.color = renk;
            l.intensity = parlaklik;
            l.range = menzil;
            l.shadows = LightShadows.None;
            if (tur == LightType.Spot)
            {
                l.spotAngle = aci;
                l.innerSpotAngle = icAci;
            }
            return l;
        }

        public static void Katman(GameObject go, int katman)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = katman;
        }

        static BoxCollider Kutu(GameObject go, Vector3 merkez, Vector3 boyut)
        {
            var k = go.AddComponent<BoxCollider>();
            k.center = merkez;
            k.size = boyut;
            return k;
        }

        // ================================================================== airfield

        public sealed class Havaalani
        {
            public GameObject Kok, PistIsiklari;
            public InisBolgesi InisBolgesi;
            public BoxCollider IkmalTetigi;
        }

        public static Havaalani HavaalaniKur(Mat m, int arazi, int yapi)
        {
            var sonuc = new Havaalani();
            var kok = new GameObject("Havaalani");
            sonuc.Kok = kok;
            Vector3 merkez = new Vector3(GeceHaritasi.PistMerkezi.x, GeceHaritasi.PistYuksekligi, GeceHaritasi.PistMerkezi.y);
            Transform pist = Grup("Pist", kok.transform);
            pist.SetPositionAndRotation(merkez, Quaternion.Euler(0f, GeceHaritasi.PistYonu, 0f));
            float L = GeceHaritasi.PistBoyu, W = GeceHaritasi.PistEni, yari = L / 2f;

            // Asphalt (counts as ground: layer Arazi, box collider), shoulders and markings.
            GameObject asfalt = Parca(PrimitiveType.Cube, "Asfalt 09-27", pist, m.Asfalt, new Vector3(0f, 0.15f, 0f), new Vector3(W, 0.3f, L), carpisici: true);
            asfalt.layer = arazi;
            Transform isaret = Grup("Isaretler", pist);
            const float y = 0.31f;
            for (float z = -yari + 90f; z <= yari - 90f; z += 50f)
                Parca(PrimitiveType.Cube, "Orta Cizgi", isaret, m.Isaret, new Vector3(0f, y, z), new Vector3(0.9f, 0.02f, 25f));
            foreach (float uc in new[] { -1f, 1f })
            {
                for (int k = 0; k < 4; k++)
                    foreach (float s in new[] { -1f, 1f })
                        Parca(PrimitiveType.Cube, "Esik Bandi", isaret, m.Isaret, new Vector3(s * (3.2f + k * 4.2f), y, uc * (yari - 25f)), new Vector3(1.8f, 0.02f, 30f));
                foreach (float s in new[] { -1f, 1f })
                {
                    Parca(PrimitiveType.Cube, "Nisan Noktasi", isaret, m.Isaret, new Vector3(s * 7f, y, uc * (yari - 300f)), new Vector3(5f, 0.02f, 45f));
                    for (int k = 0; k < 3; k++)
                        Parca(PrimitiveType.Cube, "Temas Bolgesi", isaret, m.Isaret, new Vector3(s * (5f + k * 2.2f), y, uc * (yari - 150f)), new Vector3(1.4f, 0.02f, 22f));
                }
            }

            // Runway lights: on for the take-off and the landing (the mission switches the group).
            Transform isiklar = Grup("Pist Isiklari", pist);
            sonuc.PistIsiklari = isiklar.gameObject;
            for (float z = -yari + 10f; z <= yari - 10f; z += 60f)
                foreach (float s in new[] { -1f, 1f })
                    Ampul("Kenar", isiklar, m.PistLambasi, new Vector3(s * (W / 2f + 3f), 0.9f, z));
            for (float x = -W / 2f; x <= W / 2f + 0.1f; x += 4f)
            {
                Ampul("Esik 27 (yesil)", isiklar, m.YesilLamba, new Vector3(x, 0.9f, yari + 3f));
                Ampul("Son 27 (kirmizi)", isiklar, m.KirmiziLamba, new Vector3(x, 0.9f, -yari - 3f));
            }
            // Approach lights beyond the 27 threshold (the return from the bridge), standing on the terrain.
            Vector3 ileri = GeceHaritasi.PistEkseni, sag = Vector3.Cross(Vector3.up, ileri);
            for (float z = yari + 30f; z <= yari + 330f; z += 30f)
            {
                float[] xs = Mathf.Approximately(z, yari + 150f) ? new[] { -15f, -12f, -9f, -6f, -3f, 0f, 3f, 6f, 9f, 12f, 15f } : new[] { -1.5f, 0f, 1.5f };
                foreach (float x in xs)
                {
                    Vector3 w = merkez + ileri * z + sag * x;
                    float h = GeceHaritasi.Yukseklik(w.x, w.z) + 1.4f;
                    Ampul("Yaklasma", isiklar, m.PistLambasi, pist.InverseTransformPoint(new Vector3(w.x, h, w.z)));
                }
            }

            // Landing zone: green edge lines, green lights and a beacon; shown only in the landing phase.
            float zb = GeceHaritasi.InisBolgesiBoyu, zm = yari - zb / 2f;
            var bolgeGo = new GameObject("Inis Bolgesi");
            bolgeGo.transform.SetParent(kok.transform, false);
            bolgeGo.transform.SetPositionAndRotation(pist.TransformPoint(new Vector3(0f, 0.3f, zm)), pist.rotation);
            var bolge = bolgeGo.AddComponent<InisBolgesi>();
            Transform bi = Grup("Isaretler", bolgeGo.transform);
            foreach (float s in new[] { -1f, 1f })
            {
                Parca(PrimitiveType.Cube, "Yesil Kenar", bi, m.InisCizgisi, new Vector3(s * (W / 2f - 1f), 0.03f, 0f), new Vector3(0.8f, 0.03f, zb));
                for (float z = -zb / 2f; z <= zb / 2f + 0.1f; z += 50f)
                    Ampul("Yesil Lamba", bi, m.YesilLamba, new Vector3(s * (W / 2f + 6f), 0.9f, z), 1.4f);
            }
            foreach (float uc in new[] { -1f, 1f })
                Parca(PrimitiveType.Cube, "Yesil Bant", bi, m.InisCizgisi, new Vector3(0f, 0.03f, uc * zb / 2f), new Vector3(W - 2f, 0.03f, 0.8f));
            var isaretHuzme = new GameObject("Isaret Huzmesi", typeof(MeshFilter), typeof(MeshRenderer));
            isaretHuzme.transform.SetParent(bi, false);
            isaretHuzme.transform.localPosition = new Vector3(0f, 0.5f, zb / 2f);
            isaretHuzme.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            isaretHuzme.transform.localScale = Vector3.one * 600f;
            isaretHuzme.GetComponent<MeshFilter>().sharedMesh = Lab7Varliklar.HuzmeMeshi(1.2f);
            isaretHuzme.GetComponent<MeshRenderer>().sharedMaterial = m.IsaretHuzmesi;
            isaretHuzme.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Isik("Yesil Isik", bi, LightType.Point, new Color(0.3f, 1f, 0.4f), 20f, 60f, new Vector3(0f, 4f, zb / 2f));
            var bso = new SerializedObject(bolge);
            bso.FindProperty("en").floatValue = GeceHaritasi.InisBolgesiEni;
            bso.FindProperty("boy").floatValue = zb;
            var arr = bso.FindProperty("isaretler");
            arr.arraySize = 1;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = bi.gameObject;
            bso.ApplyModifiedPropertiesWithoutUndo();
            sonuc.InisBolgesi = bolge;

            // Apron, taxiway, hangars, hut, fuel, windsock, floodlights on the north-west side (local −x).
            Transform tesis = Grup("Tesisler", pist);
            Parca(PrimitiveType.Cube, "Apron", tesis, m.Beton, new Vector3(-135f, 0.12f, -90f), new Vector3(120f, 0.25f, 230f), carpisici: true).layer = arazi;
            Parca(PrimitiveType.Cube, "Taksi Yolu", tesis, m.Beton, new Vector3(-50f, 0.12f, -90f), new Vector3(60f, 0.24f, 18f), carpisici: true).layer = arazi;
            for (int i = 0; i < 3; i++)
                HangarKur(tesis, m, yapi, new Vector3(-170f, 0f, -170f + i * 62f));
            GameObject kulube = Parca(PrimitiveType.Cube, "Harekat Kulubesi", tesis, m.Badana, new Vector3(-110f, 2.5f, 60f), new Vector3(12f, 5f, 7f), carpisici: true);
            kulube.layer = yapi;
            Parca(PrimitiveType.Cube, "Cati", tesis, m.Arduaz, new Vector3(-110f, 5.2f, 60f), new Vector3(13f, 0.4f, 8f));
            for (int k = 0; k < 3; k++)
                Parca(PrimitiveType.Cube, "Pencere", tesis, m.Pencere, new Vector3(-103.95f, 2.6f, 57f + k * 3f), new Vector3(0.05f, 1.2f, 1.6f));
            for (int k = 0; k < 2; k++)
            {
                GameObject tank = Parca(PrimitiveType.Cylinder, "Yakit Tanki", tesis, m.Tank, new Vector3(-165f, 2.5f, 70f + k * 9f), new Vector3(6f, 2.5f, 6f), carpisici: true);
                tank.layer = yapi;
            }
            Parca(PrimitiveType.Cylinder, "Ruzgarlik Diregi", tesis, m.Celik, new Vector3(-40f, 4f, 380f), new Vector3(0.2f, 4f, 0.2f));
            Parca(PrimitiveType.Cylinder, "Ruzgarlik", tesis, m.Turuncu, new Vector3(-38.5f, 7.4f, 380f), new Vector3(0.9f, 1.6f, 0.9f), Quaternion.Euler(0f, 0f, 80f));
            foreach (float z in new[] { -190f, 20f })
            {
                Parca(PrimitiveType.Cylinder, "Projektor Diregi", tesis, m.Celik, new Vector3(-90f, 6f, z), new Vector3(0.35f, 6f, 0.35f));
                Isik("Projektor", tesis, LightType.Spot, new Color(1f, 0.82f, 0.58f), 900f, 160f, new Vector3(-90f, 12f, z),
                    Quaternion.LookRotation(new Vector3(-80f, -12f, z < 0 ? 60f : -60f)), 75f, 45f);
                Ampul("Projektor Lambasi", tesis, m.SokakLambasi, new Vector3(-90f, 12f, z), 0.9f);
            }
            Katman(tesis.gameObject, yapi);
            tesis.Find("Apron").gameObject.layer = arazi;
            tesis.Find("Taksi Yolu").gameObject.layer = arazi;

            // Resupply: a low pass over the runway (≤ 150 m) refills the bombs.
            var ikmal = new GameObject("Ikmal Alani");
            ikmal.transform.SetParent(pist, false);
            sonuc.IkmalTetigi = Kutu(ikmal, new Vector3(0f, 75f, 0f), new Vector3(240f, 150f, L + 200f));
            sonuc.IkmalTetigi.isTrigger = true;
            return sonuc;
        }

        static void HangarKur(Transform ust, Mat m, int yapi, Vector3 konum)
        {
            Transform h = Grup("Hangar", ust, konum);
            Parca(PrimitiveType.Cylinder, "Cati", h, m.Hangar, Vector3.zero, new Vector3(26f, 17f, 26f), Quaternion.Euler(0f, 0f, 90f));
            Parca(PrimitiveType.Cube, "Kapi", h, m.Karanlik, new Vector3(17.05f, 4.5f, 0f), new Vector3(0.3f, 9f, 16f));
            Parca(PrimitiveType.Cube, "Kapi Isigi", h, m.Pencere, new Vector3(17.25f, 0.4f, 0f), new Vector3(0.1f, 0.5f, 15f));
            Kutu(h.gameObject, new Vector3(0f, 6.5f, 0f), new Vector3(34f, 13f, 26f));
            Katman(h.gameObject, yapi);
        }

        // ================================================================== bridge (paper H5–J5)

        /// <summary>
        /// The Bölüm 3 bridge over the wide reach H5–J5: 600 m, deck 25 m over the water (paper), twelve 50 m Pratt-truss spans
        /// on stone piers, abutments on the banks. Every 10 m truss panel is one falling piece with its own collider (the
        /// aircraft can hit the standing spans); when the bridge is destroyed only the panels within 60 m of the bomb fall
        /// (<see cref="Yikilabilir"/> cokmeYaricapi), the rest stays up.
        /// </summary>
        public static Yikilabilir KopruKur(Mat m, GameObject yangin, int hedef, int yapi, Collider su)
        {
            var kok = new GameObject("Kopru H5-J5");
            kok.transform.SetPositionAndRotation(GeceHaritasi.KopruMerkezi, Quaternion.LookRotation(GeceHaritasi.KopruEkseni));
            Transform t = kok.transform;
            const float W = GeceHaritasi.KopruEni, hx = W / 2f, H = 7f, L = GeceHaritasi.KopruBoyu;
            const int n = GeceHaritasi.KopruAcikligi;
            float aciklik = L / n;

            var paneller = new List<Transform>();
            var lambalar = new List<GameObject>();
            var kendi = new List<Collider> { su };
            Transform ust = Grup("Ust Yapi", t);
            for (int a = 0; a < n; a++)
            {
                float z0 = -L / 2f + a * aciklik;
                Transform ag = Grup($"Aciklik {a + 1}", ust, new Vector3(0f, 0f, z0 + aciklik / 2f));
                for (int p = 0; p < 5; p++)
                {
                    float zm = -aciklik / 2f + (p + 0.5f) * 10f;
                    Transform pn = Grup($"Panel {p + 1}", ag, new Vector3(0f, 0f, zm));
                    Parca(PrimitiveType.Cube, "Guverte", pn, m.Guverte, new Vector3(0f, -0.5f, 0f), new Vector3(W, 1f, 10f));
                    foreach (float x in new[] { -hx - 0.1f, hx + 0.1f })
                    {
                        Parca(PrimitiveType.Cube, "Alt Baslik", pn, m.Celik, new Vector3(x, -0.4f, 0f), new Vector3(0.6f, 0.9f, 10f));
                        Parca(PrimitiveType.Cube, "Ust Baslik", pn, m.Celik, new Vector3(x, H, 0f), new Vector3(0.6f, 0.6f, 10f));
                        Kiris("Dikme", pn, m.Celik, new Vector3(x, 0f, -5f), new Vector3(x, H, -5f), 0.45f);
                        bool sol = p < 2 || (p == 2 && x < 0f);
                        Kiris("Capraz", pn, m.Celik, new Vector3(x, 0f, sol ? -5f : 5f), new Vector3(x, H, sol ? 5f : -5f), 0.35f);
                    }
                    Kiris("Ust Kusak", pn, m.Celik, new Vector3(-hx - 0.1f, H, -5f), new Vector3(hx + 0.1f, H, -5f), 0.3f);
                    Kiris("Ruzgar Caprazi", pn, m.Celik, new Vector3(-hx, H, -5f), new Vector3(hx, H, 5f), 0.2f);
                    if (p == 4)
                    {
                        foreach (float x in new[] { -hx - 0.1f, hx + 0.1f })
                            Kiris("Son Dikme", pn, m.Celik, new Vector3(x, 0f, 5f), new Vector3(x, H, 5f), 0.45f);
                        Kiris("Ust Kusak", pn, m.Celik, new Vector3(-hx - 0.1f, H, 5f), new Vector3(hx + 0.1f, H, 5f), 0.3f);
                    }
                    // Lamps on the first panel of every second span (bulbs only; a real light on four of them).
                    if (p == 0 && a % 2 == 0)
                        foreach (float x in new[] { -hx + 0.4f, hx - 0.4f })
                        {
                            Transform lamba = Grup("Lamba", pn, new Vector3(x, 0f, -3f));
                            Parca(PrimitiveType.Cylinder, "Direk", lamba, m.Celik, new Vector3(0f, 1.6f, 0f), new Vector3(0.14f, 1.6f, 0.14f));
                            var yanik = new GameObject("Yanik");
                            yanik.transform.SetParent(lamba, false);
                            Ampul("Ampul", yanik.transform, m.SokakLambasi, new Vector3(0f, 3.3f, 0f), 0.5f);
                            if (x < 0f && (a == 0 || a == 4 || a == 6 || a == 10))
                                Isik("Isik", yanik.transform, LightType.Point, new Color(1f, 0.72f, 0.42f), 18f, 34f, new Vector3(0f, 3.2f, 0f));
                            lambalar.Add(yanik);
                        }
                    // The panel's own box: the aircraft hits the standing span; the same box falls with the panel.
                    Kutu(pn.gameObject, new Vector3(0f, H / 2f - 0.5f, 0f), new Vector3(W + 1.2f, H + 1.2f, 10f));
                    kendi.Add(pn.GetComponent<BoxCollider>());
                    paneller.Add(pn);
                }
            }

            // Piers on the riverbed / banks and the two abutments (stone, layer Yapi). The falling panels pass them and the
            // standing panels, so a span drops into the river instead of hanging on the pier tops.
            for (int k = 1; k < n; k++)
            {
                float z = -L / 2f + k * aciklik;
                Vector3 w = t.TransformPoint(new Vector3(0f, 0f, z));
                float zemin = Mathf.Min(GeceHaritasi.Yukseklik(w.x, w.z), GeceHaritasi.SuSeviyesi) - 2f - GeceHaritasi.KopruGuvertesi;
                float boy = -1f - zemin;
                GameObject ayak = Parca(PrimitiveType.Cube, "Ayak", t, m.Tas, new Vector3(0f, -1f - boy / 2f, z), new Vector3(W + 2.5f, boy, 4f), carpisici: true);
                ayak.layer = yapi;
                kendi.Add(ayak.GetComponent<Collider>());
            }
            foreach (float s in new[] { -1f, 1f })
            {
                Vector3 w = t.TransformPoint(new Vector3(0f, 0f, s * (L / 2f + 4f)));
                float zemin = GeceHaritasi.Yukseklik(w.x, w.z) - 3f - GeceHaritasi.KopruGuvertesi;
                float boy = -0.6f - zemin;
                GameObject kenar = Parca(PrimitiveType.Cube, "Kenar Ayagi", t, m.Tas, new Vector3(0f, -0.6f - boy / 2f, s * (L / 2f + 4f)), new Vector3(W + 4f, boy, 9f), carpisici: true);
                kenar.layer = yapi;
                kendi.Add(kenar.GetComponent<Collider>());
            }

            // Target collider over the whole superstructure (layer Hedef: the blast finds it, the aircraft crashes into it).
            BoxCollider hedefKutu = Kutu(kok, new Vector3(0f, 3f, 0f), new Vector3(W + 2f, 9f, L));
            kok.layer = hedef;
            foreach (Transform c in t)
                if (c.gameObject.layer == 0)
                    Katman(c.gameObject, yapi);

            // Fire points at the pier tops: the two next to the hit burn.
            Transform yanginlar = Grup("Yangin Noktalari", t);
            var noktalar = new List<Transform>();
            for (int k = 1; k < n; k++)
                noktalar.Add(Grup($"Ayak {k}", yanginlar, new Vector3(0f, 0.5f, -L / 2f + k * aciklik)));
            kok.AddComponent<Saglik>();
            var yk = kok.AddComponent<Yikilabilir>();
            var so = new SerializedObject(yk);
            so.FindProperty("tur").enumValueIndex = (int)HedefTuru.Kopru;
            so.FindProperty("gorunenAd").stringValue = "H5–J5 Köprüsü";
            so.FindProperty("puan").intValue = 400;
            so.FindProperty("hedefCarpisici").objectReferenceValue = hedefKutu;
            Dizi(so.FindProperty("dusenParcalar"), paneller.ToArray());
            Dizi(so.FindProperty("kapanacaklar"), lambalar.ToArray());
            so.FindProperty("yanginOn").objectReferenceValue = yangin;
            Dizi(so.FindProperty("yanginNoktalari"), noktalar.ToArray());
            so.FindProperty("parcaKatmani").intValue = yapi;
            so.FindProperty("itme").floatValue = 5f;
            so.FindProperty("itmeMerkezi").vector3Value = new Vector3(0f, -10f, 0f);
            so.FindProperty("donmaSuresi").floatValue = 15f;
            so.FindProperty("parcaYogunlugu").floatValue = 450f;
            so.FindProperty("cokmeYaricapi").floatValue = KopruCokmeYaricapi;
            Dizi(so.FindProperty("yoksayilanlar"), kendi);
            so.ApplyModifiedPropertiesWithoutUndo();
            return yk;
        }

        /// <summary>Horizontal distance (m) from the bomb within which the bridge's truss panels fall.</summary>
        public const float KopruCokmeYaricapi = 60f;

        // ================================================================== searchlight towers (paper F4, K5, H7)

        public static Yikilabilir KuleKur(int i, Mat m, GameObject yangin, Transform ucak, SesDinleyici kulak, int hedef, int yapi, int arazi)
        {
            Vector3 konum = GeceHaritasi.KuleKonumu(i);
            string kare = GeceHaritasi.IsildakKaresi(i);
            float yon = GeceHaritasi.IsildakYonu(i);
            var kok = new GameObject($"Isildak Kulesi {kare}");
            kok.transform.SetPositionAndRotation(konum - Vector3.up * 0.3f, Quaternion.Euler(0f, 20f + i * 31f, 0f));
            Transform t = kok.transform;
            const float H = GeceHaritasi.KuleYuksekligi;

            // Body: four timber legs with X bracing on three levels; one rigid piece when it falls.
            Transform govde = Grup("Govde", t);
            const float alt = 3.4f, ust = 2.1f;
            Vector3 Bacak(float sx, float sz, float y) { float a = Mathf.Lerp(alt, ust, y / H); return new Vector3(sx * a, y, sz * a); }
            var koseler = new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) };
            foreach (var (sx, sz) in koseler)
                Kiris("Bacak", govde, m.Ahsap, Bacak(sx, sz, 0f), Bacak(sx, sz, H), 0.55f);
            for (int s = 0; s < 3; s++)
            {
                float y0 = s * H / 3f, y1 = (s + 1) * H / 3f;
                for (int k = 0; k < 4; k++)
                {
                    var (ax, az) = koseler[k];
                    var (bx, bz) = koseler[(k + 1) % 4];
                    Kiris("Kusak", govde, m.Ahsap, Bacak(ax, az, y1), Bacak(bx, bz, y1), 0.3f);
                    Kiris("Capraz", govde, m.Ahsap, Bacak(ax, az, y0), Bacak(bx, bz, y1), 0.22f);
                    Kiris("Capraz", govde, m.Ahsap, Bacak(bx, bz, y0), Bacak(ax, az, y1), 0.22f);
                }
            }
            for (float y = 1.5f; y < H; y += 3f)
                Parca(PrimitiveType.Cube, "Merdiven", govde, m.Ahsap, new Vector3(-Mathf.Lerp(alt, ust, y / H) - 0.2f, y, 0f), new Vector3(0.15f, 0.12f, 1.1f));

            // Platform with railing and a little roof.
            Transform platform = Grup("Platform", t, new Vector3(0f, H, 0f));
            Parca(PrimitiveType.Cube, "Doseme", platform, m.Ahsap, new Vector3(0f, 0.2f, 0f), new Vector3(6.4f, 0.4f, 6.4f));
            for (int k = 0; k < 4; k++)
            {
                Quaternion q = Quaternion.Euler(0f, k * 90f, 0f);
                Parca(PrimitiveType.Cube, "Korkuluk", platform, m.Ahsap, q * new Vector3(0f, 1.3f, 3.1f), new Vector3(6.4f, 0.15f, 0.15f), q);
                Parca(PrimitiveType.Cube, "Siper", platform, m.Kum, q * new Vector3(0f, 0.75f, 3.1f), new Vector3(6.4f, 0.7f, 0.3f), q);
            }
            Parca(PrimitiveType.Cylinder, "Kaide", platform, m.Celik, new Vector3(0f, 0.75f, 0f), new Vector3(1.3f, 0.35f, 1.3f));

            // The searchlight: a turning head with the lens, the spot light and the visible beam (paper: 6 squares = 1200 m).
            Transform kafa = Grup("Isildak Kafasi", t, new Vector3(0f, H + 2.1f, 0f));
            Parca(PrimitiveType.Cylinder, "Gövde", kafa, m.Celik, Vector3.zero, new Vector3(1.6f, 0.75f, 1.6f), Quaternion.Euler(90f, 0f, 0f));
            GameObject mercek = Parca(PrimitiveType.Cylinder, "Mercek", kafa, m.Mercek, new Vector3(0f, 0f, 0.78f), new Vector3(1.45f, 0.03f, 1.45f), Quaternion.Euler(90f, 0f, 0f));
            mercek.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Light isik = Isik("Isik", kafa, LightType.Spot, new Color(1f, 0.95f, 0.82f), 260000f, GeceHaritasi.IsildakMenzili + 100f, new Vector3(0f, 0f, 0.9f), null, 7.5f, 3f);
            var huzme = new GameObject("Huzme", typeof(MeshFilter), typeof(MeshRenderer));
            huzme.transform.SetParent(kafa, false);
            huzme.transform.localPosition = new Vector3(0f, 0f, 0.82f);
            huzme.GetComponent<MeshFilter>().sharedMesh = Lab7Varliklar.HuzmeMeshi(3.2f);
            var hr = huzme.GetComponent<MeshRenderer>();
            hr.sharedMaterial = m.IsildakHuzmesi;
            hr.shadowCastingMode = ShadowCastingMode.Off;
            hr.receiveShadows = false;

            // Guard hut and sandbags at the foot (they stay).
            Transform dip = Grup("Dip", t);
            Parca(PrimitiveType.Cube, "Nobet Kulubesi", dip, m.Ahsap, new Vector3(6.5f, 1.3f, 2f), new Vector3(3f, 2.6f, 3f), carpisici: true);
            Parca(PrimitiveType.Cube, "Pencere", dip, m.Pencere, new Vector3(8.02f, 1.6f, 2f), new Vector3(0.05f, 0.7f, 1f));
            for (int k = 0; k < 10; k++)
            {
                float a = k * 36f;
                Parca(PrimitiveType.Cube, "Kum Torbasi", dip, m.Kum, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.4f, 5.2f), new Vector3(3.1f, 0.8f, 0.9f), Quaternion.Euler(0f, a, 0f));
            }
            Katman(dip.gameObject, yapi);

            BoxCollider hedefKutu = Kutu(kok, new Vector3(0f, (H + 4f) / 2f, 0f), new Vector3(8f, H + 4f, 8f));
            kok.layer = hedef;
            foreach (Transform c in t)
                if (c != dip)
                    Katman(c.gameObject, yapi);

            // The head starts in the paper direction (arrow on the map) and sweeps around it.
            var isildak = kafa.gameObject.AddComponent<Isildak>();
            kafa.rotation = Quaternion.Euler(-20f, yon, 0f);
            var iso = new SerializedObject(isildak);
            iso.FindProperty("hedef").objectReferenceValue = ucak;
            iso.FindProperty("kafa").objectReferenceValue = kafa;
            iso.FindProperty("isik").objectReferenceValue = isik;
            iso.FindProperty("huzme").objectReferenceValue = huzme.transform;
            iso.FindProperty("lamba").objectReferenceValue = mercek.GetComponent<MeshRenderer>();
            iso.FindProperty("menzil").floatValue = GeceHaritasi.IsildakMenzili;
            iso.FindProperty("taramaYonu").floatValue = yon;
            iso.FindProperty("taramaGenisligi").floatValue = 55f;
            iso.FindProperty("taramaPeriyodu").floatValue = 13f + i * 3f;
            iso.FindProperty("faz").floatValue = i * 2.1f;
            iso.FindProperty("engel").intValue = 1 << arazi;
            iso.FindProperty("kulak").objectReferenceValue = kulak;
            iso.ApplyModifiedPropertiesWithoutUndo();

            Transform yanginlar = Grup("Yangin Noktalari", t);
            var noktalar = new[] { Grup("Dip", yanginlar, new Vector3(0f, 0.3f, 0f)), Grup("Yan", yanginlar, new Vector3(-9f, 0.3f, 3f)) };
            kok.AddComponent<Saglik>();
            var yk = kok.AddComponent<Yikilabilir>();
            var so = new SerializedObject(yk);
            so.FindProperty("tur").enumValueIndex = (int)HedefTuru.Kule;
            so.FindProperty("gorunenAd").stringValue = $"{kare} Işıldağı";
            so.FindProperty("puan").intValue = 150;
            so.FindProperty("hedefCarpisici").objectReferenceValue = hedefKutu;
            Dizi(so.FindProperty("dusenParcalar"), new Object[] { govde, platform, kafa });
            so.FindProperty("isildak").objectReferenceValue = isildak;
            so.FindProperty("yanginOn").objectReferenceValue = yangin;
            Dizi(so.FindProperty("yanginNoktalari"), noktalar);
            so.FindProperty("parcaKatmani").intValue = yapi;
            so.FindProperty("itme").floatValue = 2.5f;
            so.FindProperty("itmeMerkezi").vector3Value = new Vector3(4f, 2f, 0f);
            so.FindProperty("donme").vector3Value = new Vector3(0f, 0f, 0.9f);
            so.FindProperty("parcaYogunlugu").floatValue = 120f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return yk;
        }

        // ================================================================== AA guns (paper G4, K4, I7)

        public static UcaksavarMevzii UcaksavarKur(int i, Mat m, UcusKontrol ucus, Mermi mermiOn, Isildak[] isildaklar, SesDinleyici kulak,
            int yapi, int engelMaskesi, int vurulacak)
        {
            Vector3 konum = GeceHaritasi.UcaksavarKonumu(i);
            string kare = Bolum3Harita.Ucaksavarlar[i];
            var go = new GameObject($"Ucaksavar {kare}");
            Vector3 bak = GeceHaritasi.KopruMerkezi - konum;
            go.transform.SetPositionAndRotation(konum, Quaternion.LookRotation(new Vector3(bak.x, 0f, bak.z)));
            Transform t = go.transform;
            for (int k = 0; k < 12; k++)
            {
                float a = k * 30f;
                Parca(PrimitiveType.Cube, "Kum Torbasi", t, m.Kum, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.55f, 3.6f), new Vector3(2f, 1.1f, 1f), Quaternion.Euler(0f, a, 0f));
            }
            Parca(PrimitiveType.Cylinder, "Kaide", t, m.Celik, new Vector3(0f, 1.1f, 0f), new Vector3(1.1f, 0.6f, 1.1f));
            var namlu = Grup("Namlu", t, new Vector3(0f, 2f, 0f));
            Parca(PrimitiveType.Cylinder, "Boru", namlu, m.Celik, new Vector3(0f, 0f, 1.6f), new Vector3(0.22f, 1.6f, 0.22f), Quaternion.Euler(90f, 0f, 0f));
            Parca(PrimitiveType.Cube, "Kalkan", namlu, m.Celik, new Vector3(0f, 0.2f, 0.5f), new Vector3(1.8f, 1.2f, 0.08f));
            Kutu(go, new Vector3(0f, 0.6f, 0f), new Vector3(8f, 1.2f, 8f));

            var radar = go.AddComponent<Radar>();
            radar.Hedef = ucus.transform;
            radar.YariAci = 120f;
            radar.Menzil = GeceHaritasi.UcaksavarYaricapi;
            radar.EngelKatmani = engelMaskesi;
            radar.GozYuksekligi = 3f;
            radar.DenetimAraligi = 0.2f;
            var ua = go.AddComponent<UcaksavarMevzii>();
            var so = new SerializedObject(ua);
            so.FindProperty("ucak").objectReferenceValue = ucus;
            so.FindProperty("goz").objectReferenceValue = radar;
            so.FindProperty("kulak").objectReferenceValue = kulak;
            Dizi(so.FindProperty("isildaklar"), isildaklar);
            so.FindProperty("geceGorusu").floatValue = 250f;
            so.FindProperty("mermiOn").objectReferenceValue = mermiOn;
            so.FindProperty("namlu").objectReferenceValue = namlu;
            so.FindProperty("vurulacak").intValue = vurulacak;
            so.FindProperty("menzil").floatValue = GeceHaritasi.UcaksavarYaricapi;   // paper: Ø 1600 m
            so.FindProperty("atisAraligi").floatValue = 0.35f;
            so.FindProperty("mermiHasari").intValue = 6;
            so.FindProperty("tohum").intValue = 70 + i;
            so.ApplyModifiedPropertiesWithoutUndo();
            Katman(go, yapi);
            return ua;
        }

        // ================================================================== sound locator (paper H3)

        /// <summary>
        /// The sound locator at H3 (paper "S", Ø 2000 m): a period acoustic locator (four horns on a turning frame) with the
        /// Lab 5 <see cref="SesDinleyici"/>: it hears the engine within 1000 m (not below the silence throttle) and reports
        /// where the aircraft was, the sound being late. The searchlights turn to that point and the guns fire at it.
        /// </summary>
        public static SesDinleyici SesDinleyiciKur(Mat m, UcusKontrol ucus, int yapi)
        {
            Vector3 konum = GeceHaritasi.SesDinleyiciKonumu;
            var go = new GameObject($"Ses Dinleyici {Bolum3Harita.SesDinleyici}");
            Vector3 bak = GeceHaritasi.KopruMerkezi - konum;
            go.transform.SetPositionAndRotation(konum, Quaternion.LookRotation(new Vector3(bak.x, 0f, bak.z)));
            Transform t = go.transform;
            Parca(PrimitiveType.Cylinder, "Kaide", t, m.Beton, new Vector3(0f, 0.2f, 0f), new Vector3(7f, 0.2f, 7f), carpisici: true);
            Parca(PrimitiveType.Cylinder, "Mil", t, m.Celik, new Vector3(0f, 2.2f, 0f), new Vector3(0.5f, 2f, 0.5f));
            Transform cerceve = Grup("Cerceve", t, new Vector3(0f, 4.4f, 0f), Quaternion.Euler(-12f, 0f, 0f));
            Parca(PrimitiveType.Cube, "Yatay Kol", cerceve, m.Celik, Vector3.zero, new Vector3(6.4f, 0.3f, 0.3f));
            Parca(PrimitiveType.Cube, "Dikey Kol", cerceve, m.Celik, Vector3.zero, new Vector3(0.3f, 4.6f, 0.3f));
            foreach (Vector3 p in new[] { new Vector3(-3f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(0f, -2.1f, 0f), new Vector3(0f, 2.1f, 0f) })
            {
                Transform boru = Grup("Boru", cerceve, p);
                Parca(PrimitiveType.Cylinder, "Govde", boru, m.Hangar, new Vector3(0f, 0f, 0.6f), new Vector3(0.6f, 1.4f, 0.6f), Quaternion.Euler(90f, 0f, 0f));
                Parca(PrimitiveType.Cylinder, "Agiz", boru, m.Hangar, new Vector3(0f, 0f, 2.1f), new Vector3(1.9f, 0.12f, 1.9f), Quaternion.Euler(90f, 0f, 0f));
                Parca(PrimitiveType.Cylinder, "Ic", boru, m.Karanlik, new Vector3(0f, 0f, 2.2f), new Vector3(1.6f, 0.02f, 1.6f), Quaternion.Euler(90f, 0f, 0f));
            }
            for (int k = 0; k < 2; k++)
                Parca(PrimitiveType.Cube, "Koltuk", t, m.Ahsap, new Vector3(k == 0 ? -1.6f : 1.6f, 1f, -1.2f), new Vector3(0.8f, 0.8f, 0.8f));
            for (int k = 0; k < 14; k++)
            {
                float a = k * (360f / 14f);
                Parca(PrimitiveType.Cube, "Kum Torbasi", t, m.Kum, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.45f, 5.6f), new Vector3(2.4f, 0.9f, 1f), Quaternion.Euler(0f, a, 0f));
            }
            Ampul("Kirmizi Lamba", t, Lab7Varliklar.Ampul("SesLambasi", new Color(1f, 0.2f, 0.12f), 4f), new Vector3(0f, 0.9f, 3.4f), 0.35f);
            Katman(go, yapi);

            var kulak = go.AddComponent<SesDinleyici>();
            var so = new SerializedObject(kulak);
            so.FindProperty("ucak").objectReferenceValue = ucus;
            so.FindProperty("duymaMenzili").floatValue = GeceHaritasi.SesYaricapi;   // paper ring Ø 2000 m
            so.FindProperty("sessizlikEsigi").floatValue = 0.15f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return kulak;
        }

        // ================================================================== clouds (paper single rule, E5–G5 at Orta)

        /// <summary>One cloud square: the <see cref="Bulut"/> hiding box and a slow, moon-lit particle cloud filling it.</summary>
        public static Bulut BulutKur(string kare, Material mat)
        {
            Bounds b = GeceHaritasi.BulutKutusu(kare);
            var go = new GameObject($"Bulut {kare}");
            go.transform.position = b.center;
            var bulut = go.AddComponent<Bulut>();
            bulut.Ayarla(kare, b.size);

            var gorsel = new GameObject("Gorsel");
            gorsel.transform.SetParent(go.transform, false);
            var ps = gorsel.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var ana = ps.main;
            ana.loop = true;
            ana.duration = 30f;
            ana.prewarm = true;
            ana.playOnAwake = true;
            ana.simulationSpace = ParticleSystemSimulationSpace.World;
            ana.startLifetime = new ParticleSystem.MinMaxCurve(55f, 80f);
            ana.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            ana.startSize = new ParticleSystem.MinMaxCurve(70f, 120f);
            ana.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            ana.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.55f), new Color(0.85f, 0.88f, 0.95f, 0.75f));
            ana.maxParticles = 40;
            var e = ps.emission;
            e.rateOverTime = 0.6f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(b.size.x * 0.8f, b.size.y * 0.45f, b.size.z * 0.8f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            var r = gorsel.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.maxParticleSize = 3f;
            return bulut;
        }

        // ================================================================== villages

        public static GameObject KoyKur(int k, Mat m, int yapi)
        {
            var (merkez, ev, tohum) = GeceHaritasi.Koyler[k];
            var kok = new GameObject($"Koy {k + 1}");
            kok.transform.position = GeceHaritasi.Zeminde(merkez.x, merkez.y);
            var r = new System.Random(tohum);
            float R() => (float)r.NextDouble();
            Material[] duvarlar = { m.Badana, m.Toprak, m.TasDuvar };
            Material[] catilar = { m.Kiremit, m.Kiremit, m.Arduaz };
            int konan = 0;
            for (int deneme = 0; deneme < ev * 6 && konan < ev; deneme++)
            {
                float a = R() * Mathf.PI * 2f, d = 25f + R() * 105f;
                float x = merkez.x + Mathf.Cos(a) * d, z = merkez.y + Mathf.Sin(a) * d;
                if (GeceHaritasi.NehirUzakligi(x, z) < 75f || GeceHaritasi.YolUzakligi(x, z) < 11f)
                    continue;
                if (GeceHaritasi.Normal(x, z).y < 0.94f)
                    continue;
                float w = 7f + R() * 5f, dd = 6f + R() * 3f, h = 3.8f + R() * 1.6f;
                var evGo = new GameObject($"Ev {konan + 1}");
                evGo.transform.SetParent(kok.transform, false);
                evGo.transform.SetPositionAndRotation(new Vector3(x, GeceHaritasi.Yukseklik(x, z) - 0.4f, z), Quaternion.Euler(0f, R() * 360f, 0f));
                EvKur(evGo.transform, m, duvarlar[(int)(R() * 2.999f)], catilar[(int)(R() * 2.999f)], w, dd, h, R);
                Katman(evGo, yapi);
                konan++;
            }
            Transform lamba = Grup("Koy Lambasi", kok.transform);
            lamba.position = GeceHaritasi.Zeminde(merkez.x + 6f, merkez.y + 6f);
            Parca(PrimitiveType.Cylinder, "Direk", lamba, m.Ahsap, new Vector3(0f, 2.5f, 0f), new Vector3(0.2f, 2.5f, 0.2f));
            Ampul("Ampul", lamba, m.SokakLambasi, new Vector3(0f, 5.1f, 0f), 0.6f);
            Isik("Isik", lamba, LightType.Point, new Color(1f, 0.72f, 0.42f), 25f, 45f, new Vector3(0f, 5f, 0f));
            return kok;
        }

        static void EvKur(Transform t, Mat m, Material duvar, Material cati, float w, float d, float h, System.Func<float> R)
        {
            Parca(PrimitiveType.Cube, "Duvar", t, duvar, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d));
            float s = d / Mathf.Sqrt(2f);
            Parca(PrimitiveType.Cube, "Alinlik", t, duvar, new Vector3(0f, h, 0f), new Vector3(w - 0.05f, s, s), Quaternion.Euler(45f, 0f, 0f));
            float egim = d / 2f * Mathf.Sqrt(2f) + 0.7f;
            foreach (float yan in new[] { -1f, 1f })
                Parca(PrimitiveType.Cube, "Cati", t, cati, new Vector3(0f, h + d / 4f + 0.18f, yan * d / 4f), new Vector3(w + 0.8f, 0.3f, egim), Quaternion.Euler(yan * 45f, 0f, 0f));
            Parca(PrimitiveType.Cube, "Baca", t, m.TasDuvar, new Vector3(w * 0.3f, h + d * 0.4f, d * 0.15f), new Vector3(0.7f, 2f, 0.7f));
            Parca(PrimitiveType.Cube, "Kapi", t, m.Ahsap, new Vector3(0f, 1f, d / 2f + 0.03f), new Vector3(1.1f, 2f, 0.06f));
            foreach (float yan in new[] { -1f, 1f })
                foreach (float x in new[] { -w / 3.2f, w / 3.2f })
                    Parca(PrimitiveType.Cube, "Pencere", t, R() < 0.45f ? m.Pencere : m.Cam, new Vector3(x, h * 0.55f, yan * (d / 2f + 0.03f)), new Vector3(1.1f, 1.2f, 0.06f));
            Kutu(t.gameObject, new Vector3(0f, (h + d / 2f) / 2f, 0f), new Vector3(w, h + d / 2f, d));
        }

        // ================================================================== misc

        public static void Dizi<T>(SerializedProperty dizi, IList<T> ogeler) where T : Object
        {
            dizi.arraySize = ogeler.Count;
            for (int i = 0; i < ogeler.Count; i++)
                dizi.GetArrayElementAtIndex(i).objectReferenceValue = ogeler[i];
        }
    }
}
