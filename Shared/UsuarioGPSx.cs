using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;


namespace AppVendedores2025.Shared
{
    public class UsuarioGPSx
    {
        [Key]
        public int IDUsuarioGPSx { get; set; }

        public string NombreUsuario { get; set; } = "SIN INICIO SESSION";

        public string ClaveUsuario { get; set; } = "SIN INICIO SESSION";

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime GPSTimeDispositivo { get; set; } = DateTime.Now;

        public string PhoneNumber { get; set; } = "PhoneNumber test xxx";
        public string SessionID { get; set; } = "v27-"+DeviceInfo.Current.Model;
        public string EventType { get; set; } = DeviceInfo.Manufacturer;
        public DateTime? GPSLastUpdateAPI { get; set; } = null;




    }
}
