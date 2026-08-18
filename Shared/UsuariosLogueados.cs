using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppVendedores2025.Shared
{
    public class UsuariosLogueados
    {

        [Key]
        public int IDUsuariosLogueados { get; set; }
        public string NombreUsuarioLogueado { get; set; }
        public string ClaveUsuarioLogueado { get; set; }
        public double Latitude { get; set; } = 99.9999;
        public double Longitude { get; set; } = 99.9999;
        public DateTime FechaUsuarioLogueado { get; set; } = DateTime.Now;
        public string SessionID { get; set; } = "v27-" + DeviceInfo.Current.Model;
        public string EventType { get; set; } = DeviceInfo.Manufacturer;
        public string PhoneNumber { get; set; } = "v27-" + DeviceInfo.Current.Model + "-" + DeviceInfo.Manufacturer;


    }
}
