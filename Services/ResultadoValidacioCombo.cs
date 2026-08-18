//using Android.OS;
//using static Android.Content.ClipData;

//using static Android.Content.ClipData;
//using static Android.Content.ClipData;

namespace AppVendedores2025.Services
{
    public class ResultadoValidacioCombo

    {
        public ResultadoValidacioCombo(bool resultado, string mensaje, decimal valor, int tipoDeValidacionFail)
        {
            Resultado = resultado;
            Mensaje = mensaje;
            Valor = valor;
            TipoDeValidacionFail = tipoDeValidacionFail;
        }

        public bool Resultado { get; set; }
        public string Mensaje { get; set; }
        public decimal Valor { get; set; }

        public int TipoDeValidacionFail { get; set; }
    }
}
