// Popola select da dati API
function populateSelect(selectId, data, valueField = 'id', labelFields = ['nome'], placeholder = 'Seleziona...') {
  // Variabile select: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const select = document.getElementById(selectId);
  if (!select) return;
  
  // Mantieni opzione placeholder
  select.innerHTML = `<option value="">${placeholder}</option>`;
  
  data.forEach(item => {
    // Variabile label: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const label = labelFields.map(field => item[field]).join(' ');
    select.innerHTML += `<option value="${item[valueField]}">${label}</option>`;
  });
}

// Prepara form per creazione
function setupCreateForm(modalId, formId, fields) {
  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById(formId);
  form.reset();
  form.dataset.editId = '';
}

// Prepara form per modifica
function setupEditForm(modalId, formId, data, fields) {
  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById(formId);
  form.dataset.editId = data.id;
  
  fields.forEach(field => {
    // Variabile input: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const input = form.querySelector(`[name="${field}"]`);
    if (input) {
      input.value = data[field] ?? '';
    }
  });
}

// Serializza form in oggetto
function serializeForm(formId) {
  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById(formId);
  // Variabile formData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const formData = new FormData(form);
  // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const data = {};
  
  for (let [key, value] of formData.entries()) {
    data[key] = value;
  }
  
  return data;
}

// Setup submit handler
function setupFormSubmit(formId, apiCreate, apiUpdate, onSuccess) {
  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById(formId);
  
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    if (!form.checkValidity()) {
      form.classList.add('was-validated');
      return;
    }
    
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = serializeForm(formId);
    // Variabile editId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const editId = form.dataset.editId;
    
    try {
      if (editId) {
        await apiUpdate(editId, data);
        showToast('Elemento aggiornato con successo');
      } else {
        await apiCreate(data);
        showToast('Elemento creato con successo');
      }
      
      // Chiudi modal (Tailwind compatible)
      const modalElement = document.getElementById(formId.replace('-form', '-modal'));
      modalElement.classList.add('hidden');
      
      onSuccess();
    } catch (error) {
      handleApiError(error);
    }
  });
}
