using SQLite;

public class FailedEmail
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Campo para el email que falló
    public string Email { get; set; }

    // Campo para almacenar el nombre del usuario
    public string UserName { get; set; }

    // Campo para almacenar el asunto del email
    public string Subject { get; set; }

    // Campo para almacenar el cuerpo del email
    public string Body { get; set; }

    // Estado del envío
    public string Status { get; set; }

    // Fecha y hora de creación
    public DateTime CreatedAt { get; set; }
}