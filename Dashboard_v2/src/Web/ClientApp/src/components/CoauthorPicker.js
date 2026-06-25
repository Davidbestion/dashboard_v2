import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Button, Input, InputGroup } from 'reactstrap';
import SelectionProfileCard from './SelectionProfileCard';

/**
 * Selector reutilizable de coautores basado en tarjetas.
 * Soporta autores existentes, usuarios del sistema y autores libres escritos por texto.
 */
export default function CoauthorPicker({
  value,
  onChange,
  placeholder = 'Buscar autor o escribir: Apellidos, Nombres',
  helpText,
}) {
  const [query, setQuery] = useState('');
  const [suggestions, setSuggestions] = useState([]);
  const [suggestionsOpen, setSuggestionsOpen] = useState(false);
  const debounceRef = useRef(null);

  /**
   * Limpia el temporizador pendiente cuando el selector se desmonta para evitar
   * que una búsqueda tardía intente actualizar estado sobre un componente inexistente.
   */
  useEffect(() => () => clearTimeout(debounceRef.current), []);

  /**
   * Comprueba si un candidato ya fue agregado al selector.
   * La deduplicación prioriza el identificador del backend y, como respaldo,
   * el nombre normalizado para autores libres.
   */
  const isAlreadySelected = useMemo(() => {
    return (candidate) => value.some((item) =>
      (candidate.id && item.id && candidate.id === item.id && candidate.type === item.type) ||
      item.name.trim().toLowerCase() === candidate.name.trim().toLowerCase()
    );
  }, [value]);

  /**
   * Agrega un registro al conjunto seleccionado evitando duplicados lógicos.
   */
  const addEntry = (entry) => {
    if (!entry?.name?.trim()) return;
    if (isAlreadySelected(entry)) {
      setQuery('');
      setSuggestions([]);
      setSuggestionsOpen(false);
      return;
    }

    onChange([...value, { ...entry, name: entry.name.trim() }]);
    setQuery('');
    setSuggestions([]);
    setSuggestionsOpen(false);
  };

  /**
   * Elimina una tarjeta previamente seleccionada.
   */
  const removeEntry = (entry) => {
    onChange(value.filter((item) => !(
      (entry.id && item.id && item.id === entry.id && item.type === entry.type) ||
      (!entry.id && item.name.trim().toLowerCase() === entry.name.trim().toLowerCase())
    )));
  };

  /**
   * Handles input changes with a 250ms debounce to avoid excessive API calls.
   * Queries the remote co-author search endpoint when input length >= 2.
   * Clears suggestions if the query is too short or the component is not in "search" mode.
   */
  const handleInputChange = (event) => {
    const nextQuery = event.target.value;
    setQuery(nextQuery);
    clearTimeout(debounceRef.current);

    if (nextQuery.trim().length < 2) {
      setSuggestions([]);
      setSuggestionsOpen(false);
      return;
    }

    debounceRef.current = setTimeout(async () => {
      try {
        const response = await fetch(`/api/Authors/search-coauthors?q=${encodeURIComponent(nextQuery.trim())}`, {
          credentials: 'include',
        });

        if (!response.ok) {
          setSuggestions([]);
          setSuggestionsOpen(false);
          return;
        }

        const remoteSuggestions = await response.json();
        const availableSuggestions = remoteSuggestions.filter((candidate) => !isAlreadySelected(candidate));
        setSuggestions(availableSuggestions);
        setSuggestionsOpen(availableSuggestions.length > 0);
      } catch {
        setSuggestions([]);
        setSuggestionsOpen(false);
      }
    }, 250);
  };

  /**
   * Convierte el texto escrito en un autor libre cuando el usuario pulsa Enter o el botón de agregar.
   */
  const addFreeTextEntry = () => {
    const name = query.trim();
    if (!name) return;
    addEntry({ id: null, name, type: 'new', linkedUser: null });
  };

  return (
    <div>
      {value.length > 0 && (
        <div className="mb-3">
          <div className="small text-muted mb-2">Seleccionados</div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))', gap: '0.75rem' }}>
            {value.map((entry) => (
              <SelectionProfileCard
                key={`${entry.type}-${entry.id ?? entry.name}`}
                person={entry}
                variant="selected"
                onClick={removeEntry}
                clickTitle="Clic para quitar"
              />
            ))}
          </div>
        </div>
      )}

      <div className="position-relative">
        <InputGroup>
          <Input
            value={query}
            onChange={handleInputChange}
            onKeyDown={(event) => {
              if (event.key === 'Enter' || event.key === ',') {
                event.preventDefault();
                addFreeTextEntry();
              }

              if (event.key === 'Escape') {
                setSuggestionsOpen(false);
              }
            }}
            onBlur={() => setTimeout(() => setSuggestionsOpen(false), 150)}
            placeholder={placeholder}
            autoComplete="off"
          />
          <Button type="button" color="secondary" outline onClick={addFreeTextEntry}>
            <i className="bi bi-plus" />
          </Button>
        </InputGroup>

        {suggestionsOpen && (
          <div
            className="mt-3"
            style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))', gap: '0.75rem' }}
          >
            {suggestions.map((candidate) => (
              <SelectionProfileCard
                key={`${candidate.type}-${candidate.id}`}
                person={candidate}
                variant="neutral"
                onClick={addEntry}
                clickTitle="Clic para agregar"
              />
            ))}
          </div>
        )}
      </div>

      <small className="text-muted d-block mt-1">
        Para autores nuevos escriba primero los <strong>apellidos</strong>, luego una coma y luego los <strong>nombres</strong>.{' '}
        <em>Ejemplo: García López, Juan Manuel</em>
      </small>

      {helpText && (
        <small className="text-muted d-block mt-2">
          {helpText}
        </small>
      )}
    </div>
  );
}
