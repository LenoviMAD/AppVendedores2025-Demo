using System.ComponentModel.DataAnnotations;

public class SolicitudAltaModel
{
    [Required(ErrorMessage = "El nombre y apellido son obligatorios.")]
    [StringLength(100, ErrorMessage = "El nombre y apellido no pueden exceder los 100 caracteres.")]
    public string NombreApellido { get; set; }

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    public string Direccion { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "Ingrese un número de teléfono válido.")]
    public string Telefono { get; set; }

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un email válido.")]
    public string Email { get; set; }
}
