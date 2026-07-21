using UnityEngine;
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

            if (kalanSaniye <= 0)
            {
                MevsimeGec(SonrakiMevsimiBul(aktifMevsim));
            }
        }

        // Oyun kapalıyken geçen zamanı hesaplar (20 dakikada bir mevsim atlatır)
        private void HesaplaCevrimdisiZaman()
        {
            string sonGirisString = PlayerPrefs.GetString("SonCikisZamani", DateTime.Now.ToString());
            DateTime sonGiris = DateTime.Parse(sonGirisString);
            
            TimeSpan gecenZaman = DateTime.Now - sonGiris;
            double gecenSaniye = gecenZaman.TotalSeconds;

            aktifMevsim = (MevsimTuru)PlayerPrefs.GetInt("AktifMevsim", 0);
            kalanSaniye = PlayerPrefs.GetFloat("KalanMevsimSaniyesi", ayarlar.mevsimSuresiDakika * 60);

            kalanSaniye -= (float)gecenSaniye;

            // Offline iken birden fazla mevsim geçmiş olabilir
            while (kalanSaniye <= 0)
            {
                aktifMevsim = SonrakiMevsimiBul(aktifMevsim);
                kalanSaniye += ayarlar.mevsimSuresiDakika * 60; 
            }

            Debug.Log($"[Season] Oyuna dönüldü. Mevsim: {aktifMevsim}. Bitmesine {(kalanSaniye/60):F1} dakika var.");
        }

        // Oyuncu oyundan çıkarken zamanı kaydeder
        private void OnApplicationQuit()
        {
            PlayerPrefs.SetString("SonCikisZamani", DateTime.Now.ToString());
            PlayerPrefs.SetInt("AktifMevsim", (int)aktifMevsim);
            PlayerPrefs.SetFloat("KalanMevsimSaniyesi", kalanSaniye);
            PlayerPrefs.Save();
        }

        public void MevsimeGec(MevsimTuru yeniMevsim)
        {
            aktifMevsim = yeniMevsim;
            kalanSaniye = ayarlar.mevsimSuresiDakika * 60; // 20 dk tekrar başlar

            Debug.Log($"[Season] Mevsim Değişti: {yeniMevsim} geldi!");

            MevsimEfektleriniGuncelle();
            
            OnMevsimDegisti?.Invoke(yeniMevsim);
            
            // Yasir'in Ses Sistemine Haber Ver (Müzikleri Değiştirsin)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(yeniMevsim.ToString() + "Muzik"); // Örn: "KisMuzik" isimli sesi çalar
            }
        }

        // YASİR'İN GÖRSELLERİNİN ÇAKIŞMASINI ENGELLEYEN SİSTEM
        private void MevsimEfektleriniGuncelle()
        {
            // ÖNCE BÜTÜN MEVSİMLERİ KAPATIYORUZ (KAPKARANLIK OLUYOR)
            if (ilkbaharVFX != null) ilkbaharVFX.SetActive(false);
            if (yazVFX != null) yazVFX.SetActive(false);
            if (sonbaharVFX != null) sonbaharVFX.SetActive(false);
            if (kisVFX != null) kisVFX.SetActive(false);

            // SONRA SADECE ŞUANKİ MEVSİMİ AÇIYORUZ (ÇAKIŞMA İMKANSIZLAŞIYOR)
            switch (aktifMevsim)
            {
                case MevsimTuru.Ilkbahar: if (ilkbaharVFX != null) ilkbaharVFX.SetActive(true); break;
                case MevsimTuru.Yaz: if (yazVFX != null) yazVFX.SetActive(true); break;
                case MevsimTuru.Sonbahar: if (sonbaharVFX != null) sonbaharVFX.SetActive(true); break;
                case MevsimTuru.Kis: if (kisVFX != null) kisVFX.SetActive(true); break;
            }
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
