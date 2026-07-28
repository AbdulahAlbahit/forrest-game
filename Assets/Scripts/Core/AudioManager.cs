using System;
using UnityEngine;

namespace ForrestGame.Core
{
    // Sesleri Unity Inspector'da (Arayüzde) liste halinde görebilmemiz için özel bir sınıf
    [System.Serializable]
    public class Sound
    {
        public string name; // Sesi çağıracağımız isim (Örn: "AxeHit")
        public AudioClip clip; // Ses dosyasının kendisi
        [Range(0f, 1f)] public float volume = 1f; // Ses seviyesi
        [Range(0.1f, 3f)] public float pitch = 1f; // Ses hızı/inceliği (Farklı vuruş hissi vermek için)
    }

    public class AudioManager : MonoBehaviour
    {
        // Singleton Pattern: Bu sayede Efe herhangi bir script'ten AudioManager.Instance yazarak ulaşabilir.
        public static AudioManager Instance;

        [Header("Audio Sources (Hoparlörler)")]
        [Tooltip("Arka plan müziklerini çalan kaynak")]
        public AudioSource musicSource;
        [Tooltip("Balta, buton gibi kısa ses efektlerini çalan kaynak")]
        public AudioSource sfxSource;
        [Tooltip("Yağmur, rüzgar gibi sürekli doğa seslerini çalan kaynak")]
        public AudioSource ambienceSource;
        [Tooltip("Kamyon, motor gibi sürekli araç seslerini çalan kaynak")]
        public AudioSource vehicleSource;

        [Header("Sound Arrays (Ses Listeleri)")]
        public Sound[] musicSounds;
        public Sound[] sfxSounds;
        public Sound[] ambienceSounds;
        public Sound[] vehicleSounds;

        private void Awake()
        {
            // Singleton Kurulumu: Oyunda sadece 1 tane AudioManager olmasını sağlar
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Sahneler arası (Menüden Oyuna) geçişte müziğin kesilmemesini sağlar
            }
            else
            {
                Destroy(gameObject); // Eğer yanlışlıkla sahneye 2. bir AudioManager atılırsa onu yok eder
            }
        }

        /// <summary>
        /// Arka plan müziği çalmak için kullanılır.
        /// Kullanımı: AudioManager.Instance.PlayMusic("MenuMusic");
        /// </summary>
        public void PlayMusic(string name)
        {
            Sound s = Array.Find(musicSounds, x => x.name == name);
            if (s == null)
            {
                Debug.LogWarning("Müzik Bulunamadı: " + name);
                return;
            }

            musicSource.clip = s.clip;
            musicSource.volume = s.volume;
            musicSource.pitch = s.pitch;
            musicSource.loop = true; // Müzikler bitince tekrar baştan çalar
            musicSource.Play();
        }

        /// <summary>
        /// Kısa ses efektlerini çalmak için kullanılır. (Aynı anda birden fazla çalabilir)
        /// Kullanımı: AudioManager.Instance.PlaySFX("AxeHit");
        /// </summary>
        public void PlaySFX(string name)
        {
            Sound s = Array.Find(sfxSounds, x => x.name == name);
            if (s == null)
            {
                Debug.LogWarning("SFX Bulunamadı: " + name);
                return;
            }

            // PlayOneShot kullanıyoruz çünkü 10 ağaç aynı anda kesilirse seslerin birbirini kesmemesi lazım
            sfxSource.PlayOneShot(s.clip, s.volume); 
        }

        /// <summary>
        /// Arka plan doğa/hava durumu (Ambiyans) sesini çalmak için kullanılır.
        /// </summary>
        public void PlayAmbience(string name)
        {
            Sound s = Array.Find(ambienceSounds, x => x.name == name);
            if (s == null)
            {
                Debug.LogWarning("Ambiyans Bulunamadı: " + name);
                return;
            }

            ambienceSource.clip = s.clip;
            ambienceSource.volume = s.volume;
            ambienceSource.pitch = s.pitch;
            ambienceSource.loop = true;
            ambienceSource.Play();
        }

        private Coroutine vehicleRoutine;

        /// <summary>
        /// Araç motoru gibi sürekli sesleri çalmak için kullanılır.
        /// </summary>
        public void PlayVehicle(string name)
        {
            Sound s = Array.Find(vehicleSounds, x => x.name == name);
            if (s == null)
            {
                Debug.LogWarning("Araç Sesi Bulunamadı: " + name);
                return;
            }

            if (vehicleRoutine != null)
            {
                StopCoroutine(vehicleRoutine);
            }

            vehicleRoutine = StartCoroutine(VehicleLoopRoutine(s));
        }

        private System.Collections.IEnumerator VehicleLoopRoutine(Sound s)
        {
            if (s.clip == null) yield break;

            vehicleSource.clip = s.clip;
            vehicleSource.volume = s.volume;
            vehicleSource.pitch = s.pitch;
            vehicleSource.loop = false; // Unity'nin kendi loop özelliğini kapatıp manuel döngü yapıyoruz

            float beklemeSuresi = s.clip.length;

            while (true)
            {
                vehicleSource.Play();
                yield return new WaitForSeconds(beklemeSuresi);
            }
        }

        /// <summary>
        /// Çalan müziği durdurur.
        /// </summary>
        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void StopAmbience()
        {
            if(ambienceSource != null) ambienceSource.Stop();
        }

        public void StopVehicle()
        {
            if (vehicleRoutine != null)
            {
                StopCoroutine(vehicleRoutine);
                vehicleRoutine = null;
            }
            if (vehicleSource != null) vehicleSource.Stop();
        }
    }
}
