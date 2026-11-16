namespace AsignacionMaestros.Core;

/// <summary>
/// Núcleo del algoritmo. Trabaja siempre en memoria y NO modifica lo que recibe:
/// devuelve el estado final y la lista de cambios para que la UI los muestre
/// (vista previa) y el repositorio guarde solo la diferencia.
/// </summary>
public static class Asignador
{
    public static ResultadoAsignacion Asignar(
        IReadOnlyList<Slot> original, IReadOnlyList<Maestro> maestros, ModoAsignacion modo)
    {
        var slots = original.Select(s => s.Clonar()).ToList();
        var anterior = original.ToDictionary(s => s.Id, s => s.IdMaestro);
        if (modo == ModoAsignacion.Reoptimizar)
            foreach (var s in slots.Where(s => s.Estado == EstadoSlot.Asignado))
            {
                s.IdMaestro = null;
                s.Estado = EstadoSlot.Pendiente;
            }
        else
        {
            foreach (var s in slots.Where(s => s.Estado == EstadoSlot.Asignado))
            {
                s.IdMaestro = null;
                s.Estado = EstadoSlot.Pendiente;
            }
        }

        // Lo que ya tiene maestro (Fijo, y Asignado en modo conservador) ocupa esa hora.
        var ocupados = slots.Where(s => s.IdMaestro.HasValue)
                            .Select(s => (s.IdMaestro!.Value, s.Bloque)).ToHashSet();
        var carga = maestros.ToDictionary(m => m.Id, m => slots.Count(s => s.IdMaestro == m.Id));
        var pendientes = slots.Where(s => s.Estado == EstadoSlot.Pendiente).ToList();

        // Paso 2: candidatos = ofrecen esa materia a esa hora y no están ocupados a esa hora.
        var candidatos = pendientes.ToDictionary(
            s => s.Id,
            s => maestros.Where(m => m.Ofrecer(s.IdMateria, s.Bloque) && !ocupados.Contains((m.Id, s.Bloque))).ToList());

        // Flexibilidad: a cuántos slots pendientes puede servir cada maestro (menos = más limitado).
        var flexibilidad = maestros.ToDictionary(m => m.Id, m => candidatos.Values.Count(l => l.Contains(m)));

        // Paso 3: una hora a la vez, con caminos aumentantes (Algoritmo de Kuhn).
        foreach (var grupo in pendientes.GroupBy(s => s.Bloque).OrderBy(g => g.Key))
        {
            var dueno = new Dictionary<int, Slot>();   // maestro -> slot que tiene en esta hora

            List<Maestro> Orden(Slot s) => candidatos[s.Id]
                .OrderBy(m => anterior[s.Id] == m.Id ? 0 : 1)       // 1) conservar al maestro actual
                .ThenBy(m => flexibilidad[m.Id])                      // 2) el más limitado primero
                .ThenBy(m => carga[m.Id])                             // 3) el de menos carga
                .ThenBy(m => m.Clave, StringComparer.Ordinal)         // 4) desempate fijo
                .ToList();

            bool Intentar(Slot s, HashSet<int> vistos)
            {
                foreach (var m in Orden(s))
                {
                    if (!vistos.Add(m.Id)) continue;
                    if (!dueno.TryGetValue(m.Id, out var otro) || Intentar(otro, vistos))
                    {
                        dueno[m.Id] = s;
                        return true;
                    }
                }
                return false;
            }

            foreach (var s in grupo.OrderBy(s => candidatos[s.Id].Count).ThenBy(s => s.Id))
                Intentar(s, new HashSet<int>());

            foreach (var (idMaestro, s) in dueno)
            {
                s.IdMaestro = idMaestro;
                s.Estado = EstadoSlot.Asignado;
                s.Motivo = null;
                carga[idMaestro]++;
            }
        }

        // Paso 4: motivo de cada pendiente, calculado sobre el estado final
        // (así el mensaje es el mismo sin importar el modo con el que se corrió).
        var tiene = slots.Where(s => s.IdMaestro.HasValue).ToDictionary(s => (s.IdMaestro!.Value, s.Bloque));
        foreach (var s in pendientes.Where(s => s.Estado == EstadoSlot.Pendiente))
            s.Motivo = MotivoPendiente(s, maestros, tiene);

        return ArmarResultado(original, slots);
    }

    /// <summary>Paso 5: reglas que nunca deben romperse. Lista vacía = todo bien.</summary>
    public static List<string> Validar(IReadOnlyList<Slot> slots, IReadOnlyList<Maestro> maestros)
    {
        var errores = new List<string>();
        var porId = maestros.ToDictionary(m => m.Id);

        foreach (var g in slots.Where(s => s.IdMaestro.HasValue)
                               .GroupBy(s => (s.IdMaestro!.Value, s.Bloque))
                               .Where(g => g.Count() > 1))
            errores.Add($"{porId[g.Key.Item1].Clave} está en {g.Count()} slots a la misma hora (bloque {g.Key.Item2}).");

        foreach (var s in slots.Where(s => s.IdMaestro.HasValue))
            if (!porId.TryGetValue(s.IdMaestro!.Value, out var m) || !m.Ofrecer(s.IdMateria, s.Bloque))
                errores.Add($"{s.Etiqueta}: el maestro asignado no ofrece esa materia a esa hora.");

        foreach (var s in slots)
            if ((s.Estado == EstadoSlot.Pendiente) != (s.IdMaestro == null))
                errores.Add($"{s.Etiqueta}: estado {s.Estado} incoherente con el maestro asignado.");

        return errores;
    }

    /// <summary>
    /// Flujo "el maestro cambia lo que ofrece": libera los slots que ya no puede dar
    /// (aunque sean Fijo) y reasigna en modo conservador. Recibe la lista de maestros
    /// ya con la oferta nueva.
    /// </summary>
    public static ResultadoAsignacion ReasignarPorCambioDeOferta(
        IReadOnlyList<Slot> original, IReadOnlyList<Maestro> maestrosConOfertaNueva, int idMaestro)
    {
        var nuevo = maestrosConOfertaNueva.First(m => m.Id == idMaestro);
        var trabajo = original.Select(s =>
        {
            var c = s.Clonar();
            if (c.IdMaestro == idMaestro && !nuevo.Ofrecer(c.IdMateria, c.Bloque))
            {
                c.IdMaestro = null;
                c.Estado = EstadoSlot.Pendiente;
                c.Motivo = "El maestro quitó su disponibilidad.";
            }
            return c;
        }).ToList();

        var res = Asignar(trabajo, maestrosConOfertaNueva, ModoAsignacion.Conservador);
        return ArmarResultado(original, res.Slots.ToList());
    }

    /// <summary>Slots que se quedarían sin maestro si este maestro cambia su oferta (para avisar antes de confirmar).</summary>
    public static List<Slot> SlotsAfectados(
        IReadOnlyList<Slot> slots, int idMaestro, HashSet<(int IdMateria, int Bloque)> nuevaOferta) =>
        slots.Where(s => s.IdMaestro == idMaestro && !nuevaOferta.Contains((s.IdMateria, s.Bloque))).ToList();

    internal static ResultadoAsignacion ArmarResultado(IReadOnlyList<Slot> original, List<Slot> final)
    {
        var cambios = new List<CambioPropuesto>();
        foreach (var (a, d) in original.Zip(final))
            if (a.IdMaestro != d.IdMaestro || a.Estado != d.Estado || a.Motivo != d.Motivo)
                cambios.Add(new CambioPropuesto(a.Id, a.IdMaestro, d.IdMaestro, a.Estado, d.Estado, d.Motivo));
        return new ResultadoAsignacion { Slots = final, Cambios = cambios };
    }

    private const int MaxMotivo = 200;   // = VARCHAR(200) de PaqueteMateria.MotivoPendiente

    private static string Recortar(string texto) =>
        texto.Length <= MaxMotivo ? texto : texto[..(MaxMotivo - 1)] + "…";

    private static string MotivoPendiente(
        Slot s, IReadOnlyList<Maestro> maestros, Dictionary<(int, int), Slot> tiene)
    {
        var hora = s.Hora == "" ? $"el bloque {s.Bloque}" : s.Hora;
        var ofrecen = maestros.Where(m => m.Ofrecer(s.IdMateria, s.Bloque)).ToList();
        if (ofrecen.Count == 0)
            return Recortar($"Ningún maestro ofrece la materia {s.Materia} en {hora}.");

        var detalle = ofrecen.Select(m => tiene.TryGetValue((m.Id, s.Bloque), out var otro)
            ? $"{m.Clave} (en {otro.Etiqueta})" : $"{m.Clave} (libre)");
        return Recortar($"Quienes ofrecen {s.Materia} en {hora} están ocupados: {string.Join(", ", detalle)}.");
    }
}
