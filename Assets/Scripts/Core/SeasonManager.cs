using UnityEngine;
using UnityEngine.Serialization;
using System;
using ForrestGame.Data;

namespace ForrestGame.Core
{
    public enum MevsimTuru { Ilkbahar, Yaz, Sonbahar, Kis }

    public class SeasonManager : MonoBehaviour
    {
        public static SeasonManager Instance;

        public static event Action<MevsimTuru> OnMevsimDegisti;

        [Header("Tasarım Ayarları")]
        public SeasonSettings ayarlar;

        [Header("Anlık Durum (Sadece Bilgi İçin)")]
        public MevsimTuru aktifMevsim;
        public float kalanSaniye;

        [Header("Görsel Efektler (Sahnedeki Objeleri Sürükle Bırak)")]
        [Tooltip("Sahnedeki Kar, Yaprak dökümü gibi objeleri (Particle System) buraya sürükle.")]
        public GameObject ilkbaharVFX;
        public GameObject yazVFX;
        public GameObject sonbaharVFX;
        public GameObject kisVFX;

        [Header("Snow Tree Materials (Using SH_MevsimAgac)")]
        [FormerlySerializedAs("agacKarMateryalleri")]
        public Material[] snowTreeMaterials;

        [Header("Autumn Leaf Color Blend (Using SH_MevsimAgac _TabanRengi)")]
        [Tooltip("Only leaf materials (e.g. leafsGreen 1/2, leafsDark) go here, not bark materials.")]
        public Material[] autumnLeafMaterials;
        [Tooltip("Normal (non-autumn) base color for each material above, matched by index. Set explicitly so a previous session's leftover material state can never be picked up as the baseline.")]
        public Color[] baseLeafColors;
        [Tooltip("Target autumn color for each material above, matched by index.")]
        public Color[] autumnLeafColors;

        [Header("Sonbahar Rain Fade")]
        [Tooltip("Yagmurun tam yogunluktaki emisyon orani. Sonbahar'in baslangic/bitis kenarlarinda 0'a kadar iner.")]
        public float sonbaharRainMaxEmissionRate = 180f;

        [Header("Ilkbahar Flowers (Terrain Detail Layers)")]
        [Tooltip("Cicekleri fircaladiktan sonra 'Tools/Ilkbahar Cicek Yogunlugunu Kaydet' ile olusan baseline asset'lerini (her cicek rengi/layer'i icin birer tane) buraya sürükle.")]
        public TerrainDetailBaseline[] ilkbaharCicekBaselineleri;
        private float lastAppliedFlowerAmount = -1f;

        private SnowStormController[] _sonbaharSnowStormControllers;
        private ParticleSystem _sonbaharRainParticles;
        private bool _sonbaharVFXCached;

        private SnowStormController _kisSnowStorm;
        private bool _kisVFXCached;

        [Header("Terrain Snow Blend")]
        [Tooltip("Terrain'deki toprak (ground) Terrain Layer'inin index'i.")]
        public int groundTerrainLayerIndex = 0;
        [Tooltip("Terrain'deki kar (snow) Terrain Layer'inin index'i.")]
        public int snowTerrainLayerIndex = 1;
        [Tooltip("Terrain'deki karsiz yol (road) Terrain Layer'inin index'i.")]
        public int roadTerrainLayerIndex = 2;
        [Tooltip("Terrain'deki karli yol (roadSnow) Terrain Layer'inin index'i.")]
        public int roadSnowTerrainLayerIndex = 3;

        private float lastAppliedTerrainSnowAmount = -1f;

        [Header("Directional Light Renk Gecisi")]
        [Tooltip("Sahnedeki gunes/ay isigi (Directional Light).")]
        public Light directionalLight;
        public Color ilkbaharLightColor = Color.white;
        public Color yazLightColor = new Color(1f, 0.968f, 0.780f); // #FFF7C7
        public Color sonbaharLightColor = Color.white;
        public Color kisLightColor = new Color(0.69f, 0.74f, 0.80f); // #B0BDCC daha soluk/soguk, gri-mavi kis isigi
        public float ilkbaharLightIntensity = 1f;
        public float yazLightIntensity = 1f;
        public float sonbaharLightIntensity = 1f;
        public float kisLightIntensity = 0.7f; // kapali/kararmis hava hissi

        private static readonly int SnowAmountID = Shader.PropertyToID("_KarMiktari");
        private static readonly int BaseColorID = Shader.PropertyToID("_TabanRengi");

        /// <summary>
        /// Bir mevsimin ilk %10'u baslangic gecisi, son %10'u bitis gecisidir (ayarlar.gecisOrani'nin yarisi her kenara).
        /// Ayni tarif tum mevsimlere ve tum gorsel kanallara (isik, kar, yaprak rengi, sonbahar VFX) uygulanir.
        /// </summary>
        private float EdgeDuration => ayarlar.mevsimSuresiDakika * 60f * (ayarlar.gecisOrani * 0.5f);

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            HesaplaCevrimdisiZaman();
            MevsimVFXAktifligiUygula();
        }

        private void Update()
        {
            kalanSaniye -= Time.deltaTime;

            if (kalanSaniye <= 0)
            {
                MevsimeGec(SonrakiMevsimiBul(aktifMevsim));
            }

            ApplyContinuousSeasonEffects();
        }

        /// <summary>
        /// Sadece bir mevsimin kendi suresi icinde var olan efektler (kar, sonbahar yaprak/yagmur) icin
        /// 0 -> 1 -> 0 seklinde bir "darbe" degeri. Baslangic ve bitis kenarlarinda EdgeDuration suresince yumusar.
        /// </summary>
        private float GetSeasonPulseAmount(MevsimTuru hedefMevsim)
        {
            if (aktifMevsim != hedefMevsim) return 0f;

            float edge = EdgeDuration;
            if (edge <= 0f) return 1f;

            float toplamSure = ayarlar.mevsimSuresiDakika * 60f;
            float gecenSure = toplamSure - kalanSaniye;

            if (gecenSure < edge) return Mathf.Clamp01(gecenSure / edge);
            if (kalanSaniye < edge) return Mathf.Clamp01(kalanSaniye / edge);
            return 1f;
        }

        /// <summary>
        /// Isik gibi tum mevsimlerde farkli bir hedefi olan surekli kanallar icin komsu mevsimle harmanlanmis agirlik.
        /// Sinirda (mevsim degisim ani) her iki tarafta da 0.5 olacak sekilde simetriktir, ani sicrama olmaz.
        /// </summary>
        private float GetOwnSeasonWeight()
        {
            float edge = EdgeDuration;
            if (edge <= 0f) return 1f;

            float toplamSure = ayarlar.mevsimSuresiDakika * 60f;
            float gecenSure = toplamSure - kalanSaniye;

            if (gecenSure < edge) return 0.5f + 0.5f * Mathf.Clamp01(gecenSure / edge);
            if (kalanSaniye < edge) return 0.5f + 0.5f * Mathf.Clamp01(kalanSaniye / edge);
            return 1f;
        }

        private void ApplyContinuousSeasonEffects()
        {
            ApplyContinuousLight();

            float kisMiktari = GetSeasonPulseAmount(MevsimTuru.Kis);
            ApplySnowAmount(kisMiktari);
            if (kisVFX != null)
            {
                if (!_kisVFXCached)
                {
                    _kisSnowStorm = kisVFX.GetComponent<SnowStormController>();
                    _kisVFXCached = true;
                }
                if (_kisSnowStorm != null) _kisSnowStorm.SetIntensityScale(kisMiktari);
            }

            float sonbaharMiktari = GetSeasonPulseAmount(MevsimTuru.Sonbahar);
            ApplyAutumnLeafColor(sonbaharMiktari);
            ApplySonbaharVFXYogunlugu(sonbaharMiktari);
        }

        /// <summary>
        /// Cicek yogunlugu her frame degil, sadece mevsim degistigi anda bir kere (sert 0/1 anahtar) uygulanir.
        /// Boylece her frame yuzlerce/binlerce detail hucresini yeniden yazma yuku olmaz.
        /// </summary>
        private void ApplyIlkbaharCicekYogunlugu()
        {
            if (ilkbaharCicekBaselineleri == null || ilkbaharCicekBaselineleri.Length == 0) return;
            if (Terrain.activeTerrain == null || Terrain.activeTerrain.terrainData == null) return;

            float amount = aktifMevsim == MevsimTuru.Ilkbahar ? 1f : 0f;
            if (Mathf.Approximately(amount, lastAppliedFlowerAmount)) return;

            lastAppliedFlowerAmount = amount;

            TerrainData terrainData = Terrain.activeTerrain.terrainData;

            foreach (TerrainDetailBaseline baseline in ilkbaharCicekBaselineleri)
            {
                if (baseline == null) continue;

                int layerIndex = baseline.detailLayerIndex;
                if (layerIndex >= terrainData.detailPrototypes.Length) continue;

                int width = baseline.width;
                int height = baseline.height;
                int[,] scaled = new int[height, width];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        scaled[y, x] = Mathf.RoundToInt(baseline.density[y * width + x] * amount);
                    }
                }

                terrainData.SetDetailLayer(0, 0, layerIndex, scaled);
            }
        }

        private void ApplyContinuousLight()
        {
            if (directionalLight == null) return;

            float agirlik = GetOwnSeasonWeight();
            float edge = EdgeDuration;
            float toplamSure = ayarlar.mevsimSuresiDakika * 60f;
            float gecenSure = toplamSure - kalanSaniye;

            MevsimTuru komsuMevsim;
            if (edge > 0f && gecenSure < edge)
            {
                komsuMevsim = OncekiMevsimiBul(aktifMevsim);
            }
            else if (edge > 0f && kalanSaniye < edge)
            {
                komsuMevsim = SonrakiMevsimiBul(aktifMevsim);
            }
            else
            {
                komsuMevsim = aktifMevsim;
            }

            Color hedefRenk = Color.Lerp(GetLightColorForSeason(komsuMevsim), GetLightColorForSeason(aktifMevsim), agirlik);
            float hedefYogunluk = Mathf.Lerp(GetLightIntensityForSeason(komsuMevsim), GetLightIntensityForSeason(aktifMevsim), agirlik);

            directionalLight.color = hedefRenk;
            directionalLight.intensity = hedefYogunluk;
        }

        private void HesaplaCevrimdisiZaman()
        {
            string sonGirisString = PlayerPrefs.GetString("SonCikisZamani", DateTime.Now.ToString());
            DateTime sonGiris = DateTime.Parse(sonGirisString);

            TimeSpan gecenZaman = DateTime.Now - sonGiris;
            double gecenSaniye = gecenZaman.TotalSeconds;

            aktifMevsim = (MevsimTuru)PlayerPrefs.GetInt("AktifMevsim", 0);
            kalanSaniye = PlayerPrefs.GetFloat("KalanMevsimSaniyesi", ayarlar.mevsimSuresiDakika * 60);

            kalanSaniye -= (float)gecenSaniye;

            while (kalanSaniye <= 0)
            {
                aktifMevsim = SonrakiMevsimiBul(aktifMevsim);
                kalanSaniye += ayarlar.mevsimSuresiDakika * 60;
            }

            Debug.Log($"[Season] Oyuna dönüldü. Mevsim: {aktifMevsim}. Bitmesine {(kalanSaniye/60):F1} dakika var.");
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.SetString("SonCikisZamani", DateTime.Now.ToString());
            PlayerPrefs.SetInt("AktifMevsim", (int)aktifMevsim);
            PlayerPrefs.SetFloat("KalanMevsimSaniyesi", kalanSaniye);
            PlayerPrefs.Save();
        }

        [ContextMenu("Test: Go To Winter")]
        private void TestGoToWinter()
        {
            MevsimeGec(MevsimTuru.Kis);
        }

        [ContextMenu("Test: Go To Summer")]
        private void TestGoToSummer()
        {
            MevsimeGec(MevsimTuru.Yaz);
        }

        [ContextMenu("Test: Reset Season Save")]
        private void TestResetSeasonSave()
        {
            PlayerPrefs.DeleteKey("SonCikisZamani");
            PlayerPrefs.DeleteKey("AktifMevsim");
            PlayerPrefs.DeleteKey("KalanMevsimSaniyesi");
            PlayerPrefs.Save();
            Debug.Log("[Season] Saved season state cleared. Next Play will start from Ilkbahar.");
        }

        public void MevsimeGec(MevsimTuru yeniMevsim)
        {
            aktifMevsim = yeniMevsim;
            kalanSaniye = ayarlar.mevsimSuresiDakika * 60;

            Debug.Log($"[Season] Mevsim Değişti: {yeniMevsim} geldi!");

            MevsimVFXAktifligiUygula();

            OnMevsimDegisti?.Invoke(yeniMevsim);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(yeniMevsim.ToString() + "Muzik"); // Örn: "KisMuzik" isimli sesi çalar
            }
        }

        private void MevsimVFXAktifligiUygula()
        {
            if (ilkbaharVFX != null) ilkbaharVFX.SetActive(false);
            if (yazVFX != null) yazVFX.SetActive(false);
            if (sonbaharVFX != null) sonbaharVFX.SetActive(false);
            if (kisVFX != null) kisVFX.SetActive(false);

            switch (aktifMevsim)
            {
                case MevsimTuru.Ilkbahar: if (ilkbaharVFX != null) ilkbaharVFX.SetActive(true); break;
                case MevsimTuru.Yaz: if (yazVFX != null) yazVFX.SetActive(true); break;
                case MevsimTuru.Sonbahar: if (sonbaharVFX != null) sonbaharVFX.SetActive(true); break;
                case MevsimTuru.Kis: if (kisVFX != null) kisVFX.SetActive(true); break;
            }

            ApplyIlkbaharCicekYogunlugu();
            ApplyContinuousSeasonEffects();
        }

        private Color GetLightColorForSeason(MevsimTuru mevsim)
        {
            switch (mevsim)
            {
                case MevsimTuru.Ilkbahar: return ilkbaharLightColor;
                case MevsimTuru.Yaz: return yazLightColor;
                case MevsimTuru.Sonbahar: return sonbaharLightColor;
                case MevsimTuru.Kis: return kisLightColor;
                default: return Color.white;
            }
        }

        private float GetLightIntensityForSeason(MevsimTuru mevsim)
        {
            switch (mevsim)
            {
                case MevsimTuru.Ilkbahar: return ilkbaharLightIntensity;
                case MevsimTuru.Yaz: return yazLightIntensity;
                case MevsimTuru.Sonbahar: return sonbaharLightIntensity;
                case MevsimTuru.Kis: return kisLightIntensity;
                default: return 1f;
            }
        }

        private void ApplyAutumnLeafColor(float amount)
        {
            if (autumnLeafMaterials == null) return;

            for (int i = 0; i < autumnLeafMaterials.Length; i++)
            {
                Material mat = autumnLeafMaterials[i];
                if (mat == null) continue;
                if (baseLeafColors == null || i >= baseLeafColors.Length) continue;
                if (autumnLeafColors == null || i >= autumnLeafColors.Length) continue;

                Color blended = Color.Lerp(baseLeafColors[i], autumnLeafColors[i], amount);
                mat.SetColor(BaseColorID, blended);
            }
        }

        private void ApplySonbaharVFXYogunlugu(float amount)
        {
            if (sonbaharVFX == null) return;

            if (!_sonbaharVFXCached)
            {
                _sonbaharSnowStormControllers = sonbaharVFX.GetComponentsInChildren<SnowStormController>(true);

                Transform rainTransform = sonbaharVFX.transform.Find("Rain");
                _sonbaharRainParticles = rainTransform != null ? rainTransform.GetComponent<ParticleSystem>() : null;

                _sonbaharVFXCached = true;
            }

            foreach (SnowStormController controller in _sonbaharSnowStormControllers)
            {
                controller.SetIntensityScale(amount);
            }

            if (_sonbaharRainParticles == null) return;

            ParticleSystem.EmissionModule emission = _sonbaharRainParticles.emission;
            emission.rateOverTime = amount * sonbaharRainMaxEmissionRate;
        }

        private void ApplySnowAmount(float snowAmount)
        {
            if (snowTreeMaterials != null)
            {
                foreach (Material mat in snowTreeMaterials)
                {
                    if (mat != null) mat.SetFloat(SnowAmountID, snowAmount);
                }
            }

            if (Terrain.activeTerrain != null && Terrain.activeTerrain.terrainData != null)
            {
                TerrainData terrainData = Terrain.activeTerrain.terrainData;

                if (Mathf.Abs(snowAmount - lastAppliedTerrainSnowAmount) > 0.01f)
                {
                    lastAppliedTerrainSnowAmount = snowAmount;
                    ApplyTerrainSnowBlend(terrainData, snowAmount);
                }
            }
        }

        private void ApplyTerrainSnowBlend(TerrainData terrainData, float snowAmount)
        {
            int layerCount = terrainData.alphamapLayers;
            bool blendGround = groundTerrainLayerIndex < layerCount && snowTerrainLayerIndex < layerCount;
            bool blendRoad = roadTerrainLayerIndex < layerCount && roadSnowTerrainLayerIndex < layerCount;

            if (!blendGround && !blendRoad)
            {
                return;
            }

            int width = terrainData.alphamapWidth;
            int height = terrainData.alphamapHeight;
            float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (blendGround)
                    {
                        float groundPlusSnow = alphamaps[y, x, groundTerrainLayerIndex] + alphamaps[y, x, snowTerrainLayerIndex];
                        alphamaps[y, x, snowTerrainLayerIndex] = groundPlusSnow * snowAmount;
                        alphamaps[y, x, groundTerrainLayerIndex] = groundPlusSnow * (1f - snowAmount);
                    }

                    if (blendRoad)
                    {
                        float roadPlusRoadSnow = alphamaps[y, x, roadTerrainLayerIndex] + alphamaps[y, x, roadSnowTerrainLayerIndex];
                        alphamaps[y, x, roadSnowTerrainLayerIndex] = roadPlusRoadSnow * snowAmount;
                        alphamaps[y, x, roadTerrainLayerIndex] = roadPlusRoadSnow * (1f - snowAmount);
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        private MevsimTuru SonrakiMevsimiBul(MevsimTuru suanki)
        {
            if (suanki == MevsimTuru.Ilkbahar) return MevsimTuru.Yaz;
            if (suanki == MevsimTuru.Yaz) return MevsimTuru.Sonbahar;
            if (suanki == MevsimTuru.Sonbahar) return MevsimTuru.Kis;
            return MevsimTuru.Ilkbahar;
        }

        private MevsimTuru OncekiMevsimiBul(MevsimTuru suanki)
        {
            if (suanki == MevsimTuru.Yaz) return MevsimTuru.Ilkbahar;
            if (suanki == MevsimTuru.Sonbahar) return MevsimTuru.Yaz;
            if (suanki == MevsimTuru.Kis) return MevsimTuru.Sonbahar;
            return MevsimTuru.Kis;
        }
    }
}
