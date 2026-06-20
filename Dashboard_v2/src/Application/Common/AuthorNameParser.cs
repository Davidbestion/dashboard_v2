namespace Dashboard_v2.Application.Common;

/// <summary>
/// Utilidad para parsear cadenas de texto con nombres de autores al formato
/// estructurado (apellidos / nombres de pila) usado en el ámbito académico.
///
/// Convención esperada: "Apellidos, Nombres"  — ej: "García López, Juan Manuel"
///
/// Si la cadena no contiene coma, todo el texto se trata como apellidos y el
/// nombre de pila queda nulo.  El sistema instruye al usuario a seguir la
/// convención antes de registrar el nombre, por lo que este caso representa
/// una entrada incompleta que se almacena de forma segura sin pérdida de datos.
/// </summary>
public static class AuthorNameParser
{
    /// <summary>
    /// Parsea un string de entrada en (apellidos, nombres).
    /// </summary>
    /// <param name="input">Cadena en formato "Apellidos, Nombres" o solo "Apellidos".</param>
    /// <returns>Tupla (LastName, FirstName?). FirstName es null si no se proporcionó.</returns>
    public static (string LastName, string? FirstName) Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (string.Empty, null);

        var commaIndex = input.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex < 0)
            return (input.Trim(), null);

        var lastName  = input[..commaIndex].Trim();
        var firstName = input[(commaIndex + 1)..].Trim();

        return (lastName, string.IsNullOrWhiteSpace(firstName) ? null : firstName);
    }
}
