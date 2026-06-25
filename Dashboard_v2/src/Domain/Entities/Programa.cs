namespace Dashboard_v2.Domain.Entities;

/// <summary>Academic or administrative program supported by PAP-type projects.</summary>
public class Programa
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
}
