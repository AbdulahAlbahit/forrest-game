using System.Collections.Generic;

/// <summary>
/// Unity'nin JsonUtility sistemi Dictionary'yi serialize edemez.
/// Bu sinif iki paralel liste kullanarak ayni islevi saglar.
/// Firebase Firestore ve PlayerPrefs (JSON) ile tam uyumludur.
/// </summary>
[System.Serializable]
public class SerializableDict
{
    public List<string> keys = new List<string>();
    public List<int> values = new List<int>();

    /// <summary>
    /// Verilen anahtara karsilik gelen degeri dondurur.
    /// Anahtar bulunamazsa defaultValue dondurur.
    /// </summary>
    public int Get(string key, int defaultValue = 1)
    {
        if (string.IsNullOrEmpty(key)) return defaultValue;

        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == key)
            {
                return values[i];
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Verilen anahtar icin degeri ayarlar.
    /// Anahtar zaten varsa gunceller, yoksa yeni ekler.
    /// </summary>
    public void Set(string key, int value)
    {
        if (string.IsNullOrEmpty(key)) return;

        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == key)
            {
                values[i] = value;
                return;
            }
        }

        // Anahtar bulunamadi, yeni ekle
        keys.Add(key);
        values.Add(value);
    }

    /// <summary>
    /// Verilen anahtarin mevcut olup olmadigini kontrol eder.
    /// </summary>
    public bool ContainsKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == key)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Tum anahtar-deger ciftlerini bir Dictionary olarak dondurur.
    /// Firebase'e yazarken kullanilir.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();
        for (int i = 0; i < keys.Count; i++)
        {
            dict[keys[i]] = values[i];
        }
        return dict;
    }

    /// <summary>
    /// Bir Dictionary'den verileri iceri aktarir.
    /// Firebase'den okurken kullanilir.
    /// </summary>
    public void FromDictionary(Dictionary<string, object> dict)
    {
        keys.Clear();
        values.Clear();

        if (dict == null) return;

        foreach (var kvp in dict)
        {
            keys.Add(kvp.Key);
            values.Add(System.Convert.ToInt32(kvp.Value));
        }
    }

    /// <summary>
    /// Tum verileri temizler.
    /// </summary>
    public void Clear()
    {
        keys.Clear();
        values.Clear();
    }
}
