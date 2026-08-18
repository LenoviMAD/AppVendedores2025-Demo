using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace AppVendedores2025.Localization
{
    /// <summary>
    /// Administra recursos .resx con actualización en tiempo real (INotifyPropertyChanged).
    /// Usar con Binding: Text="{Binding [BtnAceptar], Source={x:Static loc:LocalizationResourceManager.Instance}}"
    /// </summary>
    public sealed class LocalizationResourceManager : INotifyPropertyChanged
    {
        // Singleton
        public static LocalizationResourceManager Instance { get; } = new();

        private LocalizationResourceManager() { }

        private ResourceManager? _resourceManager;
        public void SetResourceManager(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Indexer: permite binding tipo [Clave]
        /// </summary>
        public string this[string text]
        {
            get
            {
                if (_resourceManager == null) return text;
                try
                {
                    var value = _resourceManager.GetString(text, CultureInfo.CurrentUICulture);
                    return string.IsNullOrEmpty(value) ? text : value!;
                }
                catch
                {
                    return text; // fallback si algo falló
                }
            }
        }

        /// <summary>
        /// Llamar cuando cambie la cultura (dispara actualización en toda la UI que bindea).
        /// </summary>
        public void NotifyCultureChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(indexerName));
        }

        // Nombre mágico para indexer en bindings
        private const string indexerName = "Item[]";
    }
}
