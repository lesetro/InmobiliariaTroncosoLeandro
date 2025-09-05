
class AutocompleteSearch {
    constructor(options) {
        this.inputId = options.inputId;
        this.hiddenInputId = options.hiddenInputId;
        this.dropdownId = options.dropdownId;
        this.selectedId = options.selectedId;
        this.infoId = options.infoId;
        this.apiEndpoint = options.apiEndpoint;
        this.placeholder = options.placeholder || 'Escriba para buscar...';
        this.minLength = options.minLength || 2;
        this.debounceTime = options.debounceTime || 300;
        
        this.searchTimeout = null;
        this.currentSelection = -1;
        
        this.init();
    }
    
    init() {
        const searchInput = document.getElementById(this.inputId);
        const hiddenInput = document.getElementById(this.hiddenInputId);
        const dropdown = document.getElementById(this.dropdownId);
        const selectedDiv = document.getElementById(this.selectedId);
        
        if (!searchInput || !hiddenInput || !dropdown) {
            console.error('AutocompleteSearch: Elementos requeridos no encontrados');
            return;
        }
        
        this.bindEvents();
    }
    
    bindEvents() {
        const searchInput = document.getElementById(this.inputId);
        
        searchInput.addEventListener('input', (e) => this.handleInput(e));
        searchInput.addEventListener('keydown', (e) => this.handleKeydown(e));
        
        // Cerrar dropdown al hacer clic fuera
        document.addEventListener('click', (e) => {
            if (!e.target.closest(`#${this.inputId}`).parentElement) {
                this.hideDropdown();
            }
        });
    }
    
    handleInput(e) {
        const termino = e.target.value.trim();
        
        // Limpiar selección si se modifica el texto
        const hiddenInput = document.getElementById(this.hiddenInputId);
        if (hiddenInput.value) {
            this.clearSelection();
        }
        
        clearTimeout(this.searchTimeout);
        
        if (termino.length < this.minLength) {
            this.hideDropdown();
            return;
        }
        
        this.showLoading();
        
        this.searchTimeout = setTimeout(() => {
            this.performSearch(termino);
        }, this.debounceTime);
    }
    
    handleKeydown(e) {
        const dropdown = document.getElementById(this.dropdownId);
        const items = dropdown.querySelectorAll('.autocomplete-item');
        
        switch(e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.currentSelection = Math.min(this.currentSelection + 1, items.length - 1);
                this.updateSelection(items);
                break;
            case 'ArrowUp':
                e.preventDefault();
                this.currentSelection = Math.max(this.currentSelection - 1, -1);
                this.updateSelection(items);
                break;
            case 'Enter':
                e.preventDefault();
                if (this.currentSelection >= 0 && items[this.currentSelection]) {
                    items[this.currentSelection].click();
                }
                break;
            case 'Escape':
                this.hideDropdown();
                break;
        }
    }
    
    async performSearch(termino) {
        try {
            const response = await fetch(`${this.apiEndpoint}?termino=${encodeURIComponent(termino)}`);
            const data = await response.json();
            
            this.hideLoading();
            
            if (data.error) {
                this.showError(data.error);
                return;
            }
            
            this.showResults(data);
        } catch (error) {
            this.hideLoading();
            this.showError('Error de conexión');
        }
    }
    
    showResults(results) {
        const dropdown = document.getElementById(this.dropdownId);
        
        if (results.length === 0) {
            dropdown.innerHTML = '<div class="autocomplete-item text-muted">No se encontraron resultados</div>';
            this.showDropdown();
            return;
        }
        
        dropdown.innerHTML = results.map(item => `
            <div class="autocomplete-item" onclick="window.autocompleteInstances['${this.inputId}'].selectItem(${item.id}, '${item.texto}', '${item.telefono || ''}', '${item.email || ''}')">
                <div class="fw-bold">${item.texto}</div>
                ${item.telefono ? `<small class="text-muted">Tel: ${item.telefono}</small>` : ''}
                ${item.email ? `<small class="text-muted d-block">Email: ${item.email}</small>` : ''}
            </div>
        `).join('');
        
        this.showDropdown();
        this.currentSelection = -1;
    }
    
    selectItem(id, texto, telefono = '', email = '') {
        const searchInput = document.getElementById(this.inputId);
        const hiddenInput = document.getElementById(this.hiddenInputId);
        const selectedDiv = document.getElementById(this.selectedId);
        const infoDiv = document.getElementById(this.infoId);
        
        hiddenInput.value = id;
        searchInput.value = texto;
        
        if (selectedDiv && infoDiv) {
            infoDiv.innerHTML = `
                ${texto}
                ${telefono ? `<br><small class="text-muted">Tel: ${telefono}</small>` : ''}
                ${email ? `<br><small class="text-muted">Email: ${email}</small>` : ''}
            `;
            
            searchInput.style.display = 'none';
            selectedDiv.style.display = 'block';
        }
        
        this.hideDropdown();
        
        // Disparar evento personalizado
        searchInput.dispatchEvent(new CustomEvent('itemSelected', { 
            detail: { id, texto, telefono, email } 
        }));
    }
    
    clearSelection() {
        const searchInput = document.getElementById(this.inputId);
        const hiddenInput = document.getElementById(this.hiddenInputId);
        const selectedDiv = document.getElementById(this.selectedId);
        
        hiddenInput.value = '';
        searchInput.value = '';
        
        if (selectedDiv) {
            searchInput.style.display = 'block';
            selectedDiv.style.display = 'none';
        }
        
        searchInput.focus();
    }
    
    showLoading() {
        const searchInput = document.getElementById(this.inputId);
        searchInput.classList.add('loading');
    }
    
    hideLoading() {
        const searchInput = document.getElementById(this.inputId);
        searchInput.classList.remove('loading');
    }
    
    showDropdown() {
        const dropdown = document.getElementById(this.dropdownId);
        dropdown.style.display = 'block';
    }
    
    hideDropdown() {
        const dropdown = document.getElementById(this.dropdownId);
        dropdown.style.display = 'none';
        this.currentSelection = -1;
    }
    
    showError(message) {
        const dropdown = document.getElementById(this.dropdownId);
        dropdown.innerHTML = `<div class="autocomplete-item text-danger">Error: ${message}</div>`;
        this.showDropdown();
    }
    
    updateSelection(items) {
        items.forEach((item, index) => {
            item.classList.toggle('active', index === this.currentSelection);
        });
    }
}

// Helper para mantener instancias globales
window.autocompleteInstances = window.autocompleteInstances || {};

// Factory function para crear instancias fácilmente
window.createAutocomplete = function(options) {
    const instance = new AutocompleteSearch(options);
    window.autocompleteInstances[options.inputId] = instance;
    return instance;
};