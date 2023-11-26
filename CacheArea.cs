using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxAlarms
{
    public class CacheArea
    {
        private readonly object verrou = new object();
        private Dictionary<int, DateTime> numerosEnCache = new Dictionary<int, DateTime>();
        private DateTime timestampDerniereMiseAJour;

        public void AjouterNumero(int numero)
        {
            lock (verrou)
            {
                if (!numerosEnCache.ContainsKey(numero))
                {
                    numerosEnCache[numero] = DateTime.Now;
                }
            }

            UpdateCache();
        }

        private void UpdateCache()
        {
            lock (verrou)
            {
                foreach (var item in numerosEnCache)
                {
                    if (item.Value >= timestampDerniereMiseAJour.AddMinutes(10))
                    {
                        numerosEnCache.Remove(item.Key);
                    }
                }

                timestampDerniereMiseAJour = DateTime.Now;
            }
        }

        public bool IsAreaInCache(int numero)
        {
            UpdateCache();
            lock (verrou)
            {
                return numerosEnCache.ContainsKey(numero); // Retourner une copie de la liste pour éviter les modifications externes
            }
        }
    }
}
