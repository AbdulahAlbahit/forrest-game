using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections;
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
        public float lightColorTransitionDuration = 5f;

        private Coroutine lightColorTransitionCoroutine;

        [Tooltip("How many seconds the snow amount takes to smoothly transition between 0 and 1")]
        [FormerlySerializedAs("karGecisSuresiSaniye")]
        public float snowTransitionDuration = 5f;

        [Header("Winter Approach Preview")]
        [Tooltip("How many minutes before Kis begins that light snow starts falling")]
        public float transitionDurationMinutes = 5f;
        [Range(0f, 1f)]
        [Tooltip("Snow amount cap during the pre-winter preview window (full snow only starts once Kis officially begins)")]
        public float lightSnowCapDuringTransition = 0.15f;
        [Range(0f, 1f)]
        [Tooltip("Snow particle intensity scale during the pre-winter preview window")]
        public float lightSnowIntensityDuringTransition = 0.25f;

        private static readonly int SnowAmountID = Shader.PropertyToID("_KarMiktari");
        private float currentSnowAmount;
        private Coroutine snowTransitionCoroutine;
        private bool winterPreviewStarted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            HesaplaCevrimdisiZaman();
            MevsimEfektleriniGuncelle();
        }

        private void Update()
        {
            kalanSaniye -= Time.deltaTime;

            UpdateWinterPreview();

            if (kalanSaniye <= 0)
            {
                MevsimeGec(SonrakiMevsimiBul(aktifMevsim));
            }
        }

        private void UpdateWinterPreview()
        {
            if (aktifMevsim == MevsimTuru.Kis) return;
            if (SonrakiMevsimiBul(aktifMevsim) != MevsimTuru.Kis) return;

            float transitionSeconds = transitionDurationMinutes * 60f;
            if (kalanSaniye > transitionSeconds) return;

            if (!winterPreviewStarted)
            {
                winterPreviewStarted = true;
                if (kisVFX != null)
                {
                    kisVFX.SetActive(true);
                    SnowStormController snowStorm = kisVFX.GetComponent<SnowStormController>();
                    if (snowStorm != null) snowStorm.SetIntensityScale(lightSnowIntensityDuringTransition);
                }
            }

            float progress = 1f - Mathf.Clamp01(kalanSaniye / transitionSeconds);
            currentSnowAmount = progress * lightSnowCapDuringTransition;
            ApplySnowAmount(currentSnowAmount);
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
            winterPreviewStarted = false;

            Debug.Log($"[Season] Mevsim Değişti: {yeniMevsim} geldi!");

            MevsimEfektleriniGuncelle();
            
            OnMevsimDegisti?.Invoke(yeniMevsim);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(yeniMevsim.ToString() + "Muzik"); // Örn: "KisMuzik" isimli sesi çalar
            }
        }

        private void MevsimEfektleriniGuncelle()
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
                case MevsimTuru.Kis:
                    if (kisVFX != null)
                    {
                        kisVFX.SetActive(true);
                        SnowStormController snowStorm = kisVFX.GetComponent<SnowStormController>();
                        if (snowStorm != null) snowStorm.SetIntensityScale(1f);
                    }
                    break;
            }

            float targetSnowAmount = aktifMevsim == MevsimTuru.Kis ? 1f : 0f;

            if (snowTransitionCoroutine != null)
            {
                StopCoroutine(snowTransitionCoroutine);
            }
            snowTransitionCoroutine = StartCoroutine(TransitionSnowAmount(targetSnowAmount));

            if (directionalLight != null)
            {
                if (lightColorTransitionCoroutine != null)
                {
                    StopCoroutine(lightColorTransitionCoroutine);
                }
                lightColorTransitionCoroutine = StartCoroutine(TransitionLight(GetLightColorForSeason(aktifMevsim), GetLightIntensityForSeason(aktifMevsim)));
            }
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

        private IEnumerator TransitionLight(Color targetColor, float targetIntensity)
        {
            Color startColor = directionalLight.color;
            float startIntensity = directionalLight.intensity;
            float elapsed = 0f;

            while (elapsed < lightColorTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / lightColorTransitionDuration);
                directionalLight.color = Color.Lerp(startColor, targetColor, progress);
                directionalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, progress);
                yield return null;
            }

            directionalLight.color = targetColor;
            directionalLight.intensity = targetIntensity;
        }

        private IEnumerator TransitionSnowAmount(float targetSnowAmount)
        {
            float startSnowAmount = currentSnowAmount;
            float elapsed = 0f;

            while (elapsed < snowTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / snowTransitionDuration);
                currentSnowAmount = Mathf.Lerp(startSnowAmount, targetSnowAmount, progress);
                ApplySnowAmount(currentSnowAmount);
                yield return null;
            }

            currentSnowAmount = targetSnowAmount;
            ApplySnowAmount(currentSnowAmount);
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
                terrainData.RefreshPrototypes();

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
    }
}
