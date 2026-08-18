using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVendedores2025.Shared
{
    public class PuntosSinTxServidor
    {
        [Key]
        public int IDPuntosSinTxServidor { get; set; }

        public string NombreUsuario { get; set; }
        public string ClaveUsuario { get; set; }
        public double Latitud {  get; set; }
        public double Longitud { get; set; }
        public DateTime GPSTime { get; set; } = DateTime.Now;
        public string PhoneNumber { get; set; } = "PhoneNumber test xxx";
        public string SessionID { get; set; } = "v27-" + DeviceInfo.Current.Model;
        public string EventType { get; set; } = DeviceInfo.Manufacturer;

    }
}
