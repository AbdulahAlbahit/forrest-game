using UnityEngine;
using ForrestGame.Core;
using System.Collections;
using ForrestGame.Managers;

namespace ForrestGame.Audio
{
    /// <summary>
    /// Efe'nin oynanış sistemini (EfeOynanisEventleri) dinleyerek,
    /// hiçbir oyun koduna müdahale etmeden Yasir'in AudioManager'ını çalıştıran sistem.
    /// </summary>
    public class AudioEventSubscriber : MonoBehaviour
    {
        private void OnEnable()
        {
            // Olayları Dinlemeye Başla (Radyo Alıcısını Aç)
            EfeOynanisEventleri.BaltaSallandi += OnBaltaSallandi;
            EfeOynanisEventleri.BaltaAgacaVurdu += OnBaltaAgacaVurdu;
            
            TreeManager.OnTreeFelledAction += OnAgacYikildi;
            
            EfeOynanisEventleri.OdunToplandi += OnOdunToplandi;
            EfeOynanisEventleri.SepetDoldu += OnSepetDoldu;
            
            EfeOynanisEventleri.KamyonYuklemeBasladi += OnKamyonYuklemeBasladi;
            EfeOynanisEventleri.OdunKamyonaYuklendi += OnOdunKamyonaYuklendi;
            EfeOynanisEventleri.KamyonYuklemeBitti += OnKamyonYuklemeBitti;
            
            EfeOynanisEventleri.HareketDurumuDegisti += OnHareketDurumuDegisti;

            
            TruckRouteController.OnTruckDepartedHome += EvdenAyrilis;
            TruckRouteController.OnTruckDeparture += DuraktanKalkis;
            TruckRouteController.OnTruckDepartedMarket += DuraktanKalkis;

            TruckRouteController.OnTruckArrival += DuragaVaris;
            TruckRouteController.OnMarketSale += SatisYapiliyor;
            TruckRouteController.OnTruckArrivedHome += EveVaris;

            // Atölye (Workshop) Üretim Eventleri
            WorkshopProductionManager.OnHammerStrike += UretimCekicSesi;
            WorkshopProductionManager.OnProductionSuccess += UretimBasariliSesi;
            WorkshopProductionManager.OnCraftingStartedGlobal += UretimBasladi;
            WorkshopProductionManager.OnCraftingFinishedGlobal += UretimBitti;

            // Market ve Pazar Satış Eventleri
            ShopManager.OnItemSold += DukkandaSatisYapildi;
            EfeMarketSalesController.OnSaleCompleted += PazardaSatisYapildi;
            EfeMarketSalesController.OnCargoReadyForMarket += KargoHazir;
            EfeMarketSalesController.OnBalanceChanged += PazarBakiyeDegisti;

            // Depo ve Transfer Eventleri
            CargoTransferZone.OnWoodTransferred += OdunTransferEdildi;

            // Genel Geliştirme Eventi
            UpgradeZoneController.OnUpgradeCompletedAction += GenelGelistirme;
        }

        private void OnDisable()
        {
            // Olayları Dinlemeyi Bırak (Hata çıkmasını önlemek için Alıcıyı Kapat)
            EfeOynanisEventleri.BaltaSallandi -= OnBaltaSallandi;
            EfeOynanisEventleri.BaltaAgacaVurdu -= OnBaltaAgacaVurdu;
            
            TreeManager.OnTreeFelledAction -= OnAgacYikildi;
            
            EfeOynanisEventleri.OdunToplandi -= OnOdunToplandi;
            EfeOynanisEventleri.SepetDoldu -= OnSepetDoldu;
            
            EfeOynanisEventleri.KamyonYuklemeBasladi -= OnKamyonYuklemeBasladi;
            EfeOynanisEventleri.OdunKamyonaYuklendi -= OnOdunKamyonaYuklendi;
            EfeOynanisEventleri.KamyonYuklemeBitti -= OnKamyonYuklemeBitti;
            
            EfeOynanisEventleri.HareketDurumuDegisti -= OnHareketDurumuDegisti;


            TruckRouteController.OnTruckDepartedHome -= EvdenAyrilis;
            TruckRouteController.OnTruckDeparture -= DuraktanKalkis;
            TruckRouteController.OnTruckDepartedMarket -= DuraktanKalkis;

            TruckRouteController.OnTruckArrival -= DuragaVaris;
            TruckRouteController.OnMarketSale -= SatisYapiliyor;
            TruckRouteController.OnTruckArrivedHome -= EveVaris;

            WorkshopProductionManager.OnHammerStrike -= UretimCekicSesi;
            WorkshopProductionManager.OnProductionSuccess -= UretimBasariliSesi;
            WorkshopProductionManager.OnCraftingStartedGlobal -= UretimBasladi;
            WorkshopProductionManager.OnCraftingFinishedGlobal -= UretimBitti;

            ShopManager.OnItemSold -= DukkandaSatisYapildi;
            EfeMarketSalesController.OnSaleCompleted -= PazardaSatisYapildi;
            EfeMarketSalesController.OnCargoReadyForMarket -= KargoHazir;
            EfeMarketSalesController.OnBalanceChanged -= PazarBakiyeDegisti;

            CargoTransferZone.OnWoodTransferred -= OdunTransferEdildi;
            UpgradeZoneController.OnUpgradeCompletedAction -= GenelGelistirme;
        }

        private void Start()
        {
            // Instance (Singleton) objelerin eventlerine Start'ta abone oluyoruz ki 
            // diğer scriptlerin Awake'te yüklenmesini beklemiş olalım.
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged += ParaDegisti;
            }

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnItemUpgraded += ElemanGelistirildi;
            }

            if (XPManager.Instance != null)
            {
                XPManager.Instance.OnLevelUp += LevelAtladi;
                XPManager.Instance.OnXPChanged += XPKazandi;
            }

            if (RegionManager.Instance != null)
            {
                RegionManager.Instance.OnRegionUnlocked += BolgeAcildi;
            }
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged -= ParaDegisti;
            }

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnItemUpgraded -= ElemanGelistirildi;
            }

            if (XPManager.Instance != null)
            {
                XPManager.Instance.OnLevelUp -= LevelAtladi;
                XPManager.Instance.OnXPChanged -= XPKazandi;
            }

            if (RegionManager.Instance != null)
            {
                RegionManager.Instance.OnRegionUnlocked -= BolgeAcildi;
            }
        }

        #region Ses Çalma Metotları

        private void OnBaltaSallandi(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("BaltaSallama");
        }

        private void OnBaltaAgacaVurdu(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("BaltaVurma");
        }

        private void OnAgacYikildi(Vector3 agacPozisyonu)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("AgacDusme");
        }

        private void OnOdunToplandi(Transform kaynak, int mevcut, int kapasite)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("OdunToplama");
        }

        private void OnSepetDoldu(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("SepetDoldu");
        }

        private void OnKamyonYuklemeBasladi(Transform kaynak)
        {
            // İsteğe bağlı: Kamyona yükleme başladığı an bir ses çıkabilir
            // if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("KamyonKapagiAcildi");
        }

        private void OnOdunKamyonaYuklendi(Transform kaynak, int mevcut, int kapasite)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("KamyonaOdunAtma");
        }

        private void OnKamyonYuklemeBitti(Transform kaynak)
        {
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX("KamyonDoldu");
        }

        private Coroutine hareketSesCoroutine;

        private void OnHareketDurumuDegisti(Transform kaynak, EfeHareketDurumu durum)
        {
            if (AudioManager.Instance == null) return;

            // Mevcut hareket sesini durdur (yeni hıza geçiş yaparken veya tamamen dururken)
            if (hareketSesCoroutine != null)
            {
                StopCoroutine(hareketSesCoroutine);
                hareketSesCoroutine = null;
            }

            if (durum == EfeHareketDurumu.Yuruyor)
            {
                hareketSesCoroutine = StartCoroutine(HareketSesiCal("YurumeSesi"));
            }
            else if (durum == EfeHareketDurumu.Kosuyor)
            {
                hareketSesCoroutine = StartCoroutine(HareketSesiCal("KosmaSesi"));
            }
        }

        private IEnumerator HareketSesiCal(string sesAdi)
        {
            Sound s = System.Array.Find(AudioManager.Instance.sfxSounds, x => x.name == sesAdi);
            
            if (s == null || s.clip == null)
            {
                Debug.LogWarning($"[AudioEventSubscriber] {sesAdi} bulunamadı veya AudioClip atanmamış!");
                yield break;
            }

            // Mükemmel döngü: Dosyanın kendi saniye uzunluğunu alıyoruz
            float beklemeSuresi = s.clip.length;

            while (true)
            {
                AudioManager.Instance.PlaySFX(sesAdi);
                yield return new WaitForSeconds(beklemeSuresi);
            }
        }

        #endregion

        #region Loop (Döngü) Müzik ve Motor Sesleri
        
        // Sesin türü ne olursa olsun (SFX, Loop Müzik, Motor Sesi) buradan çağırabiliriz.
        // AudioManager zaten PlayMusic ve PlayVehicle fonksiyonlarında "loop=true" yapıyor.


        private void EvdenAyrilis()
        {
            if (AudioManager.Instance != null) 
            {
                AudioManager.Instance.PlaySFX("ArabaCalistirma");
                StartCoroutine(YurumeSesiGecikmeli());
            }
        }

        private IEnumerator YurumeSesiGecikmeli()
        {
            float beklemeSuresi = 1.0f; // Varsayılan bekleme süresi
            
            if (AudioManager.Instance != null)
            {
                // Ses listesinden "ArabaCalistirma" sesini bulup, tam uzunluğunu saniye cinsinden alıyoruz
                Sound s = System.Array.Find(AudioManager.Instance.sfxSounds, x => x.name == "ArabaCalistirma");
                if (s != null && s.clip != null)
                {
                    beklemeSuresi = s.clip.length; // Sesin tam uzunluğu kadar bekle
                }
            }

            // Marş sesinin çalmasını bekliyoruz
            yield return new WaitForSeconds(beklemeSuresi);

            // Bekleme bitince yürüme (motor döngüsü) sesini başlatıyoruz
            if (AudioManager.Instance != null) 
            {
                AudioManager.Instance.PlayVehicle("ArabaYurume");
            }
        }

        private void DuraktanKalkis()
        {
            if (AudioManager.Instance != null) 
            {
                AudioManager.Instance.PlayVehicle("ArabaYurume");
            }
        }

        private void DuragaVaris()
        {
            if (AudioManager.Instance != null) 
            {
                AudioManager.Instance.PlaySFX("ArabaFren");
                AudioManager.Instance.PlayVehicle("ArabaRolanti");
            }
        }

        private void SatisYapiliyor()
        {
            // Satis aninda tekrar fren sesi calinmasin diye sadece rolanti tutuyoruz
            if (AudioManager.Instance != null) 
            {
                AudioManager.Instance.PlayVehicle("ArabaRolanti");
            }
        }

        private void EveVaris()
        {
            if (AudioManager.Instance != null) 
            {
                AudioManager.Instance.PlaySFX("ArabaFren");
                AudioManager.Instance.StopVehicle();
            }
        }

        #endregion

        #region Atölye (Workshop) Üretim Sesleri

        private void UretimCekicSesi()
        {
            // İşçi çekiç vurdukça çalacak olan kısa efekt
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("CekicVurma");
        }

        private void UretimBasariliSesi()
        {
            // Kutu paketlendiğinde veya üretim başarıyla bittiğinde çalacak onay efekti
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UretimTamamlandi");
        }

        private void UretimBasladi()
        {
            // Üretim boyunca arka planda çalacak döngüsel makine/testere sesi
            // (Sürekli bir ses olduğu için Vehicle listesinden veya Ambience listesinden çalabiliriz. 
            // AudioManager'da PlayVehicle'ı kullanmak motor sesiyle çakışabilir, bu yüzden istersen SFX kullanabilirsin ama loop olmaz, 
            // ya da Ambience kullanabiliriz. Biz şimdilik Ambience (arka plan) üzerinden verelim ki araba motoruyla çakışmasın)
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAmbience("AtolyeMakine");
        }

        private void UretimBitti()
        {
            // Üretim bitince arka plandaki makine/testere sesini sustur
            if (AudioManager.Instance != null) AudioManager.Instance.StopAmbience();
        }

        #endregion

        #region Arayüz (UI) ve Ekonomi Sesleri

        private void ElemanGelistirildi(string itemId, int yeniSeviye)
        {
            // Oyuncu balta, sepet veya arabayı geliştirdiğinde çalacak ses
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("GelistirmeSesi");
        }

        private void ParaDegisti(int yeniMiktar)
        {
            // Para miktarı her değiştiğinde (arttığında veya azaldığında) bu ses çalar
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("GenelBakiyeSesi");
        }

        private void PazarBakiyeDegisti(int bakiye)
        {
            // Pazarda satış yapılıp bakiye arttığında (veya UI güncellendiğinde) çalar
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("YazarKasaSesi");
        }

        private void LevelAtladi(int yeniSeviye)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("LevelAtlamaSesi");
        }

        private void XPKazandi(int yeniXP)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("XPKazanmaSesi");
        }

        private void BolgeAcildi(string bolgeAdi)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("BolgeAcilmaSesi");
        }

        private void DukkandaSatisYapildi(string itemId, int miktar)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("DukkanSatisSesi");
        }

        private void PazardaSatisYapildi(EfeMarketSalesController.SaleResult result)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("PazarSatisSesi");
        }

        private void KargoHazir(RecipeConfig config, int miktar)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("KargoHazirSesi");
        }

        private void OdunTransferEdildi()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("OdunTransferSesi");
        }

        private void GenelGelistirme()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("GelistirmeSesi");
        }

        #endregion
    }
}
